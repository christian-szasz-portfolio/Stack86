import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { DiagramRenderer } from './diagram-renderer';
import { CanvasTestUtility, MockCanvasContext } from '../../../../testing/test-helpers';
import { REG_AX, VIRTUAL_WIDTH, VIRTUAL_HEIGHT } from '../models/diagram-layout.model';

describe('DiagramRenderer', () => {
  let ctx: MockCanvasContext;
  let renderer: DiagramRenderer;

  beforeEach(() => {
    ctx = CanvasTestUtility.createContext();
    renderer = new DiagramRenderer();
  });

  afterEach(() => {
    // No persistent spies to restore — context is local.
  });

  it('render() clears the canvas and draws on the provided context', () => {
    renderer.render(ctx as unknown as CanvasRenderingContext2D, 1000, 800);
    expect(ctx.clearRect).toHaveBeenCalled();
    // Should have drawn rounded rects (paths + fills)
    expect(ctx.beginPath).toHaveBeenCalled();
    expect(ctx.fill).toHaveBeenCalled();
  });

  it('hitTest returns null outside of any component', () => {
    renderer.render(ctx as unknown as CanvasRenderingContext2D, 1000, 800);
    const hit = renderer.hitTest(-100, -100);
    expect(hit).toBeNull();
  });

  it('hitTest returns a tooltip when the cursor is over a register', () => {
    renderer.render(ctx as unknown as CanvasRenderingContext2D, 1000, 800);
    // Center of REG_AX in CSS pixels with scale 1:1
    const cx = REG_AX.x + REG_AX.width / 2;
    const cy = REG_AX.y + REG_AX.height / 2;
    const hit = renderer.hitTest(cx, cy);
    expect(hit).not.toBeNull();
    expect(hit?.label).toMatch(/AX/);
  });

  it('scales uniformly (single fit factor) regardless of pane aspect ratio', () => {
    // Uniform fit: one scale = min(w/VW, h/VH); both accessors return it, so the
    // diagram never distorts even when the pane aspect ratio differs from virtual.
    renderer.render(ctx as unknown as CanvasRenderingContext2D, 1000, 800);
    expect(renderer.getScaleX()).toBe(renderer.getScaleY());
    expect(renderer.getScaleX()).toBeCloseTo(
      Math.min(1000 / VIRTUAL_WIDTH, 800 / VIRTUAL_HEIGHT),
    );
  });
});

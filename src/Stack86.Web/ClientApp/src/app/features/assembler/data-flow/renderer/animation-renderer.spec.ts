import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { AnimationRenderer } from './animation-renderer';
import { DiagramRenderer } from './diagram-renderer';
import { CanvasTestUtility, MockCanvasContext } from '../../../../testing/test-helpers';
import { ExecutionTraceFactory } from '../../../../core/emulator/execution/execution-trace.model';

describe('AnimationRenderer', () => {
  let ctx: MockCanvasContext;
  let diagramRenderer: DiagramRenderer;
  let renderer: AnimationRenderer;

  beforeEach(() => {
    ctx = CanvasTestUtility.createContext();
    diagramRenderer = new DiagramRenderer();
    renderer = new AnimationRenderer();

    // Stub rAF to a no-op so we don't run real frames in jsdom.
    vi.stubGlobal('requestAnimationFrame', () => 0 as unknown as number);
    vi.stubGlobal('cancelAnimationFrame', () => undefined);
    vi.stubGlobal('performance', { now: () => 0 });
  });

  afterEach(() => {
    renderer.stop();
    vi.unstubAllGlobals();
  });

  it('isAnimating is false before startAnimation is called', () => {
    expect(renderer.isAnimating()).toBe(false);
  });

  it('startAnimation animates the fetch cycle even for an empty trace', () => {
    // Every executed instruction was fetched by the BIU, so even a trace with
    // no register/memory/ALU activity animates the fetch phases.
    const trace = ExecutionTraceFactory.create();
    renderer.startAnimation(
      trace,
      ctx as unknown as CanvasRenderingContext2D,
      diagramRenderer,
      1000,
      800,
    );
    expect(renderer.isAnimating()).toBe(true);
  });

  it('stop() resets the animating flag', () => {
    const trace = ExecutionTraceFactory.create();
    trace.aluUsed = true;
    renderer.startAnimation(
      trace,
      ctx as unknown as CanvasRenderingContext2D,
      diagramRenderer,
      1000,
      800,
    );
    renderer.stop();
    expect(renderer.isAnimating()).toBe(false);
  });
});

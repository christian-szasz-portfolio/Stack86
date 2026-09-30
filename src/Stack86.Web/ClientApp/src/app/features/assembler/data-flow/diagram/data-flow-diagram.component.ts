import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  OnDestroy,
  afterNextRender,
  computed,
  effect,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { EmulatorFacade } from '../../../../state/emulator.facade';
import { ExecutionStatus } from '../../../../core/emulator/execution/execution-result.model';
import { DiagramRenderer, DiagramTooltip } from '../renderer/diagram-renderer';
import { AnimationRenderer } from '../renderer/animation-renderer';
import { VIRTUAL_WIDTH, VIRTUAL_HEIGHT } from '../models/diagram-layout.model';

@Component({
  host: {
    '(window:mousemove)': 'onWindowMouseMove($event)',
    '(window:mouseup)': 'onWindowMouseUp()',
    '(window:keydown)': 'onKeyDown($event)',
  },
  selector: 'emu-data-flow-diagram',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './data-flow-diagram.component.html',
  styleUrl: './data-flow-diagram.component.scss',
})
export class DataFlowDiagramComponent implements OnDestroy {
  private readonly canvasRef = viewChild.required<ElementRef<HTMLCanvasElement>>('diagramCanvas');

  private readonly facade = inject(EmulatorFacade);
  private readonly diagramRenderer = new DiagramRenderer();
  private readonly animationRenderer = new AnimationRenderer();
  private resizeObserver: ResizeObserver | null = null;
  private resizeTimeout: number | null = null;

  /** Cached CSS dimensions (not DPR-scaled) for correct rendering. */
  private cssWidth = 0;
  private cssHeight = 0;

  protected readonly tooltip = signal<DiagramTooltip | null>(null);
  protected readonly tooltipX = signal(0);
  protected readonly tooltipY = signal(0);

  // ── Zoom / pan ──
  private static readonly MIN_ZOOM = 1;
  private static readonly MAX_ZOOM = 4;
  private static readonly ZOOM_STEP = 1.25;
  private static readonly WHEEL_STEP = 1.1;
  private static readonly PAN_STEP = 40;

  public readonly zoom = signal(1);
  public readonly canZoomIn = computed(() => this.zoom() < DataFlowDiagramComponent.MAX_ZOOM - 1e-6);
  public readonly canZoomOut = computed(() => this.zoom() > DataFlowDiagramComponent.MIN_ZOOM + 1e-6);
  private panX = 0;
  private panY = 0;
  private hovered = false;

  // Drag-to-pan state
  private dragging = false;
  private dragStartX = 0;
  private dragStartY = 0;
  private dragStartPanX = 0;
  private dragStartPanY = 0;

  constructor() {
    afterNextRender(() => {
      this.setupCanvas();
      this.observeResize();
    });

    // React to trace changes — animate data flow
    effect(() => {
      const trace = this.facade.lastTrace();
      const cpu = this.facade.cpu();
      const status = this.facade.executionStatus();

      if (!trace || !this.canvasRef().nativeElement) return;

      // Only animate in step/animated-run modes (not idle or batch)
      const delay = this.facade.executionDelayMs();
      if (status === ExecutionStatus.Running && delay <= 0) {
        // Batch mode — just redraw static with current CPU
        this.renderStatic(cpu);
        return;
      }

      this.startAnimationWithCssDimensions(trace, cpu);
    });

    // Redraw static on reset/load
    effect(() => {
      const status = this.facade.executionStatus();
      const cpu = this.facade.cpu();

      if (status === ExecutionStatus.Idle) {
        this.animationRenderer.stop();
        this.renderStatic(cpu);
      }
    });
  }

  public ngOnDestroy(): void {
    this.animationRenderer.stop();
    this.resizeObserver?.disconnect();
    if (this.resizeTimeout !== null) {
      clearTimeout(this.resizeTimeout);
    }
  }

  protected onMouseMove(event: MouseEvent): void {
    if (this.dragging) return;
    const canvas = this.canvasRef().nativeElement;
    const rect = canvas.getBoundingClientRect();
    const cssX = event.clientX - rect.left;
    const cssY = event.clientY - rect.top;

    const hit = this.diagramRenderer.hitTest(cssX, cssY);
    this.tooltip.set(hit);

    if (hit) {
      // Position tooltip to the right/below cursor, clamped to container
      const offsetX = 12;
      const offsetY = 12;
      const maxX = rect.width - 260;
      const maxY = rect.height - 80;
      this.tooltipX.set(Math.min(cssX + offsetX, maxX));
      this.tooltipY.set(Math.min(cssY + offsetY, maxY));
      canvas.style.cursor = 'pointer';
    } else {
      canvas.style.cursor = this.zoom() > DataFlowDiagramComponent.MIN_ZOOM ? 'grab' : 'default';
    }
  }

  protected onMouseEnter(): void {
    this.hovered = true;
  }

  protected onMouseLeave(): void {
    this.hovered = false;
    this.tooltip.set(null);
  }

  public zoomIn(): void {
    this.zoomTo(this.zoom() * DataFlowDiagramComponent.ZOOM_STEP, this.cssWidth / 2, this.cssHeight / 2);
  }

  public zoomOut(): void {
    this.zoomTo(this.zoom() / DataFlowDiagramComponent.ZOOM_STEP, this.cssWidth / 2, this.cssHeight / 2);
  }

  public resetZoom(): void {
    this.zoom.set(1);
    this.panX = 0;
    this.panY = 0;
    this.applyViewport();
  }

  protected onWheel(event: WheelEvent): void {
    event.preventDefault();
    const rect = this.canvasRef().nativeElement.getBoundingClientRect();
    const factor = event.deltaY < 0
      ? DataFlowDiagramComponent.WHEEL_STEP
      : 1 / DataFlowDiagramComponent.WHEEL_STEP;
    this.zoomTo(this.zoom() * factor, event.clientX - rect.left, event.clientY - rect.top);
  }

  protected onMouseDown(event: MouseEvent): void {
    if (this.zoom() <= DataFlowDiagramComponent.MIN_ZOOM) return;
    event.preventDefault();
    this.dragging = true;
    this.dragStartX = event.clientX;
    this.dragStartY = event.clientY;
    this.dragStartPanX = this.panX;
    this.dragStartPanY = this.panY;
    this.tooltip.set(null);
    this.canvasRef().nativeElement.style.cursor = 'grabbing';
  }

  protected onWindowMouseMove(event: MouseEvent): void {
    if (!this.dragging) return;
    this.panX = this.dragStartPanX + (event.clientX - this.dragStartX);
    this.panY = this.dragStartPanY + (event.clientY - this.dragStartY);
    this.clampPan(this.diagramRenderer.getBaseScale(this.cssWidth, this.cssHeight) * this.zoom());
    this.applyViewport();
  }

  protected onWindowMouseUp(): void {
    if (!this.dragging) return;
    this.dragging = false;
    this.canvasRef().nativeElement.style.cursor =
      this.zoom() > DataFlowDiagramComponent.MIN_ZOOM ? 'grab' : 'default';
  }

  protected onKeyDown(event: KeyboardEvent): void {
    if (!this.hovered) return;

    // Ctrl/Cmd + zoom
    if (event.ctrlKey || event.metaKey) {
      switch (event.key) {
        case '+':
        case '=':
          event.preventDefault();
          this.zoomIn();
          break;
        case '-':
        case '_':
          event.preventDefault();
          this.zoomOut();
          break;
        case '0':
          event.preventDefault();
          this.resetZoom();
          break;
        default:
          break;
      }
      return;
    }

    // Arrow-key panning (only meaningful when zoomed in)
    if (this.zoom() <= DataFlowDiagramComponent.MIN_ZOOM) return;
    const step = DataFlowDiagramComponent.PAN_STEP;
    switch (event.key) {
      case 'ArrowRight':
        event.preventDefault();
        this.panBy(-step, 0);
        break;
      case 'ArrowLeft':
        event.preventDefault();
        this.panBy(step, 0);
        break;
      case 'ArrowDown':
        event.preventDefault();
        this.panBy(0, -step);
        break;
      case 'ArrowUp':
        event.preventDefault();
        this.panBy(0, step);
        break;
      default:
        break;
    }
  }

  /** Pans by a CSS-px delta (used by arrow keys). */
  private panBy(dx: number, dy: number): void {
    if (this.zoom() <= DataFlowDiagramComponent.MIN_ZOOM) return;
    this.panX += dx;
    this.panY += dy;
    this.clampPan(this.diagramRenderer.getBaseScale(this.cssWidth, this.cssHeight) * this.zoom());
    this.applyViewport();
  }

  /** Zooms toward a focal point (CSS px) so that point stays put under the cursor. */
  private zoomTo(rawZoom: number, focalX: number, focalY: number): void {
    const newZoom = Math.max(
      DataFlowDiagramComponent.MIN_ZOOM,
      Math.min(DataFlowDiagramComponent.MAX_ZOOM, rawZoom),
    );
    const oldZoom = this.zoom();
    if (newZoom === oldZoom || this.cssWidth === 0 || this.cssHeight === 0) return;

    const baseScale = this.diagramRenderer.getBaseScale(this.cssWidth, this.cssHeight);
    const oldScale = baseScale * oldZoom;
    const newScale = baseScale * newZoom;
    const oldOffsetX = (this.cssWidth - VIRTUAL_WIDTH * oldScale) / 2 + this.panX;
    const oldOffsetY = (this.cssHeight - VIRTUAL_HEIGHT * oldScale) / 2 + this.panY;

    // Content point currently under the focal point.
    const contentX = (focalX - oldOffsetX) / oldScale;
    const contentY = (focalY - oldOffsetY) / oldScale;

    // Solve for the pan that keeps that content point under the focal point.
    this.panX = focalX - contentX * newScale - (this.cssWidth - VIRTUAL_WIDTH * newScale) / 2;
    this.panY = focalY - contentY * newScale - (this.cssHeight - VIRTUAL_HEIGHT * newScale) / 2;
    this.zoom.set(newZoom);
    this.clampPan(newScale);
    this.applyViewport();
  }

  /** Keeps pan within the overflow bounds (and pinned to center when not zoomed). */
  private clampPan(scale: number): void {
    if (this.zoom() <= DataFlowDiagramComponent.MIN_ZOOM) {
      this.panX = 0;
      this.panY = 0;
      return;
    }
    const overflowX = Math.max(0, (VIRTUAL_WIDTH * scale - this.cssWidth) / 2);
    const overflowY = Math.max(0, (VIRTUAL_HEIGHT * scale - this.cssHeight) / 2);
    this.panX = Math.max(-overflowX, Math.min(overflowX, this.panX));
    this.panY = Math.max(-overflowY, Math.min(overflowY, this.panY));
  }

  /** Pushes the current viewport to the renderer and repaints if not animating. */
  private applyViewport(): void {
    this.diagramRenderer.setViewport(this.zoom(), this.panX, this.panY);
    if (!this.animationRenderer.isAnimating()) {
      this.renderStatic(this.facade.cpu());
    }
  }

  /**
   * Sizes the canvas buffer for the current DPR, applies DPR scale,
   * then renders the static diagram using CSS dimensions.
   */
  private setupCanvas(): void {
    const canvas = this.canvasRef().nativeElement;
    const rect = canvas.getBoundingClientRect();
    if (rect.width === 0 || rect.height === 0) return;

    this.cssWidth = rect.width;
    this.cssHeight = rect.height;

    const dpr = window.devicePixelRatio || 1;
    canvas.width = rect.width * dpr;
    canvas.height = rect.height * dpr;

    const ctx = canvas.getContext('2d');
    if (ctx) {
      ctx.scale(dpr, dpr);
      this.diagramRenderer.render(ctx, this.cssWidth, this.cssHeight, this.facade.cpu());
    }
  }

  private observeResize(): void {
    this.resizeObserver = new ResizeObserver(() => {
      if (this.resizeTimeout !== null) {
        clearTimeout(this.resizeTimeout);
      }
      this.resizeTimeout = window.setTimeout(() => {
        this.resizeTimeout = null;
        this.handleResize();
      }, 60);
    });
    this.resizeObserver.observe(this.canvasRef().nativeElement);
  }

  /** On resize, re-setup the canvas and update any running animation. */
  private handleResize(): void {
    const wasAnimating = this.animationRenderer.isAnimating();
    if (wasAnimating) {
      this.animationRenderer.stop();
    }

    this.setupCanvas();

    // Re-clamp the pan for the new size and keep the current zoom applied.
    this.clampPan(this.diagramRenderer.getBaseScale(this.cssWidth, this.cssHeight) * this.zoom());
    this.diagramRenderer.setViewport(this.zoom(), this.panX, this.panY);

    // Restart animation with new dimensions if it was running
    if (wasAnimating) {
      const trace = this.facade.lastTrace();
      if (trace) {
        this.startAnimationWithCssDimensions(trace, this.facade.cpu());
      }
    } else {
      this.renderStatic(this.facade.cpu());
    }
  }

  /** Start animation passing CSS dimensions (not DPR-scaled pixel dimensions). */
  private startAnimationWithCssDimensions(
    trace: import('../../../../core/emulator/execution/execution-trace.model').ExecutionTrace,
    cpu?: import('../../../../core/emulator/cpu/cpu.model').CpuState,
  ): void {
    const canvas = this.canvasRef().nativeElement;
    if (!canvas || this.cssWidth === 0 || this.cssHeight === 0) return;

    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    this.animationRenderer.startAnimation(
      trace,
      ctx,
      this.diagramRenderer,
      this.cssWidth,
      this.cssHeight,
      cpu,
    );
  }

  private renderStatic(cpu?: import('../../../../core/emulator/cpu/cpu.model').CpuState): void {
    const canvas = this.canvasRef().nativeElement;
    if (!canvas) return;
    const rect = canvas.getBoundingClientRect();
    if (rect.width === 0 || rect.height === 0) return;

    this.cssWidth = rect.width;
    this.cssHeight = rect.height;

    const dpr = window.devicePixelRatio || 1;
    canvas.width = rect.width * dpr;
    canvas.height = rect.height * dpr;

    const ctx = canvas.getContext('2d');
    if (ctx) {
      ctx.scale(dpr, dpr);
      this.diagramRenderer.render(ctx, this.cssWidth, this.cssHeight, cpu);
    }
  }
}

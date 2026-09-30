import { ExecutionTrace, IoType, BusDirection } from '../../../../core/emulator/execution/execution-trace.model';
import { CpuState } from '../../../../core/emulator/cpu/cpu.model';
import { DiagramRenderer } from './diagram-renderer';
import {
  DiagramComponent,
  BusType,
  REGISTER_ID_MAP,
  ALU, FLAGS, MEMORY_BLOCK, IO_BLOCK,
  ADDR_ADDER, BUS_CONTROL,
  DECODER, CONTROL_UNIT, INSTR_QUEUE,
  INTERNAL_BUS_Y, INTERNAL_BUS_X1, INTERNAL_BUS_X2,
  ADDRESS_BUS_X, DATA_BUS_X, CONTROL_BUS_X,
  EXTERNAL_BUS_TOP, EXTERNAL_BUS_BOTTOM,
  COLORS,
} from '../models/diagram-layout.model';

interface AnimationPhase {
  startTime: number;
  duration: number;
  draw: (progress: number) => void;
}

export class AnimationRenderer {
  private static readonly PHASE_DURATION = 480;
  private phases: AnimationPhase[] = [];
  private animationStart = 0;
  private running = false;
  private frameId: number | null = null;
  private canvasCtx: CanvasRenderingContext2D | null = null;
  private diagramRenderer: DiagramRenderer | null = null;
  private canvasWidth = 0;
  private canvasHeight = 0;
  private currentCpu: CpuState | undefined;

  public startAnimation(
    trace: ExecutionTrace,
    canvasCtx: CanvasRenderingContext2D,
    diagramRenderer: DiagramRenderer,
    width: number,
    height: number,
    cpu?: CpuState,
  ): void {
    this.stop();
    this.canvasCtx = canvasCtx;
    this.diagramRenderer = diagramRenderer;
    this.canvasWidth = width;
    this.canvasHeight = height;
    this.currentCpu = cpu;
    this.phases = this.buildPhases(trace);

    if (this.phases.length === 0) return;

    this.running = true;
    this.animationStart = performance.now();
    this.tick(this.animationStart);
  }

  public stop(): void {
    this.running = false;
    if (this.frameId !== null) {
      cancelAnimationFrame(this.frameId);
      this.frameId = null;
    }
    this.phases = [];
  }

  public isAnimating(): boolean {
    return this.running;
  }

  private tick(timestamp: number): void {
    if (!this.running || !this.canvasCtx || !this.diagramRenderer) return;

    // Redraw static diagram
    this.diagramRenderer.render(this.canvasCtx, this.canvasWidth, this.canvasHeight, this.currentCpu);

    const elapsed = timestamp - this.animationStart;
    let anyActive = false;

    for (const phase of this.phases) {
      const phaseElapsed = elapsed - phase.startTime;
      if (phaseElapsed < 0) {
        anyActive = true;
        continue;
      }
      if (phaseElapsed > phase.duration) continue;

      anyActive = true;
      const progress = Math.min(phaseElapsed / phase.duration, 1);
      phase.draw(progress);
    }

    const totalDuration = this.phases.length > 0
      ? this.phases[this.phases.length - 1].startTime + this.phases[this.phases.length - 1].duration
      : 0;

    if (anyActive || elapsed < totalDuration) {
      this.frameId = requestAnimationFrame((t) => this.tick(t));
    } else {
      this.running = false;
      this.frameId = null;
    }
  }

  /** Symmetric ease-in-out used for dots and pulses traveling along a path. */
  private static easeInOut(t: number): number {
    return t < 0.5 ? 2 * t * t : 1 - Math.pow(-2 * t + 2, 2) / 2;
  }

  /**
   * "Hold" envelope for component highlights: a quick ramp-in, a steady hold at
   * full brightness, then a gentle fade-out. Keeping a component lit for the
   * bulk of its phase — instead of pulsing back to zero at the midpoint — is
   * what stops the diagram from looking like it is blinking.
   */
  private static holdEnvelope(progress: number): number {
    const rampIn = 0.18;
    const fadeStart = 0.72;
    if (progress < rampIn) return progress / rampIn;
    if (progress > fadeStart) return Math.max(0, (1 - progress) / (1 - fadeStart));
    return 1;
  }

  private buildPhases(trace: ExecutionTrace): AnimationPhase[] {
    const phases: AnimationPhase[] = [];
    let time = 0;
    const phaseDur = AnimationRenderer.PHASE_DURATION;

    // Phase 0: Instruction fetch — every step starts with the BIU fetching the
    // instruction: CS:IP → address adder → address bus → memory → data bus →
    // instruction queue → decoder → control unit.
    time = this.appendFetchPhases(phases, time);

    // Phase 1: Highlight source registers (reads) + trajectory from register → internal bus
    const readRegs = this.getUniqueComponents(trace.registersRead);
    if (readRegs.length > 0) {
      phases.push(this.createHighlightPhase(time, phaseDur, readRegs, COLORS.busData));

      // Data flows from each source register down to the internal bus first...
      for (const reg of readRegs) {
        const regCx = reg.x + reg.width / 2;
        const regBot = reg.y + reg.height;
        phases.push(this.createTrajectoryPhase(time, phaseDur * 0.55, [
          { x: regCx, y: regBot },
          { x: regCx, y: INTERNAL_BUS_Y },
        ], COLORS.busData));
      }

      // ...then the internal bus carries it across, once the dots have arrived.
      phases.push(this.createBusPulsePhase(time + phaseDur * 0.45, phaseDur * 0.55, BusType.Internal));
    }
    time += phaseDur;

    // Phase 2: ALU — show data flowing from internal bus into ALU, then flags
    if (trace.aluUsed) {
      const aluCx = ALU.x + ALU.width / 2;

      // Operands ride the internal bus up into the ALU, which lights as they land.
      phases.push(this.createHighlightPhase(time, phaseDur, [ALU], COLORS.aluActive));
      phases.push(this.createTrajectoryPhase(time, phaseDur * 0.5, [
        { x: aluCx, y: INTERNAL_BUS_Y },
        { x: aluCx, y: ALU.y },
      ], COLORS.aluActive));

      // Once the ALU has "computed", the result condition codes drop into Flags.
      phases.push(this.createHighlightPhase(time + phaseDur * 0.5, phaseDur * 0.5, [FLAGS], COLORS.flagsActive));
      phases.push(this.createTrajectoryPhase(time + phaseDur * 0.5, phaseDur * 0.5, [
        { x: aluCx, y: ALU.y + ALU.height },
        { x: aluCx, y: FLAGS.y },
      ], COLORS.flagsActive));

      time += phaseDur;
    }

    // Phase 3: Memory access — full path: BIU → Address Adder → Bus Control → External bus → Memory
    if (trace.memoryReads.length > 0 || trace.memoryWrites.length > 0) {
      // Highlight address adder
      phases.push(this.createHighlightPhase(time, phaseDur * 0.5, [ADDR_ADDER], COLORS.busAddress));

      // Segment register highlight
      if (trace.segmentUsed) {
        const segComp = REGISTER_ID_MAP[trace.segmentUsed];
        if (segComp) {
          phases.push(this.createHighlightPhase(time, phaseDur * 0.5, [segComp], COLORS.busAddress));

          // Trajectory: segment register → internal bus → address adder
          const segCx = segComp.x + segComp.width / 2;
          phases.push(this.createTrajectoryPhase(time, phaseDur * 0.4, [
            { x: segCx, y: segComp.y + segComp.height },
            { x: segCx, y: INTERNAL_BUS_Y },
          ], COLORS.busAddress));
        }
      }

      // Trajectory: internal bus → Address Adder
      const adderCx = ADDR_ADDER.x + ADDR_ADDER.width / 2;
      phases.push(this.createTrajectoryPhase(time, phaseDur * 0.5, [
        { x: adderCx, y: INTERNAL_BUS_Y },
        { x: adderCx, y: ADDR_ADDER.y },
      ], COLORS.busAddress));

      // External bus pulse — address bus fires first, then data bus follows
      phases.push(this.createBusPulsePhase(time + phaseDur * 0.2, phaseDur * 0.8, BusType.Address));

      const memDirection = trace.memoryReads.length > 0 ? BusDirection.Read : BusDirection.Write;
      phases.push(this.createBusPulsePhase(time + phaseDur * 0.4, phaseDur * 0.6, BusType.Data));
      phases.push(this.createHighlightPhase(time + phaseDur * 0.3, phaseDur * 0.7, [MEMORY_BLOCK],
        memDirection === BusDirection.Read ? COLORS.busData : COLORS.busControl));

      // Bus Control → external bus trajectory (orthogonal: down, across, down)
      const busCtrlCx = BUS_CONTROL.x + BUS_CONTROL.width / 2;
      const manifoldY = (BUS_CONTROL.y + BUS_CONTROL.height + EXTERNAL_BUS_TOP) / 2;
      phases.push(this.createTrajectoryPhase(time + phaseDur * 0.15, phaseDur * 0.5, [
        { x: busCtrlCx, y: BUS_CONTROL.y + BUS_CONTROL.height },
        { x: busCtrlCx, y: manifoldY },
        { x: DATA_BUS_X, y: manifoldY },
        { x: DATA_BUS_X, y: EXTERNAL_BUS_TOP },
      ], COLORS.busData));

      // Show address annotation
      const addr = trace.memoryReads.length > 0
        ? trace.memoryReads[0].address
        : trace.memoryWrites[0].address;
      phases.push(this.createAddrAnnotationPhase(time, phaseDur, addr, trace.segmentUsed));

      time += phaseDur;
    }

    // Phase 4: Destination register writes — data flows from internal bus up into destination registers
    const writeRegs = this.getUniqueComponents(trace.registersWritten);
    if (writeRegs.length > 0) {
      // The result travels across the internal bus first...
      phases.push(this.createBusPulsePhase(time, phaseDur * 0.55, BusType.Internal));

      // ...then rises into each destination register, which lights up on arrival.
      phases.push(this.createHighlightPhase(time + phaseDur * 0.4, phaseDur * 0.6, writeRegs, COLORS.registerActive));
      for (const reg of writeRegs) {
        const regCx = reg.x + reg.width / 2;
        phases.push(this.createTrajectoryPhase(time + phaseDur * 0.4, phaseDur * 0.6, [
          { x: regCx, y: INTERNAL_BUS_Y },
          { x: regCx, y: reg.y + reg.height },
        ], COLORS.registerActive));
      }
      time += phaseDur;
    }

    // Phase 5: I/O activity — show full path through control bus
    if (trace.ioType) {
      phases.push(this.createHighlightPhase(time, phaseDur, [IO_BLOCK],
        trace.ioType === IoType.Output ? COLORS.busData : COLORS.busAddress));
      phases.push(this.createHighlightPhase(time, phaseDur, [BUS_CONTROL], COLORS.busControl));
      phases.push(this.createBusPulsePhase(time, phaseDur, BusType.Control));

      // Bus Control → control bus trajectory (orthogonal: down, across, down)
      const busCtrlCx = BUS_CONTROL.x + BUS_CONTROL.width / 2;
      const manifoldY = (BUS_CONTROL.y + BUS_CONTROL.height + EXTERNAL_BUS_TOP) / 2;
      phases.push(this.createTrajectoryPhase(time, phaseDur * 0.6, [
        { x: busCtrlCx, y: BUS_CONTROL.y + BUS_CONTROL.height },
        { x: busCtrlCx, y: manifoldY },
        { x: CONTROL_BUS_X, y: manifoldY },
        { x: CONTROL_BUS_X, y: EXTERNAL_BUS_TOP },
      ], COLORS.busControl));

      time += phaseDur;
    }

    // Phase 6: Stack activity
    if (trace.stackAccess) {
      const spComp = REGISTER_ID_MAP['sp'];
      if (spComp) {
        phases.push(this.createHighlightPhase(time, phaseDur, [spComp], COLORS.busControl));

        // SP → internal bus trajectory
        const spCx = spComp.x + spComp.width / 2;
        phases.push(this.createTrajectoryPhase(time, phaseDur * 0.5, [
          { x: spCx, y: spComp.y + spComp.height },
          { x: spCx, y: INTERNAL_BUS_Y },
        ], COLORS.busControl));
      }
      phases.push(this.createHighlightPhase(time, phaseDur, [MEMORY_BLOCK], COLORS.busControl));
      phases.push(this.createBusPulsePhase(time, phaseDur, BusType.Data));
      time += phaseDur;
    }

    return phases;
  }

  /**
   * Appends the instruction-fetch cycle phases that run at the start of every
   * step: CS and IP feed the address adder, the physical address goes out on
   * the address bus, memory returns the instruction bytes on the data bus,
   * and the bytes flow through the instruction queue into the decoder.
   * Returns the time at which subsequent phases should start.
   */
  private appendFetchPhases(phases: AnimationPhase[], time: number): number {
    const phaseDur = AnimationRenderer.PHASE_DURATION;

    // CS:IP highlight + trajectories down to the internal bus
    const addrRegs = [REGISTER_ID_MAP['cs'], REGISTER_ID_MAP['ip']].filter(
      (c): c is DiagramComponent => c !== undefined,
    );
    phases.push(this.createHighlightPhase(time, phaseDur * 0.5, addrRegs, COLORS.busAddress));
    for (const reg of addrRegs) {
      const regCx = reg.x + reg.width / 2;
      phases.push(this.createTrajectoryPhase(time, phaseDur * 0.3, [
        { x: regCx, y: reg.y + reg.height },
        { x: regCx, y: INTERNAL_BUS_Y },
      ], COLORS.busAddress));
    }

    // Internal bus → address adder (segment × 16 + offset)
    const adderCx = ADDR_ADDER.x + ADDR_ADDER.width / 2;
    phases.push(this.createTrajectoryPhase(time + phaseDur * 0.2, phaseDur * 0.3, [
      { x: adderCx, y: INTERNAL_BUS_Y },
      { x: adderCx, y: ADDR_ADDER.y },
    ], COLORS.busAddress));
    phases.push(this.createHighlightPhase(time + phaseDur * 0.25, phaseDur * 0.4, [ADDR_ADDER], COLORS.busAddress));

    // Address out to memory; instruction bytes come back on the data bus
    phases.push(this.createBusPulsePhase(time + phaseDur * 0.4, phaseDur * 0.4, BusType.Address));
    phases.push(this.createHighlightPhase(time + phaseDur * 0.55, phaseDur * 0.45, [MEMORY_BLOCK], COLORS.busData));
    phases.push(this.createBusPulsePhase(time + phaseDur * 0.6, phaseDur * 0.4, BusType.Data, true));

    // Fetched bytes land in the queue, then the EU decodes them
    phases.push(this.createHighlightPhase(time + phaseDur * 0.75, phaseDur * 0.4, [INSTR_QUEUE], COLORS.busData));
    phases.push(this.createHighlightPhase(time + phaseDur * 0.95, phaseDur * 0.45, [DECODER, CONTROL_UNIT], COLORS.busControl));

    return time + phaseDur * 1.4;
  }

  private getUniqueComponents(regNames: string[]): DiagramComponent[] {
    const seen = new Set<string>();
    const comps: DiagramComponent[] = [];
    for (const name of regNames) {
      const comp = REGISTER_ID_MAP[name];
      if (comp && !seen.has(comp.id)) {
        seen.add(comp.id);
        comps.push(comp);
      }
    }
    return comps;
  }

  private createHighlightPhase(
    startTime: number,
    duration: number,
    components: DiagramComponent[],
    color: string,
  ): AnimationPhase {
    return {
      startTime,
      duration,
      draw: (progress: number) => {
        if (!this.canvasCtx || !this.diagramRenderer) return;

        // Hold envelope: ramp up, stay lit, then fade out. Holding the
        // component on for its whole phase is what removes the mid-phase blink.
        const alpha = 0.3 + AnimationRenderer.holdEnvelope(progress) * 0.7;

        this.canvasCtx.save();
        this.canvasCtx.globalAlpha = alpha;
        for (const comp of components) {
          this.diagramRenderer.highlightComponent(this.canvasCtx, comp, color);
        }
        this.canvasCtx.restore();
      },
    };
  }

  /**
   * Creates a trajectory phase that draws a glowing dot traveling along a
   * multi-point path in virtual coordinates, leaving a fading trail behind.
   */
  private createTrajectoryPhase(
    startTime: number,
    duration: number,
    virtualPath: Array<{ x: number; y: number }>,
    color: string,
  ): AnimationPhase {
    return {
      startTime,
      duration,
      draw: (progress: number) => {
        if (!this.canvasCtx || !this.diagramRenderer || virtualPath.length < 2) return;
        const ctx = this.canvasCtx;
        const dr = this.diagramRenderer;

        const eased = AnimationRenderer.easeInOut(progress);

        // Compute total virtual path length
        let totalLen = 0;
        const segLens: number[] = [];
        for (let i = 1; i < virtualPath.length; i++) {
          const dx = virtualPath[i].x - virtualPath[i - 1].x;
          const dy = virtualPath[i].y - virtualPath[i - 1].y;
          const len = Math.sqrt(dx * dx + dy * dy);
          segLens.push(len);
          totalLen += len;
        }

        // Find interpolated position along path
        const targetDist = eased * totalLen;
        let walked = 0;
        let dotVX = virtualPath[0].x;
        let dotVY = virtualPath[0].y;

        for (let i = 0; i < segLens.length; i++) {
          if (walked + segLens[i] >= targetDist) {
            const frac = (targetDist - walked) / segLens[i];
            dotVX = virtualPath[i].x + (virtualPath[i + 1].x - virtualPath[i].x) * frac;
            dotVY = virtualPath[i].y + (virtualPath[i + 1].y - virtualPath[i].y) * frac;
            break;
          }
          walked += segLens[i];
          dotVX = virtualPath[i + 1].x;
          dotVY = virtualPath[i + 1].y;
        }

        // Convert to scaled canvas coordinates
        const dot = dr.getScaledBusPoint(dotVX, dotVY);
        const trailStart = dr.getScaledBusPoint(virtualPath[0].x, virtualPath[0].y);

        ctx.save();

        // Draw a thin trail from start to dot position
        const fadeAlpha = progress < 0.1 ? progress * 10 : progress > 0.85 ? (1 - progress) / 0.15 : 0.6;

        // Build scaled path for trail
        const scaledPath: Array<{ x: number; y: number }> = virtualPath.map(
          (p) => dr.getScaledBusPoint(p.x, p.y),
        );

        // Trail line along path up to dot position
        ctx.globalAlpha = fadeAlpha;
        ctx.strokeStyle = color;
        ctx.lineWidth = 2;
        ctx.beginPath();
        ctx.moveTo(trailStart.x, trailStart.y);

        // Draw path segments up to the current point
        let trailWalked = 0;
        for (let i = 0; i < segLens.length; i++) {
          if (trailWalked + segLens[i] >= targetDist) {
            ctx.lineTo(dot.x, dot.y);
            break;
          }
          ctx.lineTo(scaledPath[i + 1].x, scaledPath[i + 1].y);
          trailWalked += segLens[i];
        }
        ctx.stroke();

        // Bright dot at head
        const dotRadius = dr.getScaleX() * 4;
        ctx.globalAlpha = 1;
        ctx.fillStyle = COLORS.particle;
        ctx.shadowColor = color;
        ctx.shadowBlur = 10;
        ctx.beginPath();
        ctx.arc(dot.x, dot.y, dotRadius, 0, Math.PI * 2);
        ctx.fill();

        ctx.restore();
      },
    };
  }

  private createBusPulsePhase(
    startTime: number,
    duration: number,
    busType: BusType,
    reverse = false,
  ): AnimationPhase {
    return {
      startTime,
      duration,
      draw: (progress: number) => {
        if (!this.canvasCtx || !this.diagramRenderer) return;
        const ctx = this.canvasCtx;
        const dr = this.diagramRenderer;

        ctx.save();

        const eased = AnimationRenderer.easeInOut(progress);

        if (busType === BusType.Internal) {
          // Horizontal pulse: traveling dots along internal bus
          const startX = dr.getScaledBusPoint(INTERNAL_BUS_X1, 0).x;
          const endX = dr.getScaledBusPoint(INTERNAL_BUS_X2, 0).x;
          const y = dr.getScaledBusPoint(0, INTERNAL_BUS_Y).y;
          const busColor = COLORS.busInternal;

          // Leading pulse dot
          const pulseX = startX + (endX - startX) * eased;
          const dotRadius = dr.getScaleX() * 5;

          // Trailing glow
          const trailGrad = ctx.createLinearGradient(
            Math.max(startX, pulseX - (endX - startX) * 0.15), y,
            pulseX, y,
          );
          trailGrad.addColorStop(0, 'transparent');
          trailGrad.addColorStop(1, busColor);

          ctx.globalAlpha = 0.5;
          ctx.strokeStyle = trailGrad;
          ctx.lineWidth = 4;
          ctx.beginPath();
          ctx.moveTo(Math.max(startX, pulseX - (endX - startX) * 0.15), y);
          ctx.lineTo(pulseX, y);
          ctx.stroke();

          // Bright dot
          ctx.globalAlpha = 1;
          ctx.beginPath();
          ctx.arc(pulseX, y, dotRadius, 0, Math.PI * 2);
          ctx.fillStyle = COLORS.particle;
          ctx.shadowColor = busColor;
          ctx.shadowBlur = 12;
          ctx.fill();
        } else {
          // Vertical pulse along external bus with traveling dot
          const busX = busType === BusType.Address ? ADDRESS_BUS_X
            : busType === BusType.Data ? DATA_BUS_X
            : CONTROL_BUS_X;
          const color = busType === BusType.Address ? COLORS.busAddress
            : busType === BusType.Data ? COLORS.busData
            : COLORS.busControl;

          const start = reverse
            ? dr.getScaledBusPoint(busX, EXTERNAL_BUS_BOTTOM)
            : dr.getScaledBusPoint(busX, EXTERNAL_BUS_TOP);
          const end = reverse
            ? dr.getScaledBusPoint(busX, EXTERNAL_BUS_TOP)
            : dr.getScaledBusPoint(busX, EXTERNAL_BUS_BOTTOM);
          const pulseY = start.y + (end.y - start.y) * eased;
          const dotRadius = dr.getScaleX() * 4;
          const trailFromY = reverse
            ? Math.min(start.y, pulseY - (end.y - start.y) * 0.2)
            : Math.max(start.y, pulseY - (end.y - start.y) * 0.2);

          // Trailing glow
          const trailGrad = ctx.createLinearGradient(
            start.x, trailFromY,
            start.x, pulseY,
          );
          trailGrad.addColorStop(0, 'transparent');
          trailGrad.addColorStop(1, color);

          ctx.globalAlpha = 0.5;
          ctx.strokeStyle = trailGrad;
          ctx.lineWidth = 4;
          ctx.beginPath();
          ctx.moveTo(start.x, trailFromY);
          ctx.lineTo(start.x, pulseY);
          ctx.stroke();

          // Bright dot
          ctx.globalAlpha = 1;
          ctx.beginPath();
          ctx.arc(start.x, pulseY, dotRadius, 0, Math.PI * 2);
          ctx.fillStyle = COLORS.particle;
          ctx.shadowColor = color;
          ctx.shadowBlur = 12;
          ctx.fill();
        }

        ctx.restore();
      },
    };
  }

  private createAddrAnnotationPhase(
    startTime: number,
    duration: number,
    address: number,
    segment: string | null,
  ): AnimationPhase {
    return {
      startTime,
      duration,
      draw: (progress: number) => {
        if (!this.canvasCtx || !this.diagramRenderer) return;

        // Smooth fade in/out
        const alpha = progress < 0.15 ? progress / 0.15
          : progress > 0.85 ? (1 - progress) / 0.15
          : 1;

        const dr = this.diagramRenderer;
        const ctx = this.canvasCtx;
        const adderCenter = dr.getScaledBusPoint(
          ADDR_ADDER.x + ADDR_ADDER.width / 2,
          ADDR_ADDER.y + ADDR_ADDER.height + 28,
        );

        const segName = (segment ?? 'DS').toUpperCase();
        const hex = address.toString(16).toUpperCase().padStart(4, '0');
        const text = `${segName}:0x${hex}`;

        // Background pill behind text
        ctx.save();
        ctx.globalAlpha = alpha * 0.85;
        const fontSize = dr.getScaleY() * 10;
        ctx.font = `bold ${fontSize}px monospace`;
        const textWidth = ctx.measureText(text).width;
        const pillPadX = 6;
        const pillPadY = 4;
        const pillX = adderCenter.x - textWidth / 2 - pillPadX;
        const pillY = adderCenter.y - fontSize - pillPadY;
        const pillW = textWidth + pillPadX * 2;
        const pillH = fontSize + pillPadY * 2;
        const pillR = 3;

        ctx.beginPath();
        ctx.moveTo(pillX + pillR, pillY);
        ctx.lineTo(pillX + pillW - pillR, pillY);
        ctx.quadraticCurveTo(pillX + pillW, pillY, pillX + pillW, pillY + pillR);
        ctx.lineTo(pillX + pillW, pillY + pillH - pillR);
        ctx.quadraticCurveTo(pillX + pillW, pillY + pillH, pillX + pillW - pillR, pillY + pillH);
        ctx.lineTo(pillX + pillR, pillY + pillH);
        ctx.quadraticCurveTo(pillX, pillY + pillH, pillX, pillY + pillH - pillR);
        ctx.lineTo(pillX, pillY + pillR);
        ctx.quadraticCurveTo(pillX, pillY, pillX + pillR, pillY);
        ctx.closePath();

        ctx.fillStyle = 'rgba(0, 0, 0, 0.65)';
        ctx.fill();
        ctx.strokeStyle = COLORS.busAddress;
        ctx.lineWidth = 1;
        ctx.stroke();

        // Text
        ctx.fillStyle = COLORS.busAddress;
        ctx.textAlign = 'center';
        ctx.shadowColor = COLORS.busAddress;
        ctx.shadowBlur = 4;
        ctx.fillText(text, adderCenter.x, adderCenter.y);
        ctx.restore();
      },
    };
  }
}

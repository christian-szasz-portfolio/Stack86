import { CpuState } from '../../../../core/emulator/cpu/cpu.model';
import {
  DiagramComponent,
  VIRTUAL_WIDTH,
  VIRTUAL_HEIGHT,
  EU_BOX,
  BIU_BOX,
  REG_AX, REG_BX, REG_CX, REG_DX,
  REG_SP, REG_BP, REG_SI, REG_DI,
  REG_CS, REG_DS, REG_ES, REG_SS, REG_IP,
  ALU, FLAGS, DECODER, CONTROL_UNIT,
  ADDR_ADDER, INSTR_QUEUE, BUS_CONTROL,
  MEMORY_BLOCK, IO_BLOCK,
  INTERNAL_BUS_Y, INTERNAL_BUS_X1, INTERNAL_BUS_X2,
  ADDRESS_BUS_X, DATA_BUS_X, CONTROL_BUS_X,
  EXTERNAL_BUS_TOP, EXTERNAL_BUS_BOTTOM,
  COLORS,
} from '../models/diagram-layout.model';

enum ArrowDirection {
  Down = 'down',
  Up = 'up',
  Left = 'left',
  Right = 'right',
}

export class DiagramRenderer {
  /** Single uniform scale factor (virtual → CSS px), incl. the zoom multiplier. */
  private scale = 1;
  /** Centering offsets so the fitted diagram is letterboxed in the middle of the pane. */
  private offsetX = 0;
  private offsetY = 0;
  /** User zoom multiplier (1 = fit) and pan offset in CSS px. */
  private zoom = 1;
  private panX = 0;
  private panY = 0;

  /** Updates the interactive viewport (zoom + pan). The next render applies it. */
  public setViewport(zoom: number, panX: number, panY: number): void {
    this.zoom = zoom;
    this.panX = panX;
    this.panY = panY;
  }

  /** Base fit scale before the zoom multiplier — used by the host to map zoom focal points. */
  public getBaseScale(width: number, height: number): number {
    return Math.min(width / VIRTUAL_WIDTH, height / VIRTUAL_HEIGHT);
  }

  public render(ctx: CanvasRenderingContext2D, width: number, height: number, cpu?: CpuState): void {
    // Uniform fit (× zoom): preserve the virtual aspect ratio, apply the zoom
    // multiplier, and center within the pane (plus pan) so the diagram never
    // distorts or clips regardless of viewport shape.
    this.scale = this.getBaseScale(width, height) * this.zoom;
    this.offsetX = (width - VIRTUAL_WIDTH * this.scale) / 2 + this.panX;
    this.offsetY = (height - VIRTUAL_HEIGHT * this.scale) / 2 + this.panY;

    ctx.clearRect(0, 0, width, height);

    // Gradient background (fills the whole canvas, letterbox margins included)
    const bgGrad = ctx.createLinearGradient(0, 0, 0, height);
    bgGrad.addColorStop(0, COLORS.backgroundGradientTop);
    bgGrad.addColorStop(1, COLORS.backgroundGradientBottom);
    ctx.fillStyle = bgGrad;
    ctx.fillRect(0, 0, width, height);

    // Subtle grid pattern
    this.drawGrid(ctx, width, height);

    this.drawContainerBoxes(ctx);
    this.drawRegisters(ctx, cpu);
    this.drawComponentConnections(ctx);
    this.drawAlu(ctx);
    this.drawFlags(ctx, cpu);
    this.drawDecoder(ctx);
    this.drawControlUnit(ctx);
    this.drawAddrAdder(ctx);
    this.drawInstrQueue(ctx);
    this.drawBusControl(ctx);
    this.drawInternalBus(ctx);
    this.drawExternalBuses(ctx);
    this.drawMemoryIo(ctx);
    this.drawBusLabels(ctx);
  }

  /** Maps a virtual X coordinate to a centered CSS-px position. */
  private sx(v: number): number { return this.offsetX + v * this.scale; }
  /** Maps a virtual Y coordinate to a centered CSS-px position. */
  private sy(v: number): number { return this.offsetY + v * this.scale; }
  /** Scales a length/magnitude (no centering offset — for widths, radii, font sizes). */
  private sc(v: number): number { return v * this.scale; }

  private drawGrid(ctx: CanvasRenderingContext2D, width: number, height: number): void {
    ctx.save();
    ctx.strokeStyle = 'rgba(255, 255, 255, 0.015)';
    ctx.lineWidth = 1;
    const gridSize = this.sc(40);
    for (let x = 0; x < width; x += gridSize) {
      ctx.beginPath();
      ctx.moveTo(x, 0);
      ctx.lineTo(x, height);
      ctx.stroke();
    }
    for (let y = 0; y < height; y += gridSize) {
      ctx.beginPath();
      ctx.moveTo(0, y);
      ctx.lineTo(width, y);
      ctx.stroke();
    }
    ctx.restore();
  }

  private drawRoundedRect(
    ctx: CanvasRenderingContext2D,
    comp: DiagramComponent,
    fill: string,
    stroke: string,
    radius = 4,
  ): void {
    const x = this.sx(comp.x);
    const y = this.sy(comp.y);
    const w = this.sc(comp.width);
    const h = this.sc(comp.height);
    const r = this.sc(radius);

    ctx.beginPath();
    ctx.moveTo(x + r, y);
    ctx.lineTo(x + w - r, y);
    ctx.quadraticCurveTo(x + w, y, x + w, y + r);
    ctx.lineTo(x + w, y + h - r);
    ctx.quadraticCurveTo(x + w, y + h, x + w - r, y + h);
    ctx.lineTo(x + r, y + h);
    ctx.quadraticCurveTo(x, y + h, x, y + h - r);
    ctx.lineTo(x, y + r);
    ctx.quadraticCurveTo(x, y, x + r, y);
    ctx.closePath();

    ctx.fillStyle = fill;
    ctx.fill();
    ctx.strokeStyle = stroke;
    ctx.lineWidth = 1;
    ctx.stroke();
  }

  private drawGradientRoundedRect(
    ctx: CanvasRenderingContext2D,
    comp: DiagramComponent,
    gradTop: string,
    gradBottom: string,
    stroke: string,
    radius = 4,
  ): void {
    const x = this.sx(comp.x);
    const y = this.sy(comp.y);
    const w = this.sc(comp.width);
    const h = this.sc(comp.height);
    const r = this.sc(radius);

    ctx.beginPath();
    ctx.moveTo(x + r, y);
    ctx.lineTo(x + w - r, y);
    ctx.quadraticCurveTo(x + w, y, x + w, y + r);
    ctx.lineTo(x + w, y + h - r);
    ctx.quadraticCurveTo(x + w, y + h, x + w - r, y + h);
    ctx.lineTo(x + r, y + h);
    ctx.quadraticCurveTo(x, y + h, x, y + h - r);
    ctx.lineTo(x, y + r);
    ctx.quadraticCurveTo(x, y, x + r, y);
    ctx.closePath();

    const grad = ctx.createLinearGradient(x, y, x, y + h);
    grad.addColorStop(0, gradTop);
    grad.addColorStop(1, gradBottom);
    ctx.fillStyle = grad;
    ctx.fill();
    ctx.strokeStyle = stroke;
    ctx.lineWidth = 1;
    ctx.stroke();
  }

  private drawContainerBoxes(ctx: CanvasRenderingContext2D): void {
    // EU container with gradient and inner shadow
    this.drawGradientRoundedRect(ctx, EU_BOX, COLORS.euBoxGradientTop, COLORS.euBoxGradientBottom, COLORS.border, 8);
    // Inner glow on top edge
    ctx.save();
    ctx.globalAlpha = 0.08;
    const euHighlight = ctx.createLinearGradient(
      this.sx(EU_BOX.x), this.sy(EU_BOX.y),
      this.sx(EU_BOX.x), this.sy(EU_BOX.y + 60),
    );
    euHighlight.addColorStop(0, '#ffffff');
    euHighlight.addColorStop(1, 'transparent');
    ctx.fillStyle = euHighlight;
    ctx.fillRect(this.sx(EU_BOX.x + 1), this.sy(EU_BOX.y + 1), this.sc(EU_BOX.width - 2), this.sc(60));
    ctx.restore();

    ctx.fillStyle = COLORS.label;
    ctx.font = `bold ${this.sc(13)}px monospace`;
    ctx.textAlign = 'center';
    ctx.fillText(EU_BOX.label, this.sx(EU_BOX.x + EU_BOX.width / 2), this.sy(EU_BOX.y + 18));

    // BIU container with gradient
    this.drawGradientRoundedRect(ctx, BIU_BOX, COLORS.biuBoxGradientTop, COLORS.biuBoxGradientBottom, COLORS.border, 8);
    ctx.save();
    ctx.globalAlpha = 0.08;
    const biuHighlight = ctx.createLinearGradient(
      this.sx(BIU_BOX.x), this.sy(BIU_BOX.y),
      this.sx(BIU_BOX.x), this.sy(BIU_BOX.y + 60),
    );
    biuHighlight.addColorStop(0, '#ffffff');
    biuHighlight.addColorStop(1, 'transparent');
    ctx.fillStyle = biuHighlight;
    ctx.fillRect(this.sx(BIU_BOX.x + 1), this.sy(BIU_BOX.y + 1), this.sc(BIU_BOX.width - 2), this.sc(60));
    ctx.restore();

    ctx.fillStyle = COLORS.label;
    ctx.fillText(BIU_BOX.label, this.sx(BIU_BOX.x + BIU_BOX.width / 2), this.sy(BIU_BOX.y + 18));
  }

  private drawRegisters(ctx: CanvasRenderingContext2D, cpu?: CpuState): void {
    // Group labels with subtle style
    ctx.fillStyle = COLORS.textDim;
    ctx.font = `${this.sc(11)}px monospace`;
    ctx.textAlign = 'left';
    ctx.fillText('GENERAL REGISTERS', this.sx(50), this.sy(62));
    ctx.fillText('POINTER / INDEX', this.sx(250), this.sy(62));
    ctx.fillText('SEGMENT REGISTERS', this.sx(570), this.sy(62));

    const generalRegs: [DiagramComponent, string][] = [
      [REG_AX, 'AX'], [REG_BX, 'BX'], [REG_CX, 'CX'], [REG_DX, 'DX'],
    ];
    const ptrRegs: [DiagramComponent, string][] = [
      [REG_SP, 'SP'], [REG_BP, 'BP'], [REG_SI, 'SI'], [REG_DI, 'DI'],
    ];
    const segRegs: [DiagramComponent, string][] = [
      [REG_CS, 'CS'], [REG_DS, 'DS'], [REG_ES, 'ES'], [REG_SS, 'SS'],
    ];

    const regKey = (name: string): keyof CpuState => name.toLowerCase() as keyof CpuState;

    for (const [comp, name] of [...generalRegs, ...ptrRegs, ...segRegs]) {
      this.drawRegister(ctx, comp, name, cpu ? (cpu[regKey(name)] as number) : undefined);
    }

    // IP register
    this.drawRegister(ctx, REG_IP, 'IP', cpu?.ip);

    // Hi/lo byte split marker on the general registers (dashed mid-line only —
    // the AH/AL text is drawn inside each half so it can't collide with headers).
    for (const [comp, name] of generalRegs) {
      const midX = this.sx(comp.x + comp.width / 2);
      const topY = this.sy(comp.y);
      const botY = this.sy(comp.y + comp.height);
      ctx.strokeStyle = COLORS.borderSubtle;
      ctx.lineWidth = 1;
      ctx.setLineDash([2, 2]);
      ctx.beginPath();
      ctx.moveTo(midX, topY);
      ctx.lineTo(midX, botY);
      ctx.stroke();
      ctx.setLineDash([]);

      // Sub-register labels tucked into the top corner of each half.
      const prefix = name[0];
      ctx.fillStyle = COLORS.textDim;
      ctx.font = `${this.sc(10)}px monospace`;
      ctx.textAlign = 'left';
      ctx.fillText(`${prefix}H`, this.sx(comp.x + 3), this.sy(comp.y + 8));
      ctx.fillText(`${prefix}L`, this.sx(comp.x + comp.width / 2 + 3), this.sy(comp.y + 8));
    }
  }

  private drawRegister(
    ctx: CanvasRenderingContext2D,
    comp: DiagramComponent,
    name: string,
    value?: number,
  ): void {
    this.drawGradientRoundedRect(
      ctx, comp,
      COLORS.registerGradientTop, COLORS.registerGradientBottom,
      COLORS.border, 3,
    );

    // Inner highlight on top edge
    ctx.save();
    ctx.globalAlpha = 0.06;
    const hlY = this.sy(comp.y);
    const hlGrad = ctx.createLinearGradient(0, hlY, 0, hlY + this.sc(comp.height * 0.4));
    hlGrad.addColorStop(0, '#ffffff');
    hlGrad.addColorStop(1, 'transparent');
    ctx.fillStyle = hlGrad;
    ctx.fillRect(this.sx(comp.x + 1), hlY + 1, this.sc(comp.width - 2), this.sc(comp.height * 0.4));
    ctx.restore();

    ctx.font = `bold ${this.sc(12)}px monospace`;
    ctx.textAlign = 'left';
    ctx.fillStyle = COLORS.registerText;
    ctx.fillText(name, this.sx(comp.x + 6), this.sy(comp.y + comp.height / 2 + 4));

    if (value !== undefined) {
      ctx.textAlign = 'right';
      ctx.fillStyle = value === 0 ? COLORS.textDim : COLORS.text;
      ctx.font = `${this.sc(11)}px monospace`;
      const hex = value.toString(16).toUpperCase().padStart(4, '0');
      ctx.fillText(`0x${hex}`, this.sx(comp.x + comp.width - 6), this.sy(comp.y + comp.height / 2 + 4));
    }
  }

  private drawComponentConnections(ctx: CanvasRenderingContext2D): void {
    ctx.save();
    // Visible slate wiring so the internal data paths (ALU/Flags/Decoder/Control
    // Unit/Bus Control) read as connected rather than floating.
    ctx.strokeStyle = '#6b76a4';
    ctx.lineWidth = 1.5;
    ctx.globalAlpha = 0.7;
    ctx.setLineDash([4, 3]);

    // ── EU internal connections ──

    // Internal bus → ALU (vertical down from bus to ALU top)
    const aluCx = ALU.x + ALU.width / 2;
    ctx.beginPath();
    ctx.moveTo(this.sx(aluCx), this.sy(INTERNAL_BUS_Y));
    ctx.lineTo(this.sx(aluCx), this.sy(ALU.y));
    ctx.stroke();

    // ALU → Flags (vertical)
    const aluBottom = ALU.y + ALU.height;
    const flagsTop = FLAGS.y;
    ctx.beginPath();
    ctx.moveTo(this.sx(aluCx), this.sy(aluBottom));
    ctx.lineTo(this.sx(aluCx), this.sy(flagsTop));
    ctx.stroke();

    // Flags → Decoder
    const flagsBottom = FLAGS.y + FLAGS.height;
    const decoderTop = DECODER.y;
    ctx.beginPath();
    ctx.moveTo(this.sx(aluCx), this.sy(flagsBottom));
    ctx.lineTo(this.sx(aluCx), this.sy(decoderTop));
    ctx.stroke();

    // Decoder → Control Unit
    const decoderBottom = DECODER.y + DECODER.height;
    const ctrlTop = CONTROL_UNIT.y;
    ctx.beginPath();
    ctx.moveTo(this.sx(aluCx), this.sy(decoderBottom));
    ctx.lineTo(this.sx(aluCx), this.sy(ctrlTop));
    ctx.stroke();

    // ── BIU internal connections ──

    // Internal bus → Address Adder (vertical down from bus)
    const adderCx = ADDR_ADDER.x + ADDR_ADDER.width / 2;
    ctx.beginPath();
    ctx.moveTo(this.sx(adderCx), this.sy(INTERNAL_BUS_Y));
    ctx.lineTo(this.sx(adderCx), this.sy(ADDR_ADDER.y));
    ctx.stroke();

    // Address Adder → Instruction Queue
    const adderBottom = ADDR_ADDER.y + ADDR_ADDER.height;
    const queueTop = INSTR_QUEUE.y;
    ctx.beginPath();
    ctx.moveTo(this.sx(adderCx), this.sy(adderBottom));
    ctx.lineTo(this.sx(adderCx), this.sy(queueTop));
    ctx.stroke();

    // Instruction Queue → Bus Control
    const queueBottom = INSTR_QUEUE.y + INSTR_QUEUE.height;
    const busCtrlTop = BUS_CONTROL.y;
    const queueCx = INSTR_QUEUE.x + INSTR_QUEUE.width / 2;
    ctx.beginPath();
    ctx.moveTo(this.sx(queueCx), this.sy(queueBottom));
    ctx.lineTo(this.sx(queueCx), this.sy(busCtrlTop));
    ctx.stroke();

    // ── BIU → EU hand-off: Instruction Queue → Decoder (fetched bytes cross over) ──
    const queueLeftX = INSTR_QUEUE.x;
    const queueMidY = INSTR_QUEUE.y + INSTR_QUEUE.height / 2;
    const decoderRightX = DECODER.x + DECODER.width;
    const decoderMidY = DECODER.y + DECODER.height / 2;
    const queueCrossX = (decoderRightX + queueLeftX) / 2;
    ctx.beginPath();
    ctx.moveTo(this.sx(queueLeftX), this.sy(queueMidY));
    ctx.lineTo(this.sx(queueCrossX), this.sy(queueMidY));
    ctx.lineTo(this.sx(queueCrossX), this.sy(decoderMidY));
    ctx.lineTo(this.sx(decoderRightX), this.sy(decoderMidY));
    ctx.stroke();

    // ── Control Unit → Bus Control (control signals coordinate the bus) ──
    const ctrlRightX = CONTROL_UNIT.x + CONTROL_UNIT.width;
    const ctrlMidY = CONTROL_UNIT.y + CONTROL_UNIT.height / 2;
    const busCtrlLeftX = BUS_CONTROL.x;
    const busCtrlMidY = BUS_CONTROL.y + BUS_CONTROL.height / 2;
    const ctrlCrossX = (ctrlRightX + busCtrlLeftX) / 2;
    ctx.beginPath();
    ctx.moveTo(this.sx(ctrlRightX), this.sy(ctrlMidY));
    ctx.lineTo(this.sx(ctrlCrossX), this.sy(ctrlMidY));
    ctx.lineTo(this.sx(ctrlCrossX), this.sy(busCtrlMidY));
    ctx.lineTo(this.sx(busCtrlLeftX), this.sy(busCtrlMidY));
    ctx.stroke();

    // ── BIU → External buses (Bus Control down to external bus top) ──
    // Routed orthogonally: drop from Bus Control to a horizontal manifold, run
    // across, then drop straight into each bus top (right angles only).
    const busCtrlBottom = BUS_CONTROL.y + BUS_CONTROL.height;
    const busCtrlCx = BUS_CONTROL.x + BUS_CONTROL.width / 2;
    const manifoldY = (busCtrlBottom + EXTERNAL_BUS_TOP) / 2;

    // Drop from Bus Control down to the manifold
    ctx.beginPath();
    ctx.moveTo(this.sx(busCtrlCx), this.sy(busCtrlBottom));
    ctx.lineTo(this.sx(busCtrlCx), this.sy(manifoldY));
    ctx.stroke();

    // Horizontal manifold spanning all three external-bus columns
    ctx.beginPath();
    ctx.moveTo(this.sx(ADDRESS_BUS_X), this.sy(manifoldY));
    ctx.lineTo(this.sx(CONTROL_BUS_X), this.sy(manifoldY));
    ctx.stroke();

    // Drop straight down from the manifold into each external bus top
    for (const busX of [ADDRESS_BUS_X, DATA_BUS_X, CONTROL_BUS_X]) {
      ctx.beginPath();
      ctx.moveTo(this.sx(busX), this.sy(manifoldY));
      ctx.lineTo(this.sx(busX), this.sy(EXTERNAL_BUS_TOP));
      ctx.stroke();
    }

    // ── Arrow tips ──
    ctx.setLineDash([]);
    ctx.globalAlpha = 0.8;
    this.drawArrowTip(ctx, this.sx(aluCx), this.sy(ALU.y), ArrowDirection.Down);
    this.drawArrowTip(ctx, this.sx(aluCx), this.sy(flagsTop), ArrowDirection.Down);
    this.drawArrowTip(ctx, this.sx(aluCx), this.sy(decoderTop), ArrowDirection.Down);
    this.drawArrowTip(ctx, this.sx(aluCx), this.sy(ctrlTop), ArrowDirection.Down);
    this.drawArrowTip(ctx, this.sx(adderCx), this.sy(ADDR_ADDER.y), ArrowDirection.Down);
    this.drawArrowTip(ctx, this.sx(adderCx), this.sy(queueTop), ArrowDirection.Down);
    this.drawArrowTip(ctx, this.sx(queueCx), this.sy(busCtrlTop), ArrowDirection.Down);
    this.drawArrowTip(ctx, this.sx(DATA_BUS_X), this.sy(EXTERNAL_BUS_TOP), ArrowDirection.Down);
    this.drawArrowTip(ctx, this.sx(ADDRESS_BUS_X), this.sy(EXTERNAL_BUS_TOP), ArrowDirection.Down);
    this.drawArrowTip(ctx, this.sx(CONTROL_BUS_X), this.sy(EXTERNAL_BUS_TOP), ArrowDirection.Down);
    this.drawArrowTip(ctx, this.sx(decoderRightX), this.sy(decoderMidY), ArrowDirection.Left);
    this.drawArrowTip(ctx, this.sx(busCtrlLeftX), this.sy(busCtrlMidY), ArrowDirection.Right);

    ctx.restore();
  }

  private drawArrowTip(ctx: CanvasRenderingContext2D, x: number, y: number, direction: ArrowDirection): void {
    const size = this.sc(4);
    ctx.beginPath();
    switch (direction) {
      case ArrowDirection.Down:
        ctx.moveTo(x - size, y - size);
        ctx.lineTo(x, y);
        ctx.lineTo(x + size, y - size);
        break;
      case ArrowDirection.Up:
        ctx.moveTo(x - size, y + size);
        ctx.lineTo(x, y);
        ctx.lineTo(x + size, y + size);
        break;
      case ArrowDirection.Right:
        ctx.moveTo(x - size, y - size);
        ctx.lineTo(x, y);
        ctx.lineTo(x - size, y + size);
        break;
      case ArrowDirection.Left:
        ctx.moveTo(x + size, y - size);
        ctx.lineTo(x, y);
        ctx.lineTo(x + size, y + size);
        break;
    }
    ctx.stroke();
  }

  private drawAlu(ctx: CanvasRenderingContext2D): void {
    // Draw ALU as trapezoid with gradient
    const x = this.sx(ALU.x);
    const y = this.sy(ALU.y);
    const w = this.sc(ALU.width);
    const h = this.sc(ALU.height);
    const inset = this.sc(30);

    ctx.beginPath();
    ctx.moveTo(x + inset, y);
    ctx.lineTo(x + w - inset, y);
    ctx.lineTo(x + w, y + h);
    ctx.lineTo(x, y + h);
    ctx.closePath();

    const aluGrad = ctx.createLinearGradient(x, y, x, y + h);
    aluGrad.addColorStop(0, COLORS.aluGradientTop);
    aluGrad.addColorStop(1, COLORS.aluGradientBottom);
    ctx.fillStyle = aluGrad;
    ctx.fill();
    ctx.strokeStyle = COLORS.border;
    ctx.lineWidth = 1.5;
    ctx.stroke();

    // Inner highlight
    ctx.save();
    ctx.globalAlpha = 0.08;
    ctx.beginPath();
    ctx.moveTo(x + inset + 2, y + 2);
    ctx.lineTo(x + w - inset - 2, y + 2);
    ctx.lineTo(x + w - 2, y + h - 2);
    ctx.lineTo(x + 2, y + h - 2);
    ctx.closePath();
    const aluHL = ctx.createLinearGradient(x, y, x, y + h * 0.4);
    aluHL.addColorStop(0, '#ffffff');
    aluHL.addColorStop(1, 'transparent');
    ctx.fillStyle = aluHL;
    ctx.fill();
    ctx.restore();

    ctx.fillStyle = COLORS.textBright;
    ctx.font = `bold ${this.sc(16)}px monospace`;
    ctx.textAlign = 'center';
    ctx.fillText('ALU', this.sx(ALU.x + ALU.width / 2), this.sy(ALU.y + ALU.height / 2 + 6));

    // Tiny +/- icons
    ctx.fillStyle = COLORS.textDim;
    ctx.font = `${this.sc(12)}px monospace`;
    ctx.fillText('+ − × ÷', this.sx(ALU.x + ALU.width / 2), this.sy(ALU.y + ALU.height / 2 + 18));
  }

  private drawFlags(ctx: CanvasRenderingContext2D, cpu?: CpuState): void {
    this.drawGradientRoundedRect(ctx, FLAGS, '#3d583d', '#293c29', COLORS.border, 3);

    // Draw individual flag indicators
    const flagNames = ['ZF', 'CF', 'SF', 'OF'];
    const flagValues = cpu?.flags
      ? [cpu.flags.zero, cpu.flags.carry, cpu.flags.sign, cpu.flags.overflow]
      : [false, false, false, false];

    const cellWidth = FLAGS.width / flagNames.length;
    for (let i = 0; i < flagNames.length; i++) {
      const cx = FLAGS.x + cellWidth * i + cellWidth / 2;
      const cy = FLAGS.y + FLAGS.height / 2;

      // Active flag indicator dot
      if (flagValues[i]) {
        ctx.save();
        ctx.fillStyle = COLORS.flagsActive;
        ctx.shadowColor = COLORS.flagsActive;
        ctx.shadowBlur = 6;
        ctx.beginPath();
        ctx.arc(this.sx(cx - 12), this.sy(cy), this.sc(3), 0, Math.PI * 2);
        ctx.fill();
        ctx.restore();
      }

      ctx.fillStyle = flagValues[i] ? COLORS.textBright : COLORS.textDim;
      ctx.font = `${flagValues[i] ? 'bold' : 'normal'} ${this.sc(12)}px monospace`;
      ctx.textAlign = 'center';
      ctx.fillText(flagNames[i], this.sx(cx), this.sy(cy + 4));
    }
  }

  private drawDecoder(ctx: CanvasRenderingContext2D): void {
    this.drawRoundedRect(ctx, DECODER, COLORS.decoder, COLORS.border, 3);
    ctx.fillStyle = COLORS.text;
    ctx.font = `bold ${this.sc(11)}px monospace`;
    ctx.textAlign = 'center';
    ctx.fillText(DECODER.label, this.sx(DECODER.x + DECODER.width / 2), this.sy(DECODER.y + DECODER.height / 2 + 4));
  }

  private drawControlUnit(ctx: CanvasRenderingContext2D): void {
    this.drawRoundedRect(ctx, CONTROL_UNIT, COLORS.decoder, COLORS.border, 3);
    ctx.fillStyle = COLORS.text;
    ctx.font = `bold ${this.sc(11)}px monospace`;
    ctx.textAlign = 'center';
    ctx.fillText(CONTROL_UNIT.label, this.sx(CONTROL_UNIT.x + CONTROL_UNIT.width / 2), this.sy(CONTROL_UNIT.y + CONTROL_UNIT.height / 2 + 4));
  }

  private drawAddrAdder(ctx: CanvasRenderingContext2D): void {
    // Draw as circle with gradient and Σ. Radius is bound to the box height so the
    // circle stays inside its bounds, leaving room for the bus line above/below.
    const cx = this.sx(ADDR_ADDER.x + ADDR_ADDER.width / 2);
    const cy = this.sy(ADDR_ADDER.y + ADDR_ADDER.height / 2);
    const r = this.sc(ADDR_ADDER.height / 2);

    // Vertical bus line running through the adder column (internal bus ↕ queue),
    // drawn first so the circle sits on top of its middle section.
    ctx.save();
    ctx.strokeStyle = COLORS.busIdle;
    ctx.lineWidth = 2;
    ctx.beginPath();
    ctx.moveTo(cx, this.sy(INTERNAL_BUS_Y));
    ctx.lineTo(cx, this.sy(INSTR_QUEUE.y));
    ctx.stroke();
    ctx.restore();

    const circGrad = ctx.createRadialGradient(cx - r * 0.3, cy - r * 0.3, 0, cx, cy, r);
    circGrad.addColorStop(0, COLORS.aluGradientTop);
    circGrad.addColorStop(1, COLORS.aluGradientBottom);

    ctx.beginPath();
    ctx.arc(cx, cy, r, 0, Math.PI * 2);
    ctx.fillStyle = circGrad;
    ctx.fill();
    ctx.strokeStyle = COLORS.border;
    ctx.lineWidth = 1.5;
    ctx.stroke();

    ctx.fillStyle = COLORS.textBright;
    ctx.font = `bold ${this.sc(24)}px serif`;
    ctx.textAlign = 'center';
    ctx.fillText('Σ', cx, cy + this.sc(8));

    // Label
    ctx.fillStyle = COLORS.textDim;
    ctx.font = `${this.sc(11)}px monospace`;
    ctx.fillText('ADDRESS ADDER', cx, this.sy(ADDR_ADDER.y + ADDR_ADDER.height + 14));
  }

  private drawInstrQueue(ctx: CanvasRenderingContext2D): void {
    this.drawGradientRoundedRect(
      ctx, INSTR_QUEUE,
      COLORS.registerGradientTop, COLORS.registerGradientBottom,
      COLORS.border, 3,
    );

    // Draw 6 cells with alternating subtle shade
    const cellWidth = INSTR_QUEUE.width / 6;
    for (let i = 0; i < 6; i++) {
      if (i % 2 === 1) {
        ctx.save();
        ctx.globalAlpha = 0.05;
        ctx.fillStyle = '#ffffff';
        ctx.fillRect(
          this.sx(INSTR_QUEUE.x + cellWidth * i),
          this.sy(INSTR_QUEUE.y),
          this.sc(cellWidth),
          this.sc(INSTR_QUEUE.height),
        );
        ctx.restore();
      }
      if (i > 0) {
        const x = this.sx(INSTR_QUEUE.x + cellWidth * i);
        ctx.strokeStyle = COLORS.borderSubtle;
        ctx.lineWidth = 0.5;
        ctx.beginPath();
        ctx.moveTo(x, this.sy(INSTR_QUEUE.y));
        ctx.lineTo(x, this.sy(INSTR_QUEUE.y + INSTR_QUEUE.height));
        ctx.stroke();
      }

      // Cell byte index
      ctx.fillStyle = COLORS.textDim;
      ctx.font = `${this.sc(10)}px monospace`;
      ctx.textAlign = 'center';
      ctx.fillText(
        `${i}`,
        this.sx(INSTR_QUEUE.x + cellWidth * i + cellWidth / 2),
        this.sy(INSTR_QUEUE.y + INSTR_QUEUE.height / 2 + 3),
      );
    }

    // Label
    ctx.fillStyle = COLORS.textDim;
    ctx.font = `${this.sc(11)}px monospace`;
    ctx.textAlign = 'center';
    ctx.fillText('INSTRUCTION QUEUE (6 BYTES)',
      this.sx(INSTR_QUEUE.x + INSTR_QUEUE.width / 2), this.sy(INSTR_QUEUE.y - 4));

    // Direction arrow on right side
    ctx.save();
    ctx.strokeStyle = COLORS.textDim;
    ctx.lineWidth = 1;
    const arrowY = this.sy(INSTR_QUEUE.y + INSTR_QUEUE.height / 2);
    const arrowStartX = this.sx(INSTR_QUEUE.x + INSTR_QUEUE.width + 6);
    const arrowEndX = this.sx(INSTR_QUEUE.x + INSTR_QUEUE.width + 18);
    ctx.beginPath();
    ctx.moveTo(arrowStartX, arrowY);
    ctx.lineTo(arrowEndX, arrowY);
    ctx.stroke();
    this.drawArrowTip(ctx, arrowStartX, arrowY, ArrowDirection.Left);
    ctx.restore();
  }

  private drawBusControl(ctx: CanvasRenderingContext2D): void {
    this.drawRoundedRect(ctx, BUS_CONTROL, COLORS.decoder, COLORS.border, 3);
    ctx.fillStyle = COLORS.text;
    ctx.font = `bold ${this.sc(11)}px monospace`;
    ctx.textAlign = 'center';
    ctx.fillText(BUS_CONTROL.label, this.sx(BUS_CONTROL.x + BUS_CONTROL.width / 2), this.sy(BUS_CONTROL.y + BUS_CONTROL.height / 2 + 4));
  }

  private drawInternalBus(ctx: CanvasRenderingContext2D): void {
    const y = this.sy(INTERNAL_BUS_Y);
    const x1 = this.sx(INTERNAL_BUS_X1);
    const x2 = this.sx(INTERNAL_BUS_X2);

    // Bus glow (subtle)
    ctx.save();
    ctx.shadowColor = COLORS.busInternalGlow;
    ctx.shadowBlur = 5;
    ctx.strokeStyle = COLORS.busInternal;
    ctx.lineWidth = 2.5;
    ctx.beginPath();
    ctx.moveTo(x1, y);
    ctx.lineTo(x2, y);
    ctx.stroke();
    ctx.restore();

    // Secondary thin line for double-bus effect
    ctx.strokeStyle = 'rgba(192, 120, 214, 0.15)';
    ctx.lineWidth = 6;
    ctx.beginPath();
    ctx.moveTo(x1, y);
    ctx.lineTo(x2, y);
    ctx.stroke();

    // Bus label
    ctx.fillStyle = COLORS.busInternal;
    ctx.font = `bold ${this.sc(11)}px monospace`;
    ctx.textAlign = 'center';
    ctx.fillText('INTERNAL BUS', this.sx((INTERNAL_BUS_X1 + INTERNAL_BUS_X2) / 2), y - this.sc(8));

    // Bidirectional arrows
    ctx.save();
    ctx.strokeStyle = COLORS.busInternal;
    ctx.lineWidth = 1;
    ctx.globalAlpha = 0.6;
    const arrowMargin = this.sc(20);
    this.drawArrowTip(ctx, x1 + arrowMargin, y, ArrowDirection.Left);
    this.drawArrowTip(ctx, x2 - arrowMargin, y, ArrowDirection.Right);
    ctx.restore();

    // Draw connection stubs from registers down to internal bus
    ctx.lineWidth = 1;
    ctx.globalAlpha = 0.3;

    const stubRegs = [REG_AX, REG_BX, REG_CX, REG_DX, REG_SP, REG_BP, REG_SI, REG_DI];
    for (const reg of stubRegs) {
      const cx = this.sx(reg.x + reg.width / 2);
      const bot = this.sy(reg.y + reg.height);

      // Gradient stub
      const stubGrad = ctx.createLinearGradient(0, bot, 0, y);
      stubGrad.addColorStop(0, 'rgba(192, 120, 214, 0.05)');
      stubGrad.addColorStop(1, 'rgba(192, 120, 214, 0.3)');
      ctx.strokeStyle = stubGrad;
      ctx.beginPath();
      ctx.moveTo(cx, bot);
      ctx.lineTo(cx, y);
      ctx.stroke();
    }

    // Stubs from segment regs and IP to internal bus
    const biuRegs = [REG_CS, REG_DS, REG_ES, REG_SS, REG_IP];
    for (const reg of biuRegs) {
      const cx = this.sx(reg.x + reg.width / 2);
      const bot = this.sy(reg.y + reg.height);

      const stubGrad = ctx.createLinearGradient(0, bot, 0, y);
      stubGrad.addColorStop(0, 'rgba(192, 120, 214, 0.05)');
      stubGrad.addColorStop(1, 'rgba(192, 120, 214, 0.3)');
      ctx.strokeStyle = stubGrad;
      ctx.beginPath();
      ctx.moveTo(cx, bot);
      ctx.lineTo(cx, y);
      ctx.stroke();
    }

    ctx.globalAlpha = 1;
  }

  private drawExternalBuses(ctx: CanvasRenderingContext2D): void {
    const topY = this.sy(EXTERNAL_BUS_TOP);
    const botY = this.sy(EXTERNAL_BUS_BOTTOM);

    // Draw each bus with a subtle glow
    const buses: [number, string, string][] = [
      [ADDRESS_BUS_X, COLORS.busAddress, COLORS.busAddressGlow],
      [DATA_BUS_X, COLORS.busData, COLORS.busDataGlow],
      [CONTROL_BUS_X, COLORS.busControl, COLORS.busControlGlow],
    ];

    for (const [busX, color, glow] of buses) {
      const x = this.sx(busX);

      // Wide glow
      ctx.save();
      ctx.strokeStyle = glow;
      ctx.lineWidth = 6;
      ctx.beginPath();
      ctx.moveTo(x, topY);
      ctx.lineTo(x, botY);
      ctx.stroke();
      ctx.restore();

      // Core line
      ctx.save();
      ctx.shadowColor = glow;
      ctx.shadowBlur = 4;
      ctx.strokeStyle = color;
      ctx.lineWidth = 2.5;
      ctx.beginPath();
      ctx.moveTo(x, topY);
      ctx.lineTo(x, botY);
      ctx.stroke();
      ctx.restore();

      // Arrow tip at bottom
      ctx.save();
      ctx.strokeStyle = color;
      ctx.lineWidth = 1.5;
      this.drawArrowTip(ctx, x, botY, ArrowDirection.Down);
      ctx.restore();
    }

    // ── System-bus rail tying the three buses into Memory and I/O ──
    const memCx = this.sx(MEMORY_BLOCK.x + MEMORY_BLOCK.width / 2);
    const ioCx = this.sx(IO_BLOCK.x + IO_BLOCK.width / 2);
    const railLeft = Math.min(memCx, this.sx(ADDRESS_BUS_X));
    const railRight = Math.max(ioCx, this.sx(CONTROL_BUS_X));

    ctx.save();
    ctx.strokeStyle = COLORS.busIdle;
    ctx.lineWidth = 2;
    ctx.beginPath();
    ctx.moveTo(railLeft, botY);
    ctx.lineTo(railRight, botY);
    ctx.stroke();

    // Short drops from the rail down into each external block, with arrowheads.
    const blockTopY = this.sy(MEMORY_BLOCK.y);
    for (const cx of [memCx, ioCx]) {
      ctx.beginPath();
      ctx.moveTo(cx, botY);
      ctx.lineTo(cx, blockTopY);
      ctx.stroke();
      this.drawArrowTip(ctx, cx, blockTopY, ArrowDirection.Down);
    }
    ctx.restore();
  }

  private drawMemoryIo(ctx: CanvasRenderingContext2D): void {
    // Memory block with gradient
    this.drawGradientRoundedRect(
      ctx, MEMORY_BLOCK,
      COLORS.memoryGradientTop, COLORS.memoryGradientBottom,
      COLORS.border, 6,
    );
    ctx.fillStyle = COLORS.textBright;
    ctx.font = `bold ${this.sc(14)}px monospace`;
    ctx.textAlign = 'center';
    ctx.fillText(MEMORY_BLOCK.label,
      this.sx(MEMORY_BLOCK.x + MEMORY_BLOCK.width / 2),
      this.sy(MEMORY_BLOCK.y + MEMORY_BLOCK.height / 2 + 5));

    // Tiny memory indicator
    ctx.fillStyle = COLORS.textDim;
    ctx.font = `${this.sc(10)}px monospace`;
    ctx.fillText('1MB Address Space',
      this.sx(MEMORY_BLOCK.x + MEMORY_BLOCK.width / 2),
      this.sy(MEMORY_BLOCK.y + MEMORY_BLOCK.height + 12));

    // I/O block with gradient
    this.drawGradientRoundedRect(
      ctx, IO_BLOCK,
      COLORS.ioGradientTop, COLORS.ioGradientBottom,
      COLORS.border, 6,
    );
    ctx.fillStyle = COLORS.textBright;
    ctx.font = `bold ${this.sc(14)}px monospace`;
    ctx.fillText(IO_BLOCK.label,
      this.sx(IO_BLOCK.x + IO_BLOCK.width / 2),
      this.sy(IO_BLOCK.y + IO_BLOCK.height / 2 + 5));

    // I/O port indicator
    ctx.fillStyle = COLORS.textDim;
    ctx.font = `${this.sc(10)}px monospace`;
    ctx.fillText('INT 21h',
      this.sx(IO_BLOCK.x + IO_BLOCK.width / 2),
      this.sy(IO_BLOCK.y + IO_BLOCK.height + 12));
  }

  private drawBusLabels(ctx: CanvasRenderingContext2D): void {
    // Sit the labels inside the empty bus corridor (below the bus tops) so they
    // clear the Control Unit box above; a dark pill keeps them legible over the
    // bus lines they annotate.
    const y = this.sy(EXTERNAL_BUS_TOP + 18);
    ctx.font = `bold ${this.sc(11)}px monospace`;
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';

    const labels: [number, string, string][] = [
      [ADDRESS_BUS_X, 'ADDRESS BUS · 20-bit', COLORS.busAddress],
      [DATA_BUS_X, 'DATA BUS · 16-bit', COLORS.busData],
      [CONTROL_BUS_X, 'CONTROL BUS', COLORS.busControl],
    ];

    const padX = this.sc(5);
    const pillH = this.sc(14);
    for (const [busX, text, color] of labels) {
      const cx = this.sx(busX);
      const tw = ctx.measureText(text).width;
      ctx.fillStyle = 'rgba(20, 22, 34, 0.85)';
      this.fillRoundedRectPx(ctx, cx - tw / 2 - padX, y - pillH / 2, tw + padX * 2, pillH, this.sc(3));
      ctx.fillStyle = color;
      ctx.fillText(text, cx, y);
    }

    ctx.textBaseline = 'alphabetic';
  }

  /** Fills a rounded rectangle given already-scaled CSS-px coordinates. */
  private fillRoundedRectPx(
    ctx: CanvasRenderingContext2D,
    x: number, y: number, w: number, h: number, r: number,
  ): void {
    ctx.beginPath();
    ctx.moveTo(x + r, y);
    ctx.lineTo(x + w - r, y);
    ctx.quadraticCurveTo(x + w, y, x + w, y + r);
    ctx.lineTo(x + w, y + h - r);
    ctx.quadraticCurveTo(x + w, y + h, x + w - r, y + h);
    ctx.lineTo(x + r, y + h);
    ctx.quadraticCurveTo(x, y + h, x, y + h - r);
    ctx.lineTo(x, y + r);
    ctx.quadraticCurveTo(x, y, x + r, y);
    ctx.closePath();
    ctx.fill();
  }

  // ── Highlight methods called by AnimationRenderer ──

  public highlightComponent(
    ctx: CanvasRenderingContext2D,
    comp: DiagramComponent,
    color: string,
  ): void {
    // The Address Adder is drawn as a circle, so highlight it with a matching ring.
    if (comp.id === ADDR_ADDER.id) {
      const cx = this.sx(comp.x + comp.width / 2);
      const cy = this.sy(comp.y + comp.height / 2);
      const r = this.sc(comp.height / 2) + 4;

      ctx.save();
      ctx.shadowColor = color;
      ctx.shadowBlur = 10;
      ctx.strokeStyle = color;
      ctx.lineWidth = 2;
      ctx.beginPath();
      ctx.arc(cx, cy, r, 0, Math.PI * 2);
      ctx.stroke();
      ctx.restore();

      ctx.save();
      ctx.globalAlpha = 0.08;
      ctx.fillStyle = color;
      ctx.beginPath();
      ctx.arc(cx, cy, r, 0, Math.PI * 2);
      ctx.fill();
      ctx.restore();
      return;
    }

    const x = this.sx(comp.x) - 3;
    const y = this.sy(comp.y) - 3;
    const w = this.sc(comp.width) + 6;
    const h = this.sc(comp.height) + 6;

    // Outer glow
    ctx.save();
    ctx.shadowColor = color;
    ctx.shadowBlur = 10;
    ctx.strokeStyle = color;
    ctx.lineWidth = 2;
    ctx.strokeRect(x, y, w, h);
    ctx.restore();

    // Inner fill overlay
    ctx.save();
    ctx.globalAlpha = 0.08;
    ctx.fillStyle = color;
    ctx.fillRect(x, y, w, h);
    ctx.restore();
  }

  public getScaledBusPoint(virtualX: number, virtualY: number): { x: number; y: number } {
    return { x: this.sx(virtualX), y: this.sy(virtualY) };
  }

  public getScaleX(): number { return this.scale; }
  public getScaleY(): number { return this.scale; }

  /** Hit test a CSS pixel coordinate against diagram components. Returns tooltip info or null. */
  public hitTest(cssX: number, cssY: number): DiagramTooltip | null {
    for (const entry of TOOLTIP_ENTRIES) {
      const comp = entry.component;
      const x = this.sx(comp.x);
      const y = this.sy(comp.y);
      const w = this.sc(comp.width);
      const h = this.sc(comp.height);

      if (cssX >= x && cssX <= x + w && cssY >= y && cssY <= y + h) {
        return { label: entry.label, description: entry.description };
      }
    }
    return null;
  }
}

export interface DiagramTooltip {
  label: string;
  description: string;
}

interface TooltipEntry {
  component: DiagramComponent;
  label: string;
  description: string;
}

const TOOLTIP_ENTRIES: TooltipEntry[] = [
  { component: REG_AX, label: 'AX (Accumulator)', description: 'Primary register for arithmetic, I/O, and interrupt operations. Split into AH (high byte) and AL (low byte).' },
  { component: REG_BX, label: 'BX (Base)', description: 'General purpose register often used as a base pointer for memory addressing.' },
  { component: REG_CX, label: 'CX (Count)', description: 'Counter register used by LOOP, REP, and shift/rotate instructions.' },
  { component: REG_DX, label: 'DX (Data)', description: 'Data register used for I/O port addressing and multiply/divide operations.' },
  { component: REG_SP, label: 'SP (Stack Pointer)', description: 'Points to the top of the stack. Modified by PUSH, POP, CALL, and RET.' },
  { component: REG_BP, label: 'BP (Base Pointer)', description: 'Used to reference parameters and local variables on the stack frame.' },
  { component: REG_SI, label: 'SI (Source Index)', description: 'Source index for string operations with MOVSB/MOVSW.' },
  { component: REG_DI, label: 'DI (Destination Index)', description: 'Destination index for string operations with MOVSB/MOVSW.' },
  { component: REG_CS, label: 'CS (Code Segment)', description: 'Points to the segment containing the currently executing code.' },
  { component: REG_DS, label: 'DS (Data Segment)', description: 'Default segment for MOV and most data access operations.' },
  { component: REG_ES, label: 'ES (Extra Segment)', description: 'Extra data segment, destination for string operations.' },
  { component: REG_SS, label: 'SS (Stack Segment)', description: 'Segment for the stack. Used with SP and BP for stack access.' },
  { component: REG_IP, label: 'IP (Instruction Pointer)', description: 'Holds the offset of the next instruction to be fetched from the code segment.' },
  { component: ALU, label: 'ALU (Arithmetic Logic Unit)', description: 'Performs arithmetic (+, −, ×, ÷) and logical (AND, OR, XOR, NOT) operations. Updates flags register.' },
  { component: FLAGS, label: 'Flags Register', description: 'Status flags set by ALU: ZF (Zero), CF (Carry), SF (Sign), OF (Overflow).' },
  { component: DECODER, label: 'Instruction Decoder', description: 'Decodes fetched instruction bytes into micro-operations for the control unit.' },
  { component: CONTROL_UNIT, label: 'Control Unit', description: 'Generates control signals to coordinate all CPU components during instruction execution.' },
  { component: ADDR_ADDER, label: 'Address Adder (Σ)', description: 'Combines segment base (×16) with offset to produce a 20-bit physical address.' },
  { component: INSTR_QUEUE, label: 'Instruction Queue', description: '6-byte prefetch queue. The BIU fetches ahead while the EU executes, improving throughput.' },
  { component: BUS_CONTROL, label: 'Bus Control Logic', description: 'Manages bus arbitration, read/write signals, and memory/IO select lines.' },
  { component: MEMORY_BLOCK, label: 'Memory (1MB)', description: 'The 8086 can address up to 1MB (20-bit address bus). Contains code, data, and stack segments.' },
  { component: IO_BLOCK, label: 'I/O Ports', description: 'Communicates with peripherals via IN/OUT instructions and INT 21h DOS services.' },
];

export interface Point {
  x: number;
  y: number;
}

export interface DiagramComponent {
  id: string;
  label: string;
  x: number;
  y: number;
  width: number;
  height: number;
}

export enum BusType {
  Data = 'data',
  Address = 'address',
  Control = 'control',
  Internal = 'internal',
}

export interface DiagramConnection {
  from: string;
  to: string;
  path: Point[];
  busType: BusType;
}

// ── Proportional layout (1000x800 virtual canvas, scaled to fit) ──

// Execution Unit (EU) — left half
export const EU_BOX: DiagramComponent = { id: 'eu', label: 'Execution Unit (EU)', x: 20, y: 30, width: 440, height: 650 };

// General-purpose registers inside EU
export const REG_AX: DiagramComponent = { id: 'ax', label: 'AX', x: 50, y: 70, width: 160, height: 32 };
export const REG_BX: DiagramComponent = { id: 'bx', label: 'BX', x: 50, y: 110, width: 160, height: 32 };
export const REG_CX: DiagramComponent = { id: 'cx', label: 'CX', x: 50, y: 150, width: 160, height: 32 };
export const REG_DX: DiagramComponent = { id: 'dx', label: 'DX', x: 50, y: 190, width: 160, height: 32 };

// Sub-register labels (AH/AL etc.) share same box, drawn as split
export const REG_AH: DiagramComponent = { id: 'ah', label: 'AH', x: 50, y: 70, width: 80, height: 32 };
export const REG_AL: DiagramComponent = { id: 'al', label: 'AL', x: 130, y: 70, width: 80, height: 32 };

// Pointer/Index registers
export const REG_SP: DiagramComponent = { id: 'sp', label: 'SP', x: 250, y: 70, width: 160, height: 32 };
export const REG_BP: DiagramComponent = { id: 'bp', label: 'BP', x: 250, y: 110, width: 160, height: 32 };
export const REG_SI: DiagramComponent = { id: 'si', label: 'SI', x: 250, y: 150, width: 160, height: 32 };
export const REG_DI: DiagramComponent = { id: 'di', label: 'DI', x: 250, y: 190, width: 160, height: 32 };

// ALU (trapezoid drawn as rectangle for simplicity, rendered specially)
export const ALU: DiagramComponent = { id: 'alu', label: 'ALU', x: 120, y: 300, width: 220, height: 80 };

// Flags register
export const FLAGS: DiagramComponent = { id: 'flags', label: 'Flags', x: 120, y: 400, width: 220, height: 36 };

// Instruction decoder
export const DECODER: DiagramComponent = { id: 'decoder', label: 'Instruction Decoder', x: 80, y: 470, width: 300, height: 50 };

// Control Unit
export const CONTROL_UNIT: DiagramComponent = { id: 'control', label: 'Control Unit', x: 80, y: 540, width: 300, height: 50 };

// ── Bus Interface Unit (BIU) — right half ──
export const BIU_BOX: DiagramComponent = { id: 'biu', label: 'Bus Interface Unit (BIU)', x: 540, y: 30, width: 430, height: 650 };

// Segment registers inside BIU
export const REG_CS: DiagramComponent = { id: 'cs', label: 'CS', x: 570, y: 70, width: 160, height: 32 };
export const REG_DS: DiagramComponent = { id: 'ds', label: 'DS', x: 570, y: 110, width: 160, height: 32 };
export const REG_ES: DiagramComponent = { id: 'es', label: 'ES', x: 570, y: 150, width: 160, height: 32 };
export const REG_SS: DiagramComponent = { id: 'ss', label: 'SS', x: 570, y: 190, width: 160, height: 32 };

// Instruction Pointer
export const REG_IP: DiagramComponent = { id: 'ip', label: 'IP', x: 780, y: 70, width: 160, height: 32 };

// Address Adder (Σ)
export const ADDR_ADDER: DiagramComponent = { id: 'adder', label: 'Σ', x: 640, y: 280, width: 100, height: 60 };

// Instruction Queue (6-byte)
export const INSTR_QUEUE: DiagramComponent = { id: 'queue', label: 'Instruction Queue', x: 580, y: 400, width: 360, height: 50 };

// Bus Control Logic
export const BUS_CONTROL: DiagramComponent = { id: 'busctrl', label: 'Bus Control', x: 640, y: 520, width: 200, height: 50 };

// ── External Blocks (bottom) ──
export const MEMORY_BLOCK: DiagramComponent = { id: 'memory', label: 'Memory', x: 200, y: 740, width: 240, height: 50 };
export const IO_BLOCK: DiagramComponent = { id: 'io', label: 'I/O', x: 560, y: 740, width: 200, height: 50 };

// ── Internal bus (horizontal between EU and BIU) ──
export const INTERNAL_BUS_Y = 240;
export const INTERNAL_BUS_X1 = 40;
export const INTERNAL_BUS_X2 = 950;

// ── External buses (vertical from BIU to bottom) ──
export const ADDRESS_BUS_X = 340;
export const DATA_BUS_X = 600;
export const CONTROL_BUS_X = 800;

export const EXTERNAL_BUS_TOP = 600;
// The vertical buses stop short of the blocks; the horizontal system-bus rail sits
// here, centered in the band above Memory/I/O, and short drops feed the blocks.
export const EXTERNAL_BUS_BOTTOM = 710;

// ── All register components as array ──
export const ALL_REGISTERS: DiagramComponent[] = [
  REG_AX, REG_BX, REG_CX, REG_DX,
  REG_SP, REG_BP, REG_SI, REG_DI,
  REG_CS, REG_DS, REG_ES, REG_SS,
  REG_IP,
];

export const REGISTER_ID_MAP: Record<string, DiagramComponent> = {};
for (const reg of ALL_REGISTERS) {
  REGISTER_ID_MAP[reg.id] = reg;
}
// Map sub-register names to parent
REGISTER_ID_MAP['ah'] = REG_AX;
REGISTER_ID_MAP['al'] = REG_AX;
REGISTER_ID_MAP['bh'] = REG_BX;
REGISTER_ID_MAP['bl'] = REG_BX;
REGISTER_ID_MAP['ch'] = REG_CX;
REGISTER_ID_MAP['cl'] = REG_CX;
REGISTER_ID_MAP['dh'] = REG_DX;
REGISTER_ID_MAP['dl'] = REG_DX;

// ── Layout constants ──
export const VIRTUAL_WIDTH = 1000;
export const VIRTUAL_HEIGHT = 850;

// ── Color palette (CSS variable fallbacks) ──
export const COLORS = {
  background: '#1e1e2e',
  backgroundGradientTop: '#22223a',
  backgroundGradientBottom: '#1a1a28',
  euBox: '#2c2c54',
  euBoxGradientTop: '#363674',
  euBoxGradientBottom: '#262650',
  biuBox: '#294858',
  biuBoxGradientTop: '#2f586e',
  biuBoxGradientBottom: '#213c4c',
  register: '#3d3d66',
  registerGradientTop: '#48487c',
  registerGradientBottom: '#33335c',
  registerText: '#e0e0e0',
  registerActive: '#5aa4e6',
  alu: '#513c22',
  aluGradientTop: '#6d5330',
  aluGradientBottom: '#42311c',
  aluActive: '#d69f6c',
  flags: '#3b543b',
  flagsActive: '#57ab5b',
  decoder: '#3d3d58',
  bus: '#4a5570',
  // Idle slate for bus lines at rest — the semantic hue only asserts when a
  // path is active, so the resting diagram stays calm rather than rainbow.
  busIdle: '#464f68',
  // Vivid but clean bus hues (address≈green, data≈blue, control≈red, internal≈violet).
  // Saturation stays close to the source colours; the calmer look comes from the
  // reduced glow/shadow, not from washing the hues out to grey.
  busData: '#5eaae8',
  busAddress: '#8fc06d',
  busControl: '#df6f78',
  busInternal: '#c078d6',
  busDataGlow: 'rgba(94, 170, 232, 0.28)',
  busAddressGlow: 'rgba(143, 192, 109, 0.28)',
  busControlGlow: 'rgba(223, 111, 120, 0.28)',
  busInternalGlow: 'rgba(192, 120, 214, 0.28)',
  memory: '#264e38',
  memoryGradientTop: '#316e4d',
  memoryGradientBottom: '#1d422f',
  io: '#552840',
  ioGradientTop: '#6e3452',
  ioGradientBottom: '#411f32',
  text: '#c9cedb',
  textBright: '#ffffff',
  textDim: '#aab2c2',
  label: '#cdd3e1',
  border: '#444466',
  borderSubtle: '#3a3a55',
  activeGlow: 'rgba(90, 164, 230, 0.3)',
  particle: '#ffffff',
} as const;

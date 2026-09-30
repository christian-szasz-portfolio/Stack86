import { createFeatureSelector, createSelector } from '@ngrx/store';
import { EmulatorState, EMULATOR_FEATURE_KEY } from './emulator.state';

export const selectEmulatorState = createFeatureSelector<EmulatorState>(EMULATOR_FEATURE_KEY);

export const selectCpu = createSelector(selectEmulatorState, (s) => s.cpu);
export const selectRegisters = createSelector(selectCpu, (cpu) => ({
  ax: cpu.ax,
  bx: cpu.bx,
  cx: cpu.cx,
  dx: cpu.dx,
  sp: cpu.sp,
  bp: cpu.bp,
  si: cpu.si,
  di: cpu.di,
  ip: cpu.ip,
  cs: cpu.cs,
  ds: cpu.ds,
  es: cpu.es,
  ss: cpu.ss,
}));
export const selectFlags = createSelector(selectCpu, (cpu) => cpu.flags);
export const selectMemory = createSelector(selectEmulatorState, (s) => s.memory);
export const selectInstructions = createSelector(selectEmulatorState, (s) => s.instructions);
export const selectCurrentInstruction = createSelector(
  selectInstructions,
  selectCpu,
  (instructions, cpu) => instructions.find((i) => i.address === cpu.ip) ?? null,
);
export const selectExecutionStatus = createSelector(selectEmulatorState, (s) => s.status);
export const selectBreakpoints = createSelector(selectEmulatorState, (s) => s.breakpoints);
export const selectConsoleOutput = createSelector(selectEmulatorState, (s) => s.consoleOutput);
export const selectError = createSelector(selectEmulatorState, (s) => s.error);
export const selectSourceCode = createSelector(selectEmulatorState, (s) => s.sourceCode);
export const selectLabels = createSelector(selectEmulatorState, (s) => s.labels);
export const selectParseErrors = createSelector(selectEmulatorState, (s) => s.parseErrors);
export const selectLastTrace = createSelector(selectEmulatorState, (s) => s.lastTrace);

export const selectMemorySlice = (start: number, length: number) =>
  createSelector(selectMemory, (memory) => memory.slice(start, start + length));

export const selectStackView = createSelector(selectMemory, selectCpu, (memory, cpu) => {
  const entries: { address: number; value: number }[] = [];
  for (let addr = cpu.sp; addr < 0xFFFE; addr += 2) {
    if (addr >= 0 && addr + 1 < memory.length) {
      entries.push({ address: addr, value: memory[addr] | (memory[addr + 1] << 8) });
    }
  }
  return entries;
});

/**
 * Centralized data-testid catalog. Templates use these values; tests reference
 * them through helpers below to keep selectors stable + searchable.
 */
export const TID = {
  // Compiler toolbar
  compileLanguageDropdown: 'compile-language-dropdown',
  compileSampleDropdown: 'compile-sample-dropdown',
  compileBtn: 'compile-btn',
  compileLoadIntoAsm: 'compile-load-into-asm-btn',

  // Compiler editor / files
  compileAddFile: 'compile-add-file',
  compileFileTab: (i: number) => `compile-file-tab-${i}`,

  // Compile console
  compileConsoleProblems: 'compile-console-tab-problems',
  compileConsoleBuildLog: 'compile-console-tab-buildlog',
  compileConsoleList: 'compile-console-list',

  // Compile results
  compileAsmOutput: 'compile-asm-output',

  // Assembler toolbar
  asmAssemble: 'asm-assemble-btn',
  asmRun: 'asm-run-btn',
  asmStep: 'asm-step-btn',
  asmPause: 'asm-pause-btn',
  asmReset: 'asm-reset-btn',
  asmSampleDropdown: 'asm-sample-dropdown',
  asmSpeedInput: 'asm-speed-input',
  asmSpeedUnit: 'asm-speed-unit',
  asmDataFlowToggle: 'asm-data-flow-toggle',

  // Assembler debugger
  asmRegister: (name: string) => `asm-register-${name}`,
  asmFlag: (name: string) => `asm-flag-${name}`,

  // Assembler console
  asmConsoleOutput: 'asm-console-tab-output',
  asmConsoleProblems: 'asm-console-tab-problems',
  asmConsoleBuildLog: 'asm-console-tab-buildlog',
} as const;

import type { Page, Locator } from '@playwright/test';

export function byTid(page: Page | Locator, id: string): Locator {
  return page.locator(`[data-testid="${id}"]`);
}

import { test as base, type Page } from '@playwright/test';
import type { CompileResponse, CompilationDiagnostic } from '../../src/app/core/compiler/compiler.models';

export interface CompileMockState {
  assembly: string;
  errors: CompilationDiagnostic[];
  warnings: CompilationDiagnostic[];
  log: string[];
  fail: boolean;
  failStatus: number;
}

export interface CompileMockApi {
  state: CompileMockState;
  setAssembly(asm: string): void;
  setErrors(errors: CompilationDiagnostic[]): void;
  setWarnings(warnings: CompilationDiagnostic[]): void;
  setLog(lines: string[]): void;
  setFailure(status: number | null): void;
}

const DEFAULT_ASM = `; --- mock compiled output ---
.MODEL SMALL
.STACK 100h
.CODE
START:
    MOV AH, 4Ch
    INT 21h
END START
`;

async function installCompileMocks(page: Page): Promise<CompileMockApi> {
  const state: CompileMockState = {
    assembly: DEFAULT_ASM,
    errors: [],
    warnings: [],
    log: ['[Info] preprocess', '[Info] transpile', '[Info] emit assembly'],
    fail: false,
    failStatus: 500,
  };

  // Catch-all guard: any unmocked /api/** endpoint returns a benign 404 rather than
  // hitting the dev proxy and 502'ing (which would surface the global error modal).
  // Registered first so the specific Compiler routes below take precedence.
  await page.route('**/api/**', async (route) => {
    await route.fulfill({
      status: 404,
      contentType: 'application/json',
      body: JSON.stringify({ detail: 'Unmocked endpoint (demo e2e)' }),
    });
  });

  await page.route('**/api/Compiler/compile', async (route) => {
    if (state.fail) {
      await route.fulfill({
        status: state.failStatus,
        contentType: 'application/json',
        body: JSON.stringify({ detail: 'Mock compilation failure' }),
      });
      return;
    }
    const response: CompileResponse = {
      assembly: state.errors.length === 0 ? state.assembly : undefined,
      errors: state.errors,
      warnings: state.warnings,
      consoleMessages: state.log,
    };
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(response),
    });
  });

  await page.route('**/api/Compiler/compileStream', async (route) => {
    if (state.fail) {
      await route.fulfill({
        status: state.failStatus,
        contentType: 'application/json',
        body: JSON.stringify({ detail: 'Mock streaming failure' }),
      });
      return;
    }
    const lines: string[] = [];
    for (const text of state.log) {
      lines.push(JSON.stringify({ type: 'log', text }));
    }
    lines.push(
      JSON.stringify({
        type: 'result',
        assembly: state.errors.length === 0 ? state.assembly : undefined,
        errors: state.errors,
        warnings: state.warnings,
      }),
    );
    await route.fulfill({
      status: 200,
      contentType: 'application/x-ndjson',
      body: lines.join('\n') + '\n',
    });
  });

  return {
    state,
    setAssembly(asm: string): void {
      state.assembly = asm;
    },
    setErrors(errors: CompilationDiagnostic[]): void {
      state.errors = errors;
    },
    setWarnings(warnings: CompilationDiagnostic[]): void {
      state.warnings = warnings;
    },
    setLog(lines: string[]): void {
      state.log = lines;
    },
    setFailure(status: number | null): void {
      state.fail = status !== null;
      if (status) state.failStatus = status;
    },
  };
}

interface Fixtures {
  compileApi: CompileMockApi;
  autoCompileMocks: void;
}

export const test = base.extend<Fixtures>({
  compileApi: async ({ page }, use) => {
    const api = await installCompileMocks(page);
    await use(api);
  },
  autoCompileMocks: [async ({ compileApi }, use) => {
    void compileApi;
    await use();
  }, { auto: true }],
});

export { expect } from '@playwright/test';

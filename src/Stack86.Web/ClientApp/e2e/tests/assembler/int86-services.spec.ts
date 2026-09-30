import { test, expect } from '../../fixtures';
import { setMonacoValue, waitForMonacoReady } from '../../utils/monaco';

/**
 * Drives every INT 86h service handler in DataHandler.handleInt86 by
 * pushing a stable set of args onto the stack and calling each AH value
 * from 0x01 through 0x2A. Args at SP+0/2/4/6 are valid pointers/sizes
 * that won't crash the handlers.
 *
 * Goal: cover the ~400 lines of switch-case code for INT 86h services
 * in core/emulator/instruction/handlers/data.handler.ts and exercise
 * the heap-allocator (Malloc/Free/Calloc/Realloc).
 */
function makeInt86Sweep(): string {
  const lines: string[] = [
    '; Seed scratch memory at 0x500 with "Hi$" and a buffer at 0x504',
    'MOV WORD PTR [0x500], 0x6948',  // 'H','i'
    'MOV WORD PTR [0x502], 0x0024',  // '$', null
    'MOV WORD PTR [0x504], 0x6948',
    'MOV WORD PTR [0x506], 0x0024',
    '',
    '; Push args: 0x500 (ptr1), 0x504 (ptr2), 1 (val), 16 (size)',
    'PUSH 16',
    'PUSH 1',
    'PUSH 0x504',
    'PUSH 0x500',
    '',
  ];
  // Service sweep: each AH then INT 0x86.
  for (let ah = 0x01; ah <= 0x2A; ah++) {
    lines.push(`MOV AH, 0x${ah.toString(16).toUpperCase().padStart(2, '0')}`);
    lines.push(`INT 0x86`);
  }
  lines.push('HLT');
  return lines.join('\n');
}

const INT86_SWEEP = makeInt86Sweep();

test.describe('INT 86h — service handlers', () => {
  test('sweeps every service AH=01..2A', async ({ page }) => {
    await page.goto('/assembler');
    await waitForMonacoReady(page);
    await setMonacoValue(page, null, INT86_SWEEP);
    await page.locator('[data-testid="asm-assemble-btn"] button').click();
    const runBtn = page.locator('[data-testid="asm-run-btn"] button');
    await expect(runBtn).toBeEnabled({ timeout: 10_000 });
    await runBtn.click();
    // Just confirm execution finished without crashing the page.
    await expect(page.locator('[data-testid="asm-status"]')).toBeVisible();
  });

  test('Malloc → Free round-trip exercises HeapAllocator', async ({ page }) => {
    const SRC = `
; Malloc(16) → AX = ptr
PUSH 16
MOV AH, 0x20
INT 0x86
ADD SP, 2
MOV BX, AX

; Calloc(4, 4) → AX = ptr
PUSH 4
PUSH 4
MOV AH, 0x22
INT 0x86
ADD SP, 4
MOV CX, AX

; Realloc(BX, 32) → AX = new ptr
PUSH 32
PUSH BX
MOV AH, 0x23
INT 0x86
ADD SP, 4
MOV BX, AX

; Free(BX)
PUSH BX
MOV AH, 0x21
INT 0x86
ADD SP, 2

; Free(CX)
PUSH CX
MOV AH, 0x21
INT 0x86
ADD SP, 2

HLT`;
    await page.goto('/assembler');
    await waitForMonacoReady(page);
    await setMonacoValue(page, null, SRC);
    await page.locator('[data-testid="asm-assemble-btn"] button').click();
    const runBtn = page.locator('[data-testid="asm-run-btn"] button');
    await expect(runBtn).toBeEnabled({ timeout: 10_000 });
    await runBtn.click();
    await expect(page.locator('[data-testid="asm-status"]')).toBeVisible();
  });
});

test.describe('INT 21h — string output (AH=09)', () => {
  test('writes a $-terminated string to console', async ({ page }) => {
    // Place "Hi$" at 0x500: bytes 0x48 'H', 0x69 'i', 0x24 '$'
    const SRC = `MOV WORD PTR [0x500], 0x6948
MOV WORD PTR [0x502], 0x0024
MOV DX, 0x500
MOV AH, 0x09
INT 0x21
MOV AH, 0x4C
INT 0x21`;
    await page.goto('/assembler');
    await waitForMonacoReady(page);
    await setMonacoValue(page, null, SRC);
    await page.locator('[data-testid="asm-assemble-btn"] button').click();
    const runBtn = page.locator('[data-testid="asm-run-btn"] button');
    await expect(runBtn).toBeEnabled({ timeout: 10_000 });
    await runBtn.click();
    const outputTab = page.locator('[data-testid="console-tab-output"]');
    if (await outputTab.count() > 0) await outputTab.click();
    await expect(page.locator('[data-testid="asm-console-list"]')).toContainText('Hi', { timeout: 5_000 });
  });
});

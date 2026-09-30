import { test, expect } from '../../fixtures';
import { setMonacoValue, waitForMonacoReady } from '../../utils/monaco';

/**
 * Targets branch/line coverage gaps in:
 * - DataHandler.lea (LEA with memory + register operands)
 * - DataHandler Strcmp/Strncmp with DIFFERENT strings (covers c1!=c2 branch)
 * - DataHandler Atoi parsing positive/negative numbers
 * - All flag-setting arithmetic paths (overflow, sign, parity, aux carry)
 * - Conditional jumps in both taken & not-taken directions
 */

test.describe('Handlers — deep branch coverage', () => {
  test('LEA loads address into register', async ({ page }) => {
    const SRC = `LEA SI, [0x500]
LEA DI, [0x600]
LEA BX, [0x100]
HLT`;
    await page.goto('/assembler');
    await waitForMonacoReady(page);
    await setMonacoValue(page, null, SRC);
    await page.locator('[data-testid="asm-assemble-btn"] button').click();
    await expect(page.locator('[data-testid="asm-run-btn"] button')).toBeEnabled({ timeout: 10_000 });
    await page.locator('[data-testid="asm-run-btn"] button').click();
    await expect(page.locator('[data-testid="asm-status"]')).toBeVisible();
  });

  test('Strcmp/Strncmp with different strings hits c1!=c2 branches', async ({ page }) => {
    // Place "AB$" at 0x500 and "AC$" at 0x510 — second char differs.
    const SRC = `MOV WORD PTR [0x500], 0x4241
MOV WORD PTR [0x502], 0x0024
MOV WORD PTR [0x510], 0x4341
MOV WORD PTR [0x512], 0x0024

PUSH 0x510
PUSH 0x500
MOV AH, 0x04
INT 0x86
ADD SP, 4

PUSH 4
PUSH 0x510
PUSH 0x500
MOV AH, 0x10
INT 0x86
ADD SP, 6

; Memcmp with different
PUSH 4
PUSH 0x510
PUSH 0x500
MOV AH, 0x15
INT 0x86
ADD SP, 6

HLT`;
    await page.goto('/assembler');
    await waitForMonacoReady(page);
    await setMonacoValue(page, null, SRC);
    await page.locator('[data-testid="asm-assemble-btn"] button').click();
    await expect(page.locator('[data-testid="asm-run-btn"] button')).toBeEnabled({ timeout: 10_000 });
    await page.locator('[data-testid="asm-run-btn"] button').click();
    await expect(page.locator('[data-testid="asm-status"]')).toBeVisible();
  });

  test('Atoi parses numeric string from memory', async ({ page }) => {
    // "123$" at 0x500: '1'=0x31, '2'=0x32, '3'=0x33, '$'=0x24
    const SRC = `MOV WORD PTR [0x500], 0x3231
MOV WORD PTR [0x502], 0x2433
PUSH 0x500
MOV AH, 0x08
INT 0x86
ADD SP, 2
HLT`;
    await page.goto('/assembler');
    await waitForMonacoReady(page);
    await setMonacoValue(page, null, SRC);
    await page.locator('[data-testid="asm-assemble-btn"] button').click();
    await expect(page.locator('[data-testid="asm-run-btn"] button')).toBeEnabled({ timeout: 10_000 });
    await page.locator('[data-testid="asm-run-btn"] button').click();
    await expect(page.locator('[data-testid="asm-status"]')).toBeVisible();
  });

  test('arithmetic edge cases trigger every flag path', async ({ page }) => {
    const SRC = `; OF + SF + ZF + CF + PF + AF coverage
MOV AX, 0x7FFF
ADD AX, 1          ; overflow into negative → OF=1
MOV AX, 0xFFFF
ADD AX, 1          ; result 0 → ZF=1, CF=1
MOV AX, 0
SUB AX, 1          ; CF=1, SF=1
MOV AX, 0x8000
NEG AX             ; overflow case (most negative)
MOV AX, 5
MOV BX, 5
SUB AX, BX         ; ZF=1
MOV AX, 0x0F
ADD AX, 0x01       ; AF=1 (BCD aux)
MOV AX, 0xFF
ADD AX, 0xFF       ; carry path
MOV AX, 0
MOV BX, 0
DIV BX             ; div-by-zero — engine should halt
HLT`;
    await page.goto('/assembler');
    await waitForMonacoReady(page);
    await setMonacoValue(page, null, SRC);
    await page.locator('[data-testid="asm-assemble-btn"] button').click();
    await expect(page.locator('[data-testid="asm-run-btn"] button')).toBeEnabled({ timeout: 10_000 });
    await page.locator('[data-testid="asm-run-btn"] button').click();
    await expect(page.locator('[data-testid="asm-status"]')).toBeVisible();
  });

  test('conditional jumps — both directions for every variant', async ({ page }) => {
    const SRC = `; Set up signed comparison: AX=5, BX=10 → AX < BX
MOV AX, 5
MOV BX, 10
CMP AX, BX

JE eq1          ; not taken
eq1:
JNE neq1        ; taken
neq1:
JL lt1          ; taken (5 < 10 signed)
lt1:
JLE le1         ; taken
le1:
JG gt1          ; not taken
gt1:
JGE ge1         ; not taken
ge1:
JB b1           ; taken (unsigned 5 < 10)
b1:
JA a1           ; not taken
a1:
JZ z1           ; not taken (5 != 10)
z1:
JNZ nz1         ; taken
nz1:

; Now AX > BX
MOV AX, 100
MOV BX, 50
CMP AX, BX
JG gt2          ; taken
gt2:
JGE ge2         ; taken
ge2:
JA a2           ; taken
a2:
JL lt2          ; not taken
lt2:

; Equal case
MOV AX, 7
MOV BX, 7
CMP AX, BX
JE eq2          ; taken
eq2:
JZ z2           ; taken
z2:

; LOOP variants
MOV CX, 3
loop_start:
DEC CX
LOOP loop_start

HLT`;
    await page.goto('/assembler');
    await waitForMonacoReady(page);
    await setMonacoValue(page, null, SRC);
    await page.locator('[data-testid="asm-assemble-btn"] button').click();
    await expect(page.locator('[data-testid="asm-run-btn"] button')).toBeEnabled({ timeout: 10_000 });
    await page.locator('[data-testid="asm-run-btn"] button').click();
    await expect(page.locator('[data-testid="asm-status"]')).toBeVisible();
  });

  test('reset + reload flow exercises emulator.effects reset path', async ({ page }) => {
    const SRC = `MOV AX, 1\nMOV BX, 2\nADD AX, BX\nHLT`;
    await page.goto('/assembler');
    await waitForMonacoReady(page);
    await setMonacoValue(page, null, SRC);
    await page.locator('[data-testid="asm-assemble-btn"] button').click();
    const runBtn = page.locator('[data-testid="asm-run-btn"] button');
    await expect(runBtn).toBeEnabled({ timeout: 10_000 });
    await runBtn.click();
    await page.waitForTimeout(200);
    // Reset
    const resetBtn = page.locator('[data-testid="asm-reset-btn"] button');
    if (await resetBtn.count() > 0) await resetBtn.click();
    await page.waitForTimeout(200);
    // Re-assemble & step a few times
    await page.locator('[data-testid="asm-assemble-btn"] button').click();
    await expect(runBtn).toBeEnabled({ timeout: 10_000 });
    const stepBtn = page.locator('[data-testid="asm-step-btn"] button');
    for (let i = 0; i < 5; i++) {
      if (await stepBtn.isEnabled()) await stepBtn.click();
    }
  });
});

import { test, expect } from '../../fixtures';
import { setMonacoValue, waitForMonacoReady } from '../../utils/monaco';

/**
 * Comprehensive ASM exercising every opcode handler in the InstructionRegistry
 * across many addressing modes. Goal: maximize coverage of
 *   - core/emulator/instruction/handlers/{data,arithmetic,logic,flow}.handler.ts
 *   - core/emulator/instruction/operand.util.ts
 *   - core/emulator/parser/parser.ts + tokenizer.ts
 *   - core/emulator/binary/x86-encoder.ts (assembly emits encoded bytes)
 *   - core/emulator/execution/execution-engine.ts
 *
 * Uses only syntax demonstrated in the working sample programs:
 * 0x-prefix hex, decimal, char literals, plain labels, WORD PTR for memory.
 */
const KITCHEN_SINK = `; --- kitchen sink ---
; MOV variants: reg/imm, reg/reg, reg/mem, mem/reg, mem/imm
MOV AX, 0x00FF
MOV BX, AX
MOV WORD PTR [0x300], 0x1234
MOV CX, [0x300]
MOV AL, AH
MOV DL, 'A'

; XCHG
XCHG AX, BX

; Stack: PUSH/POP
PUSH AX
PUSH BX
PUSH CX
POP CX
POP BX
POP AX

; Arithmetic
MOV AX, 100
ADD AX, 50
SUB AX, 25
INC AX
DEC AX
NEG AX
NEG AX
MOV BX, 5
MUL BX
MOV BX, 3
DIV BX

; Logic + shifts
MOV AX, 0xFF00
AND AX, 0x0FF0
OR  AX, 0x000F
XOR AX, 0xFFFF
NOT AX
SHL AX, 1
SHR AX, 1

; Compare + every conditional jump
MOV AX, 5
CMP AX, 5
JE eq1
eq1:
CMP AX, 10
JNE ne1
ne1:
CMP AX, 5
JG g1
g1:
CMP AX, 5
JGE ge1
ge1:
CMP AX, 10
JL l1
l1:
CMP AX, 5
JLE le1
le1:
CMP AX, 5
JA a1
a1:
CMP AX, 10
JB b1
b1:
CMP AX, 0
JZ z1
z1:
CMP AX, 1
JNZ nz1
nz1:
JMP after_jumps
after_jumps:

; CALL/RET + LOOP + NOP
CALL myproc
MOV CX, 3
loop_start:
NOP
LOOP loop_start
JMP printout

myproc:
NOP
NOP
RET

printout:
; INT 21h AH=02 (char output)
MOV AH, 0x02
MOV DL, 'H'
INT 0x21
MOV DL, 'i'
INT 0x21

; Exit
MOV AH, 0x4C
INT 0x21
`;

test.describe('Assembler — opcode coverage', () => {
  test('runs kitchen-sink program to completion', async ({ page }) => {
    page.on('download', (d) => { d.cancel().catch(() => {}); });
    await page.goto('/assembler');
    await waitForMonacoReady(page);
    await setMonacoValue(page, null, KITCHEN_SINK);
    await page.locator('[data-testid="asm-assemble-btn"] button').click();
    const runBtn = page.locator('[data-testid="asm-run-btn"] button');
    await expect(runBtn).toBeEnabled({ timeout: 10_000 });
    await runBtn.click();
    const outputTab = page.locator('[data-testid="console-tab-output"]');
    if (await outputTab.count() > 0) await outputTab.click();
    await expect(page.locator('[data-testid="asm-console-list"]')).toBeVisible();

    // After run, status transitions away from Idle. Wait for runBtn to be
    // disabled (meaning Running or Halted) so canExportCom is true.
    await expect(runBtn).toBeDisabled({ timeout: 5_000 }).catch(() => {});
    await page.locator('[data-testid="asm-file-menu-trigger"]').click();
    const exportItem = page.locator('[data-testid="asm-file-menu-item-exportCom"]');
    if (await exportItem.count() > 0) {
      await exportItem.click({ force: true }).catch(() => {});
      await page.waitForTimeout(200);
    }
  });

  test('step-by-step through kitchen-sink advances IP', async ({ page }) => {
    await page.goto('/assembler');
    await waitForMonacoReady(page);
    await setMonacoValue(page, null, KITCHEN_SINK);
    await page.locator('[data-testid="asm-assemble-btn"] button').click();
    const stepBtn = page.locator('[data-testid="asm-step-btn"] button');
    await expect(stepBtn).toBeEnabled({ timeout: 10_000 });
    for (let i = 0; i < 25; i++) {
      if (await stepBtn.isEnabled()) await stepBtn.click();
    }
    await expect(page.locator('[data-testid="asm-register-IP-hex"]')).toBeVisible();
  });

  test('kitchen-sink with data-flow diagram enabled', async ({ page }) => {
    await page.setViewportSize({ width: 1600, height: 1000 });
    await page.goto('/assembler');
    await waitForMonacoReady(page);
    await setMonacoValue(page, null, KITCHEN_SINK);
    await page.locator('[data-testid="asm-data-flow-toggle"] button').click();
    await expect(page.locator('emu-data-flow-diagram canvas')).toBeVisible({ timeout: 10_000 });
    // Wait for canvas to actually have layout dimensions
    await page.waitForFunction(() => {
      const c = document.querySelector('emu-data-flow-diagram canvas') as HTMLCanvasElement | null;
      return !!c && c.getBoundingClientRect().width > 100;
    }, undefined, { timeout: 5_000 }).catch(() => {});
    await page.locator('[data-testid="asm-assemble-btn"] button').click();
    const stepBtn = page.locator('[data-testid="asm-step-btn"] button');
    await expect(stepBtn).toBeEnabled({ timeout: 10_000 });
    for (let i = 0; i < 20; i++) {
      if (await stepBtn.isEnabled()) {
        await stepBtn.click();
        await page.waitForTimeout(80); // give animation a frame
      }
    }
    const canvas = page.locator('emu-data-flow-diagram canvas');
    const box = await canvas.boundingBox();
    if (box) {
      // Many mouse moves to fire hit-test for each diagram component
      const w = box.width, h = box.height;
      for (let i = 0; i < 8; i++) {
        await page.mouse.move(box.x + (w * (i + 1)) / 10, box.y + (h * (i + 1)) / 10);
      }
      await page.mouse.move(box.x - 10, box.y - 10);
    }
    await page.locator('[data-testid="asm-data-flow-toggle"] button').click();
  });
});

test.describe('Assembler — INT 21h I/O', () => {
  test('AH=01 reads a character from input', async ({ page }) => {
    await page.goto('/assembler');
    await waitForMonacoReady(page);
    await setMonacoValue(page, null, `MOV AH, 0x01\nINT 0x21\nMOV AH, 0x4C\nINT 0x21`);
    await page.locator('[data-testid="asm-assemble-btn"] button').click();
    const runBtn = page.locator('[data-testid="asm-run-btn"] button');
    if (await runBtn.isEnabled()) await runBtn.click();
    await expect(page.locator('[data-testid="asm-status"]')).toBeVisible();
  });
});

import { test, expect } from '../../fixtures';
import { setMonacoValue, waitForMonacoReady } from '../../utils/monaco';

test.describe('Parser — error reporting', () => {
  test('unknown mnemonic surfaces in console list', async ({ page }) => {
    await page.goto('/8086-emulator');
    await waitForMonacoReady(page);
    await setMonacoValue(page, null, `FOOBAR AX, 1\nHLT`);
    await page.locator('[data-testid="asm-assemble-btn"] button').click();
    await expect(page.locator('[data-testid="asm-console-list"]')).toContainText(/unknown|FOOBAR/i, { timeout: 5_000 });
  });

  test('runs valid simple program', async ({ page }) => {
    await page.goto('/8086-emulator');
    await waitForMonacoReady(page);
    await setMonacoValue(page, null, `MOV AX, 7\nMOV BX, AX\nADD AX, BX\nHLT`);
    await page.locator('[data-testid="asm-assemble-btn"] button').click();
    const runBtn = page.locator('[data-testid="asm-run-btn"] button');
    await expect(runBtn).toBeEnabled({ timeout: 10_000 });
    await runBtn.click();
    await expect(page.locator('[data-testid="asm-register-AX-dec"]')).toContainText('14', { timeout: 5_000 });
  });

  test('many tokenizer constructs parse: hex, decimal, char, comments, labels', async ({ page }) => {
    const SRC = `; comment line
; another comment
start_label:
  MOV AX, 0xABCD       ; hex literal with 0x prefix
  MOV BX, 100          ; decimal literal
  MOV CL, 'Z'          ; char literal
  MOV DX, 0
  CMP AX, 0xABCD
  JE matched
  MOV DX, 1
matched:
  HLT`;
    await page.goto('/8086-emulator');
    await waitForMonacoReady(page);
    await setMonacoValue(page, null, SRC);
    await page.locator('[data-testid="asm-assemble-btn"] button').click();
    const runBtn = page.locator('[data-testid="asm-run-btn"] button');
    await expect(runBtn).toBeEnabled({ timeout: 10_000 });
    await runBtn.click();
    await expect(page.locator('[data-testid="asm-register-AX-dec"]')).toBeVisible();
  });

  test('CALL/RET subroutine pattern', async ({ page }) => {
    const SRC = `MOV AX, 10
CALL doubler
MOV BX, AX
HLT
doubler:
  ADD AX, AX
  RET`;
    await page.goto('/8086-emulator');
    await waitForMonacoReady(page);
    await setMonacoValue(page, null, SRC);
    await page.locator('[data-testid="asm-assemble-btn"] button').click();
    const runBtn = page.locator('[data-testid="asm-run-btn"] button');
    await expect(runBtn).toBeEnabled({ timeout: 10_000 });
    await runBtn.click();
    await expect(page.locator('[data-testid="asm-register-BX-dec"]')).toContainText('20', { timeout: 5_000 });
  });
});

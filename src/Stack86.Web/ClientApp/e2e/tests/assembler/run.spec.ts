import { test, expect } from '../../fixtures';
import { setMonacoValue, waitForMonacoReady } from '../../utils/monaco';

const HLT_ONLY = `MOV AX, 1
HLT`;

test.describe('Assembler — run', () => {
  test('run executes to HLT and updates registers', async ({ page }) => {
    await page.goto('/assembler');
    await waitForMonacoReady(page);
    await setMonacoValue(page, null, HLT_ONLY);
    await page.locator('[data-testid="asm-assemble-btn"] button').click();
    await page.locator('[data-testid="asm-run-btn"] button').click();
    const ax = page.locator('[data-testid="asm-register-AX-dec"]');
    await expect(ax).toContainText('1', { timeout: 5_000 });
  });
});

test.describe('Assembler — flags after CMP', () => {
  test('CMP equal sets ZF', async ({ page }) => {
    await page.goto('/assembler');
    await waitForMonacoReady(page);
    await setMonacoValue(page, null, `MOV AX, 5\nCMP AX, 5\nHLT`);
    await page.locator('[data-testid="asm-assemble-btn"] button').click();
    await page.locator('[data-testid="asm-run-btn"] button').click();
    const zf = page.locator('[data-testid="asm-flag-ZF"]');
    await expect(zf).toHaveAttribute('data-set', 'true', { timeout: 5_000 });
  });
});

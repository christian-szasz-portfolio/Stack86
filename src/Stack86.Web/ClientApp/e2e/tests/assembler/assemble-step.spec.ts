import { test, expect } from '../../fixtures';
import { setMonacoValue, waitForMonacoReady } from '../../utils/monaco';

const SIMPLE_ASM = `MOV AX, 5
MOV BX, 3
ADD AX, BX
HLT`;

test.describe('Assembler — assemble + step', () => {
  test('assemble button parses code and shows registers', async ({ page }) => {
    await page.goto('/assembler');
    await expect(page.locator('[data-testid="asm-assemble-btn"]')).toBeVisible({ timeout: 15_000 });

    await waitForMonacoReady(page);
    await setMonacoValue(page, null, SIMPLE_ASM);

    await page.locator('[data-testid="asm-assemble-btn"] button').click();
    await expect(page.locator('[data-testid="asm-registers"]')).toBeVisible();
    await expect(page.locator('[data-testid="asm-flags"]')).toBeVisible();
  });

  test('step advances IP and updates AX after ADD', async ({ page }) => {
    await page.goto('/assembler');
    await waitForMonacoReady(page);
    await setMonacoValue(page, null, SIMPLE_ASM);
    await page.locator('[data-testid="asm-assemble-btn"] button').click();

    const stepBtn = page.locator('[data-testid="asm-step-btn"] button');
    await stepBtn.click();
    await stepBtn.click();
    await stepBtn.click();

    const axDec = page.locator('[data-testid="asm-register-AX-dec"]');
    await expect(axDec).toContainText('8');
  });

  test('reset clears computed state', async ({ page }) => {
    await page.goto('/assembler');
    await waitForMonacoReady(page);
    await setMonacoValue(page, null, SIMPLE_ASM);
    await page.locator('[data-testid="asm-assemble-btn"] button').click();
    await page.locator('[data-testid="asm-step-btn"] button').click();
    await page.locator('[data-testid="asm-step-btn"] button').click();
    await page.locator('[data-testid="asm-reset-btn"] button').click();
    const axDec = page.locator('[data-testid="asm-register-AX-dec"]');
    await expect(axDec).toContainText('0');
  });
});

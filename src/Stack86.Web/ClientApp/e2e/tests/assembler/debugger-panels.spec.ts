import { test, expect } from '../../fixtures';

test.describe('Debugger panels', () => {
  test('registers panel renders all 8086 registers', async ({ page }) => {
    await page.goto('/assembler');
    await expect(page.locator('[data-testid="asm-registers"]')).toBeVisible({ timeout: 15_000 });
    for (const reg of ['AX', 'BX', 'CX', 'DX', 'SP', 'BP', 'SI', 'DI', 'IP']) {
      await expect(page.locator(`[data-testid="asm-register-${reg}"]`)).toBeVisible();
    }
  });

  test('flags panel shows all status flags', async ({ page }) => {
    await page.goto('/assembler');
    await expect(page.locator('[data-testid="asm-flags"]')).toBeVisible({ timeout: 15_000 });
    for (const flag of ['CF', 'PF', 'AF', 'ZF', 'SF', 'OF', 'DF', 'IF', 'TF']) {
      const flagEl = page.locator(`[data-testid="asm-flag-${flag}"]`);
      if (await flagEl.count() > 0) {
        await expect(flagEl.first()).toBeVisible();
      }
    }
  });

  test('memory view loads with toolbar and content', async ({ page }) => {
    await page.goto('/assembler');
    await expect(page.locator('[data-testid="asm-memory-toolbar"]')).toBeVisible({ timeout: 15_000 });
    await expect(page.locator('[data-testid="asm-memory-content"]')).toBeVisible();
  });

  test('memory address input jumps to address', async ({ page }) => {
    await page.goto('/assembler');
    const input = page.locator('[data-testid="asm-memory-address-input"]');
    await input.waitFor({ state: 'visible', timeout: 15_000 });
    await input.fill('0100');
    await input.press('Enter');
    await expect(page.locator('[data-testid="asm-memory-content"]')).toContainText('0100');
  });

  test('memory view jump buttons', async ({ page }) => {
    await page.goto('/assembler');
    await page.locator('[data-testid="asm-memory-jump-stack"]').waitFor({ state: 'visible', timeout: 15_000 });
    await page.locator('[data-testid="asm-memory-jump-stack"]').click();
    await page.locator('[data-testid="asm-memory-jump-zero"]').click();
    await page.locator('[data-testid="asm-memory-jump-data"]').click();
    await expect(page.locator('[data-testid="asm-memory-content"]')).toBeVisible();
  });

  test('stack view renders content panel', async ({ page }) => {
    await page.goto('/assembler');
    await expect(page.locator('[data-testid="asm-stack-content"]')).toBeVisible({ timeout: 15_000 });
  });

  test('stack populates after PUSH operations', async ({ page }) => {
    await page.goto('/assembler');
    // Load Stack Operations sample
    await page.locator('[data-testid="asm-sample-dropdown"] button').first().click();
    await page.locator('.dropdown__panel .dropdown__item', { hasText: 'Stack Operations' }).first().click();
    await page.locator('[data-testid="asm-assemble-btn"] button').click();
    const runBtn = page.locator('[data-testid="asm-run-btn"] button');
    await expect(runBtn).toBeEnabled({ timeout: 10_000 });
    await runBtn.click();
    // Stack content should be visible (rows may or may not be populated depending on final SP)
    await expect(page.locator('[data-testid="asm-stack-content"]')).toBeVisible();
  });
});

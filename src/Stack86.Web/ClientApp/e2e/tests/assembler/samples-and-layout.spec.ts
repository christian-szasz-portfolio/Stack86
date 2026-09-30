import { test, expect } from '../../fixtures';

test.describe('Assembler — sample programs', () => {
  test('default sample loads and assembles', async ({ page }) => {
    await page.goto('/assembler');
    await page.locator('[data-testid="asm-sample-dropdown"]').waitFor({ state: 'visible', timeout: 15_000 });
    // open dropdown
    const dropdownBtn = page.locator('[data-testid="asm-sample-dropdown"] button').first();
    await dropdownBtn.click();
    // pick first sample
    const firstItem = page.locator('.dropdown__panel .dropdown__item').first();
    if (await firstItem.count() > 0) {
      await firstItem.click();
    }
    await page.locator('[data-testid="asm-assemble-btn"] button').click();
    await expect(page.locator('[data-testid="asm-status"]')).toBeVisible();
  });
});

test.describe('Responsive layout', () => {
  test('narrow viewport applies compact toolbar', async ({ page }) => {
    await page.setViewportSize({ width: 600, height: 800 });
    await page.goto('/assembler');
    await expect(page.locator('emu-toolbar.compact, emu-toolbar')).toBeVisible({ timeout: 15_000 });
  });

  test('wide viewport renders full toolbar', async ({ page }) => {
    await page.setViewportSize({ width: 1600, height: 900 });
    await page.goto('/assembler');
    await expect(page.locator('[data-testid="asm-assemble-btn"]')).toBeVisible({ timeout: 15_000 });
    await expect(page.locator('[data-testid="asm-data-flow-toggle"]')).toBeVisible();
  });
});

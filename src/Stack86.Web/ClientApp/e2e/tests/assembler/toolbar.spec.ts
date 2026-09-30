import { test, expect } from '../../fixtures';

test.describe('Assembler — toolbar controls', () => {
  test('renders all primary toolbar buttons', async ({ page }) => {
    await page.goto('/assembler');
    await expect(page.locator('[data-testid="asm-assemble-btn"]')).toBeVisible({ timeout: 15_000 });
    await expect(page.locator('[data-testid="asm-run-btn"]')).toBeVisible();
    await expect(page.locator('[data-testid="asm-step-btn"]')).toBeVisible();
    await expect(page.locator('[data-testid="asm-pause-btn"]')).toBeVisible();
    await expect(page.locator('[data-testid="asm-reset-btn"]')).toBeVisible();
    await expect(page.locator('[data-testid="asm-sample-dropdown"]')).toBeVisible();
    await expect(page.locator('[data-testid="asm-data-flow-toggle"]')).toBeVisible();
  });

  test('speed slider updates label', async ({ page }) => {
    await page.goto('/assembler');
    const slider = page.locator('[data-testid="asm-speed-input"]');
    await slider.evaluate((el: HTMLInputElement) => {
      el.value = '50';
      el.dispatchEvent(new Event('input', { bubbles: true }));
    });
    await expect(page.locator('[data-testid="asm-speed-control"]')).toContainText('50');
  });

  test('data-flow toggle is clickable', async ({ page }) => {
    await page.goto('/assembler');
    const toggle = page.locator('[data-testid="asm-data-flow-toggle"] button');
    await expect(toggle).toBeVisible();
    await toggle.click();
    // After toggle, button should still be visible (data-flow panel is rendered separately)
    await expect(toggle).toBeVisible();
  });
});

import { test, expect } from '../../fixtures';

test.describe('Compiler toolbar — language and samples', () => {
  test('toolbar renders language and sample dropdowns', async ({ page }) => {
    await page.goto('/compiler');
    await expect(page.locator('[data-testid="compile-language-dropdown"]')).toBeVisible();
    await expect(page.locator('[data-testid="compile-sample-dropdown"]')).toBeVisible();
    await expect(page.locator('[data-testid="compile-btn"]')).toBeVisible();
  });
});

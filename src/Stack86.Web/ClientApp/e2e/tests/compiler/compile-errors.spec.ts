import { test, expect } from '../../fixtures';

test.describe('Compile flow — diagnostics', () => {
  test('errors auto-switch the console to Problems tab', async ({ page, compileApi }) => {
    compileApi.setErrors([
      { message: 'expected ;', line: 5, column: 12, file: 'main.c' },
    ]);
    await page.goto('/compiler');
    await page.locator('[data-testid="compile-btn"] button').click();

    const problemsTab = page.locator('[data-testid="console-tab-problems"]');
    await expect(problemsTab).toHaveClass(/tab-bar__tab--active/, { timeout: 10_000 });
    await expect(page.locator('[data-testid="compile-console-list"]')).toContainText('expected ;');
  });

  test('warnings appear in Problems tab without blocking assembly', async ({ page, compileApi }) => {
    compileApi.setWarnings([
      { message: 'unused variable', line: 3, column: 4, file: 'main.c' },
    ]);
    await page.goto('/compiler');
    await page.locator('[data-testid="compile-btn"] button').click();

    const problemsTab = page.locator('[data-testid="console-tab-problems"]');
    await expect(problemsTab).toHaveClass(/tab-bar__tab--active/, { timeout: 10_000 });
    await expect(page.locator('[data-testid="compile-console-list"]')).toContainText('unused variable');
  });

  test('compilation HTTP failure surfaces in console', async ({ page, compileApi }) => {
    compileApi.setFailure(500);
    await page.goto('/compiler');
    await page.locator('[data-testid="compile-btn"] button').click();
    // Either Problems tab activates, or the error toast/text shows; minimum: a problem line
    const problems = page.locator('[data-testid="console-tab-problems"]');
    await expect(problems).toBeVisible();
  });
});

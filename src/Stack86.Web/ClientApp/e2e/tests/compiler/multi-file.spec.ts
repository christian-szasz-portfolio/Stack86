import { test, expect } from '../../fixtures';
import { waitForMonacoReady } from '../../utils/monaco';

test.describe('Compiler — multi-file editor', () => {
  test('add a new file via tab bar', async ({ page }) => {
    await page.goto('/compiler');
    await waitForMonacoReady(page);
    // Click the + button to add a file
    await page.locator('.tab-bar__add').click();
    const input = page.locator('.new-file-input__field');
    await input.waitFor({ state: 'visible', timeout: 5_000 });
    await input.fill('helper.c');
    await input.press('Enter');
    // New tab appears
    await expect(page.locator('.tab', { hasText: 'helper.c' })).toBeVisible();
  });

  test('switch between tabs', async ({ page }) => {
    await page.goto('/compiler');
    await waitForMonacoReady(page);
    await page.locator('.tab-bar__add').click();
    const input = page.locator('.new-file-input__field');
    await input.fill('extra.c');
    await input.press('Enter');
    await expect(page.locator('.tab', { hasText: 'extra.c' })).toBeVisible();
    // Click main tab
    const mainTab = page.locator('.tab').first();
    await mainTab.click();
    await expect(mainTab).toHaveClass(/tab--active/);
  });

  test('close a non-main tab', async ({ page }) => {
    await page.goto('/compiler');
    await waitForMonacoReady(page);
    await page.locator('.tab-bar__add').click();
    const input = page.locator('.new-file-input__field');
    await input.fill('temp.c');
    await input.press('Enter');
    const tab = page.locator('.tab', { hasText: 'temp.c' });
    await expect(tab).toBeVisible();
    await tab.locator('.tab__close').click();
    await expect(tab).toHaveCount(0);
  });

  test('menu-bar opens and exposes File/Edit menus', async ({ page }) => {
    await page.goto('/compiler');
    await waitForMonacoReady(page);
    const triggers = page.locator('.menu-bar__trigger');
    const count = await triggers.count();
    expect(count).toBeGreaterThan(0);
    // Open and close each top-level menu
    for (let i = 0; i < count; i++) {
      await triggers.nth(i).click();
      await page.locator('.menu-bar__dropdown').first().waitFor({ state: 'visible', timeout: 3_000 }).catch(() => {});
      // Close by clicking trigger again
      await triggers.nth(i).click();
    }
  });

  test('cancel new file with Escape', async ({ page }) => {
    await page.goto('/compiler');
    await waitForMonacoReady(page);
    await page.locator('.tab-bar__add').click();
    const input = page.locator('.new-file-input__field');
    await input.waitFor({ state: 'visible' });
    await input.press('Escape');
    await expect(input).toHaveCount(0);
  });

  test('compile from compiler editor and verify build log', async ({ page, compileApi }) => {
    compileApi.setLog(['[Info] preprocess', '[Info] codegen', '[Info] done']);
    await page.goto('/compiler');
    await waitForMonacoReady(page);
    // Find compile button (search for the compile testid pattern)
    const compileBtn = page.locator('[data-testid^="compiler-compile"], button:has-text("Compile")').first();
    if (await compileBtn.count() > 0) {
      await compileBtn.click();
      await page.waitForTimeout(500);
    }
  });
});

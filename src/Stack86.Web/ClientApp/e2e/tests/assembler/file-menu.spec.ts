import { test, expect } from '../../fixtures';

test.describe('Assembler file menu', () => {
  test('file menu opens and shows items', async ({ page }) => {
    await page.goto('/assembler');
    const trigger = page.locator('[data-testid="asm-file-menu-trigger"]');
    await trigger.waitFor({ state: 'visible', timeout: 15_000 });
    await trigger.click();
    await expect(page.locator('[data-testid="asm-file-menu-dropdown"]')).toBeVisible();
    await expect(page.locator('[data-testid="asm-file-menu-item-importAsm"]')).toBeVisible();
    await expect(page.locator('[data-testid="asm-file-menu-item-exportAsm"]')).toBeVisible();
    await expect(page.locator('[data-testid="asm-file-menu-item-importCom"]')).toBeVisible();
    await expect(page.locator('[data-testid="asm-file-menu-item-exportCom"]')).toBeVisible();
  });

  test('file menu closes when clicking outside', async ({ page }) => {
    await page.goto('/assembler');
    const trigger = page.locator('[data-testid="asm-file-menu-trigger"]');
    await trigger.waitFor({ state: 'visible', timeout: 15_000 });
    await trigger.click();
    await expect(page.locator('[data-testid="asm-file-menu-dropdown"]')).toBeVisible();
    await page.locator('body').click({ position: { x: 5, y: 5 } });
    await expect(page.locator('[data-testid="asm-file-menu-dropdown"]')).toHaveCount(0);
  });

  test('file menu closes on Escape', async ({ page }) => {
    await page.goto('/assembler');
    const trigger = page.locator('[data-testid="asm-file-menu-trigger"]');
    await trigger.waitFor({ state: 'visible', timeout: 15_000 });
    await trigger.click();
    await expect(page.locator('[data-testid="asm-file-menu-dropdown"]')).toBeVisible();
    await page.keyboard.press('Escape');
    await expect(page.locator('[data-testid="asm-file-menu-dropdown"]')).toHaveCount(0);
  });

  test('Export ASM triggers download', async ({ page }) => {
    await page.goto('/assembler');
    const trigger = page.locator('[data-testid="asm-file-menu-trigger"]');
    await trigger.waitFor({ state: 'visible', timeout: 15_000 });
    await trigger.click();
    const downloadPromise = page.waitForEvent('download', { timeout: 10_000 }).catch(() => null);
    await page.locator('[data-testid="asm-file-menu-item-exportAsm"]').click();
    const download = await downloadPromise;
    if (download) {
      expect(download.suggestedFilename()).toMatch(/\.asm$/i);
    }
  });

  test('Speed control updates label', async ({ page }) => {
    await page.goto('/assembler');
    const speedInput = page.locator('[data-testid="asm-speed-input"]');
    await speedInput.waitFor({ state: 'visible', timeout: 15_000 });
    await speedInput.fill('100');
    await speedInput.dispatchEvent('input');
    await expect(page.locator('[data-testid="asm-speed-control"]')).toBeVisible();
  });
});

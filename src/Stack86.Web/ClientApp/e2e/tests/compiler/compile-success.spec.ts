import { test, expect } from '../../fixtures';

test.describe('Compile flow — success', () => {
  test('compile button renders generated assembly', async ({ page, compileApi }) => {
    compileApi.setAssembly('; mock\nMOV AX, 0001h\nINT 21h');
    await page.goto('/compiler');

    await page.locator('[data-testid="compile-toolbar"]').waitFor({ state: 'visible' });
    await page.locator('[data-testid="compile-btn"] button').click();

    // Build log appears
    await expect(page.locator('[data-testid="console-tab-build-log"]')).toBeVisible({ timeout: 10_000 });
    // Asm output panel becomes visible (Monaco renders the value)
    await expect(page.locator('[data-testid="compile-asm-output"]')).toBeVisible();
    // Load-into-assembler button appears once assembly is available
    await expect(page.locator('[data-testid="compile-load-into-asm-btn"]')).toBeVisible({ timeout: 10_000 });
  });

  test('clicking "Load into Assembler" navigates to /assembler', async ({ page, compileApi }) => {
    compileApi.setAssembly('MOV AX, 1h\nHLT');
    await page.goto('/compiler');
    await page.locator('[data-testid="compile-btn"] button').click();
    await page.locator('[data-testid="compile-load-into-asm-btn"]').waitFor({ state: 'visible', timeout: 10_000 });
    await page.locator('[data-testid="compile-load-into-asm-btn"] button').click();
    await expect(page).toHaveURL(/\/8086-emulator/, { timeout: 10_000 });
  });
});

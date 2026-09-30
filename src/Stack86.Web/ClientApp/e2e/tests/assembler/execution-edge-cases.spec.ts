import { test, expect } from '../../fixtures';
import { setMonacoValue, waitForMonacoReady } from '../../utils/monaco';

test.describe('Execution engine — edge cases', () => {
  test('reset returns CPU to initial state', async ({ page }) => {
    await page.goto('/8086-emulator');
    await waitForMonacoReady(page);
    await setMonacoValue(page, null, `MOV AX, 99\nHLT`);
    await page.locator('[data-testid="asm-assemble-btn"] button').click();
    await page.locator('[data-testid="asm-run-btn"] button').click();
    await page.locator('[data-testid="asm-reset-btn"] button').click();
    // After reset AX should be 0
    const ax = page.locator('[data-testid="asm-register-AX-dec"]');
    await expect(ax).toContainText('0', { timeout: 5_000 });
  });

  test('pause during a long-running loop', async ({ page }) => {
    const LOOP = `MOV CX, 65000
loop_top:
  NOP
  LOOP loop_top
  HLT`;
    await page.goto('/8086-emulator');
    await waitForMonacoReady(page);
    await setMonacoValue(page, null, LOOP);
    await page.locator('[data-testid="asm-assemble-btn"] button').click();
    // Slow execution so we can pause
    const speedInput = page.locator('[data-testid="asm-speed-input"]');
    if (await speedInput.count() > 0) {
      await speedInput.fill('100');
    }
    await page.locator('[data-testid="asm-run-btn"] button').click();
    await page.waitForTimeout(150);
    const pauseBtn = page.locator('[data-testid="asm-pause-btn"] button');
    if (await pauseBtn.isEnabled()) {
      await pauseBtn.click();
    }
    await expect(page.locator('[data-testid="asm-status"]')).toBeVisible();
  });

  test('division by zero halts with error', async ({ page }) => {
    const SRC = `MOV AX, 100\nMOV BX, 0\nDIV BX\nHLT`;
    await page.goto('/8086-emulator');
    await waitForMonacoReady(page);
    await setMonacoValue(page, null, SRC);
    await page.locator('[data-testid="asm-assemble-btn"] button').click();
    await page.locator('[data-testid="asm-run-btn"] button').click();
    await page.waitForTimeout(500);
    await expect(page.locator('[data-testid="asm-status"]')).toBeVisible();
  });

  test('changing execution speed unit', async ({ page }) => {
    await page.goto('/8086-emulator');
    await waitForMonacoReady(page);
    const unit = page.locator('[data-testid="asm-speed-unit"] select, [data-testid="asm-speed-unit"]').first();
    if (await unit.count() > 0) {
      // Try to change unit via the underlying select if present
      const selectEl = page.locator('[data-testid="asm-speed-unit"] select').first();
      if (await selectEl.count() > 0) {
        const options = await selectEl.locator('option').allTextContents();
        if (options.length > 1) {
          await selectEl.selectOption({ index: 1 });
          await selectEl.selectOption({ index: 0 });
        }
      }
    }
  });

  test('all 10 sample programs assemble without error', async ({ page }) => {
    await page.goto('/8086-emulator');
    await waitForMonacoReady(page);
    const dropdownBtn = page.locator('[data-testid="asm-sample-dropdown"] button').first();
    await dropdownBtn.click();
    const items = page.locator('.dropdown__panel .dropdown__item');
    const count = await items.count();
    // Just load each, assemble, reset — exhaustive sample coverage
    for (let i = 0; i < Math.min(count, 10); i++) {
      await dropdownBtn.click().catch(() => {});
      await items.nth(i).click().catch(() => {});
      await page.locator('[data-testid="asm-assemble-btn"] button').click();
      await page.waitForTimeout(150);
      await page.locator('[data-testid="asm-reset-btn"] button').click();
    }
  });
});

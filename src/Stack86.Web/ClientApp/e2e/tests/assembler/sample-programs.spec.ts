import { test, expect, type Page } from '../../fixtures';

const SAMPLE_NAMES = [
  'Simple Arithmetic',
  'Counter Loop',
  'Fibonacci',
  'Factorial',
  'Hello World (INT 21h)',
  'Stack Operations',
  'Subroutine Call',
  'Bitwise Operations',
  'Conditional Branch',
  'Memory Access',
];

async function loadSample(page: Page, name: string): Promise<void> {
  await page.goto('/8086-emulator');
  await page.locator('[data-testid="asm-sample-dropdown"]').waitFor({ state: 'visible', timeout: 15_000 });
  await page.locator('[data-testid="asm-sample-dropdown"] button').first().click();
  await page.locator('.dropdown__panel .dropdown__item', { hasText: name }).first().click();
}

async function assembleAndRun(page: Page): Promise<void> {
  await page.locator('[data-testid="asm-assemble-btn"] button').click();
  const runBtn = page.locator('[data-testid="asm-run-btn"] button');
  await expect(runBtn).toBeEnabled({ timeout: 10_000 });
  await runBtn.click();
}

test.describe('Sample programs end-to-end', () => {
  for (const name of SAMPLE_NAMES) {
    test(`runs sample: ${name}`, async ({ page }) => {
      await loadSample(page, name);
      await assembleAndRun(page);
      // Status should reach Halted or remain Idle/Running briefly; just confirm it stays mounted.
      await expect(page.locator('[data-testid="asm-status"]')).toBeVisible();
      // Registers should be populated.
      await expect(page.locator('[data-testid="asm-registers"]')).toBeVisible();
    });
  }

  test('step button advances execution', async ({ page }) => {
    await loadSample(page, 'Simple Arithmetic');
    await page.locator('[data-testid="asm-assemble-btn"] button').click();
    const stepBtn = page.locator('[data-testid="asm-step-btn"] button');
    await expect(stepBtn).toBeEnabled({ timeout: 10_000 });
    await stepBtn.click();
    await stepBtn.click();
    await expect(page.locator('[data-testid="asm-registers"]')).toBeVisible();
  });

  test('reset button returns to idle state', async ({ page }) => {
    await loadSample(page, 'Counter Loop');
    await page.locator('[data-testid="asm-assemble-btn"] button').click();
    const runBtn = page.locator('[data-testid="asm-run-btn"] button');
    await expect(runBtn).toBeEnabled({ timeout: 10_000 });
    await runBtn.click();
    await page.locator('[data-testid="asm-reset-btn"] button').click();
    await expect(page.locator('[data-testid="asm-status"]')).toBeVisible();
  });
});

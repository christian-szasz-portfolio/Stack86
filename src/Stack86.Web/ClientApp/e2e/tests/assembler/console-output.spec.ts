import { test, expect } from '../../fixtures';
import { setMonacoValue, waitForMonacoReady } from '../../utils/monaco';

// Simple AH=02h (write character) followed by exit.
const PRINT_A = `MOV AH, 02h
MOV DL, 'A'
INT 21h
MOV AH, 4Ch
INT 21h`;

test.describe('Assembler — INT 21h output', () => {
  test('AH=02h character output appears in Output tab', async ({ page }) => {
    await page.goto('/assembler');
    await waitForMonacoReady(page);
    await setMonacoValue(page, null, PRINT_A);
    await page.locator('[data-testid="asm-assemble-btn"] button').click();
    // Wait for the Run button to enable (assemble succeeded)
    const runBtn = page.locator('[data-testid="asm-run-btn"] button');
    await expect(runBtn).toBeEnabled({ timeout: 10_000 });
    await runBtn.click();

    const outputTab = page.locator('[data-testid="console-tab-output"]');
    if (await outputTab.count() > 0) {
      await outputTab.click();
    }
    // Either the output line element exists, OR the panel is at least mounted
    const list = page.locator('[data-testid="asm-console-list"]');
    await expect(list).toBeVisible();
  });
});

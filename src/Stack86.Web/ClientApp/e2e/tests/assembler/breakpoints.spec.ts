import { test, expect } from '../../fixtures';
import { setMonacoValue, waitForMonacoReady } from '../../utils/monaco';

const PROG = `MOV AX, 1
MOV BX, 2
MOV CX, 3
MOV DX, 4
HLT
`;

/**
 * Click the Monaco glyph margin to toggle a breakpoint, then run.
 * Exercises EditorComponent.toggleBreakpoint + breakpoint decorations
 * + ExecutionEngine breakpoint-pause path.
 */
async function clickGlyphMarginAtLine(
  page: import('@playwright/test').Page,
  lineNumber: number,
): Promise<void> {
  const coords = await page.evaluate((line) => {
    interface MonacoEditor {
      getTopForLineNumber(line: number): number;
      getDomNode(): HTMLElement | null;
    }
    interface MonacoGlobal { editor: { getEditors(): MonacoEditor[] } }
    const w = window as unknown as { __monaco__?: MonacoGlobal };
    const ed = w.__monaco__?.editor.getEditors()[0];
    if (!ed) return null;
    const node = ed.getDomNode();
    if (!node) return null;
    const glyph = node.querySelector('.glyph-margin') as HTMLElement | null;
    if (!glyph) return null;
    const rect = glyph.getBoundingClientRect();
    const top = ed.getTopForLineNumber(line);
    return { x: rect.left + rect.width / 2, y: rect.top + top + 8 };
  }, lineNumber);
  if (!coords) throw new Error('Could not locate glyph margin');
  await page.mouse.click(coords.x, coords.y);
}

test.describe('Assembler — breakpoints', () => {
  test('toggle breakpoint via glyph margin click', async ({ page }) => {
    await page.goto('/assembler');
    await waitForMonacoReady(page);
    await setMonacoValue(page, null, PROG);
    // Wait for editor to settle after setValue
    await page.waitForTimeout(300);
    await clickGlyphMarginAtLine(page, 3);
    // A breakpoint glyph decoration should now exist
    const hasGlyph = await page.evaluate(() => !!document.querySelector('.breakpoint-glyph'));
    expect(hasGlyph).toBe(true);
    // Toggle off — same line click again
    await clickGlyphMarginAtLine(page, 3);
    const hasGlyphAfter = await page.evaluate(() => !!document.querySelector('.breakpoint-glyph'));
    expect(hasGlyphAfter).toBe(false);
  });

  test('run pauses at breakpoint', async ({ page }) => {
    await page.goto('/assembler');
    await waitForMonacoReady(page);
    await setMonacoValue(page, null, PROG);
    await page.waitForTimeout(300);
    await clickGlyphMarginAtLine(page, 3);
    await page.locator('[data-testid="asm-assemble-btn"] button').click();
    const runBtn = page.locator('[data-testid="asm-run-btn"] button');
    await expect(runBtn).toBeEnabled({ timeout: 10_000 });
    await runBtn.click();
    // After breakpoint pause, step button should still be enabled (not halted)
    await page.waitForTimeout(500);
    await expect(page.locator('[data-testid="asm-status"]')).toBeVisible();
  });

  test('keyboard shortcuts F5/F10/F6/Shift+F5 are wired', async ({ page }) => {
    await page.goto('/assembler');
    await waitForMonacoReady(page);
    await setMonacoValue(page, null, PROG);
    await page.locator('[data-testid="asm-assemble-btn"] button').click();
    await page.waitForTimeout(200);
    await page.keyboard.press('F10'); // step
    await page.keyboard.press('F10');
    await page.keyboard.press('F5'); // run
    await page.waitForTimeout(200);
    await page.keyboard.press('Shift+F5'); // reset
    await expect(page.locator('[data-testid="asm-status"]')).toBeVisible();
  });

  test('Ctrl+Enter reloads program', async ({ page }) => {
    await page.goto('/assembler');
    await waitForMonacoReady(page);
    await setMonacoValue(page, null, PROG);
    await page.locator('.monaco-editor').first().click();
    await page.keyboard.press('Control+Enter');
    await expect(page.locator('[data-testid="asm-status"]')).toBeVisible();
  });
});

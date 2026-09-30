import type { Page, Locator } from '@playwright/test';

/**
 * Helpers for interacting with Monaco editors in e2e tests.
 * Monaco is loaded via dynamic import (not exposed on window), so we use
 * keyboard-based DOM interaction instead of the Monaco JS API.
 */

export async function waitForMonacoReady(page: Page, scope: Locator | null = null): Promise<void> {
  const root = scope ?? page.locator('body');
  await root.locator('.monaco-editor .view-lines').first().waitFor({ state: 'visible', timeout: 20_000 });
  // Wait for the global monaco hook installed by MonacoEditorService.
  await page.waitForFunction(() => '__monaco__' in window, undefined, { timeout: 20_000 });
}

export async function setMonacoValue(page: Page, scope: Locator | null, value: string): Promise<void> {
  const root = scope ?? page.locator('body');
  const editor = root.locator('.monaco-editor').first();
  await editor.waitFor({ state: 'visible', timeout: 20_000 });
  await page.waitForFunction(() => '__monaco__' in window, undefined, { timeout: 20_000 });

  // Drive the editor through Monaco's API for cross-browser reliability.
  const ok = await page.evaluate((newValue: string) => {
    interface MonacoGlobal {
      editor: { getEditors(): { setValue(v: string): void; focus(): void }[] };
    }
    const w = window as unknown as { __monaco__?: MonacoGlobal };
    const editors = w.__monaco__?.editor.getEditors() ?? [];
    if (editors.length === 0) return false;
    const ed = editors[0];
    ed.focus();
    ed.setValue(newValue);
    return true;
  }, value);

  if (!ok) {
    // Fallback: keyboard-driven entry.
    await editor.locator('.view-lines').first().click();
    const isMac = process.platform === 'darwin';
    await page.keyboard.press(isMac ? 'Meta+A' : 'Control+A');
    await page.keyboard.press('Delete');
    await page.keyboard.insertText(value);
  }
}

export async function getMonacoValue(page: Page, scope: Locator | null): Promise<string> {
  const root = scope ?? page.locator('body');
  return root.locator('.monaco-editor .view-lines').first().innerText();
}

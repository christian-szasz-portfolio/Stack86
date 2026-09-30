import { test, expect } from '../fixtures';

test.describe('App shell + routing', () => {
  test('root redirects to /assembler', async ({ page }) => {
    await page.goto('/');
    await expect(page).toHaveURL(/\/assembler/);
  });

  test('unknown route redirects to /assembler', async ({ page }) => {
    await page.goto('/no-such-route');
    await expect(page).toHaveURL(/\/assembler/);
  });
});

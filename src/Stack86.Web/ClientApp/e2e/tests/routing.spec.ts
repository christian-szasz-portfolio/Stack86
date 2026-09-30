import { test, expect } from '../fixtures';

test.describe('App shell + routing', () => {
  test('root redirects to /assembler', async ({ page }) => {
    await page.goto('/');
    await expect(page).toHaveURL(/\/8086-emulator/);
  });

  test('unknown route redirects to /assembler', async ({ page }) => {
    await page.goto('/no-such-route');
    await expect(page).toHaveURL(/\/8086-emulator/);
  });
});

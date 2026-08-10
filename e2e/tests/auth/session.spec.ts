import { test, expect } from '../../fixtures/test-fixtures';
import { storageStateFor } from '../../utils/roles';

test.describe('session handling', () => {
  test.use({ storageState: storageStateFor('owner') });

  test('signing out clears the token @smoke', async ({ page, nav }) => {
    await page.goto('/books');

    await nav.logout();

    await expect(page).toHaveURL(/\/login/);
    expect(await page.evaluate(() => localStorage.getItem('token'))).toBeNull();
  });

  test('signing out prevents going back to a protected page', async ({ page, nav }) => {
    await page.goto('/books');
    await nav.logout();
    await expect(page).toHaveURL(/\/login/);

    await page.goBack();

    await expect(page).toHaveURL(/\/login/, { timeout: 20_000 });
  });

  test('a session survives a reload', async ({ page }) => {
    await page.goto('/books');

    await page.reload();

    await expect(page).toHaveURL(/\/books/);
  });

  test('an authenticated visit to the root lands on the catalogue', async ({ page }) => {
    await page.goto('/');

    await expect(page).toHaveURL(/\/books/);
  });

  test('theme choice persists across reloads', async ({ page, nav }) => {
    await page.goto('/books');

    await nav.setTheme('dark');
    await page.reload();

    expect(await page.evaluate(() => localStorage.getItem('theme'))).toBe('dark');
  });
});

test.describe('password reset request', () => {
  test.use({ storageState: { cookies: [], origins: [] } });

  test('accepts an address without confirming whether it exists', async ({ page }) => {
    // The response must be identical either way, or the form leaks which addresses
    // are registered.
    await page.goto('/reset-password');

    await page.locator('#email').fill('nobody@example.invalid');
    await page.locator('#send-reset-link').click();

    await expect(
      page.getByText('If this email exists, a reset link has been sent.'),
    ).toBeVisible();
  });
});

import { test, expect } from '../../fixtures/test-fixtures';
import { storageStateFor } from '../../utils/roles';

test.describe('support portal', () => {
  test.use({ storageState: storageStateFor('superadmin') });

  test('lands on the dashboard @smoke', async ({ page }) => {
    await page.goto('/support');

    await expect(page.getByRole('heading', { name: 'Support Dashboard' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'MBC Support' })).toBeVisible();
  });

  test('navigates between every section', async ({ page }) => {
    await page.goto('/support');

    for (const [label, urlPart] of [
      ['Tenants', 'tenants'],
      ['Users', 'users'],
      ['Books', 'books'],
      ['Report Logs', 'report-logs'],
    ] as const) {
      await page.getByRole('link', { name: label, exact: true }).click();
      await expect(page).toHaveURL(new RegExp(urlPart));
    }
  });

  test('lists tenants', async ({ page }) => {
    await page.goto('/support/tenants');

    await expect(page.locator('table')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Create New Account' })).toBeVisible();
  });

  test('tenant creation opens the signup form', async ({ page }) => {
    await page.goto('/support/tenants');
    await page.getByRole('button', { name: 'Create New Account' }).click();

    await expect(page).toHaveURL(/\/support\/tenants\/new/);
    await expect(page.getByRole('heading', { name: 'Create Your Account' })).toBeVisible();
  });

  test('lists users across tenants', async ({ page }) => {
    await page.goto('/support/users');

    await expect(page.locator('table')).toBeVisible();
    await expect(page.getByText('Tenant ID')).toBeVisible();
  });

  test('superadmins cannot be impersonated', async ({ page }) => {
    // Impersonating another superadmin would be an unlogged privilege loop.
    await page.goto('/support/users');

    const superadminRows = page.locator('tr').filter({ hasText: 'SuperAdmin' });
    const count = await superadminRows.count();
    test.skip(count === 0, 'no superadmin rows visible');

    await expect(
      superadminRows.first().locator('button[matTooltip="Impersonate user"]'),
    ).toHaveCount(0);
  });

  test('lists books across tenants', async ({ page }) => {
    await page.goto('/support/books');

    await expect(page.locator('table')).toBeVisible();
    await expect(page.getByPlaceholder('Search by title...')).toBeVisible();
  });

  test('books can be filtered by tenant', async ({ page }) => {
    await page.goto('/support/books');

    await page.getByLabel('Filter by Tenant').click();

    await expect(
      page.locator('.mat-mdc-select-panel').getByRole('option', { name: 'All Tenants' }),
    ).toBeVisible();
  });
});

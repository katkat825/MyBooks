import { test, expect } from '../../fixtures/test-fixtures';
import { storageStateFor } from '../../utils/roles';

/**
 * The most valuable tests in this suite. Every one of these asserts that a role cannot
 * reach something it should not, which is the failure mode that does real damage.
 */
test.describe('unauthenticated access', () => {
  test.use({ storageState: { cookies: [], origins: [] } });

  const guarded = [
    '/books',
    '/create',
    '/profile',
    '/account/users',
    '/account/integrations',
    '/account/bulk-import',
    '/account/genres-series',
    '/support',
    '/support/tenants',
    '/support/users',
    '/support/books',
    '/support/report-logs',
    '/global/content-review',
  ];

  for (const path of guarded) {
    test(`${path} redirects to login`, async ({ page }) => {
      await page.goto(path);

      await expect(page).toHaveURL(/\/login/, { timeout: 20_000 });
    });
  }

  test('a discarded token does not grant access', async ({ page }) => {
    await page.addInitScript(() => localStorage.setItem('token', 'not-a-real-jwt'));

    await page.goto('/account/users');

    // The guard only checks that a token exists, so the server must be what rejects this.
    // Landing on the page with data visible would mean the API trusted a forged token.
    await expect(page.locator('table')).toHaveCount(0);
  });
});

test.describe('reader role', () => {
  test.use({ storageState: storageStateFor('user') });

  const forbidden = [
    '/create',
    '/account/users',
    '/account/integrations',
    '/account/bulk-import',
    '/account/genres-series',
    '/support',
    '/global/content-review',
  ];

  for (const path of forbidden) {
    test(`cannot reach ${path}`, async ({ page }) => {
      await page.goto(path);

      await expect(page).not.toHaveURL(new RegExp(path.replace(/\//g, '\\/')), {
        timeout: 20_000,
      });
    });
  }

  test('sees no owner menu', async ({ page, nav }) => {
    await page.goto('/books');
    await nav.openAccountMenu();

    await expect(nav.ownerMenuTrigger()).toHaveCount(0);
  });

  test('sees no support link', async ({ page, nav }) => {
    await page.goto('/books');
    await nav.openAccountMenu();

    await expect(nav.supportLink()).toHaveCount(0);
  });

  test('can still read the catalogue @smoke', async ({ page, bookList }) => {
    await bookList.goto();

    await expect(page).toHaveURL(/\/books/);
  });

  test('has no add-book affordance', async ({ bookList }) => {
    await bookList.goto();

    await expect(bookList.addBook).toHaveCount(0);
  });
});

test.describe('owner role', () => {
  test.use({ storageState: storageStateFor('owner') });

  test('reaches every account page', async ({ page }) => {
    for (const path of [
      '/account/users',
      '/account/integrations',
      '/account/bulk-import',
      '/account/genres-series',
    ]) {
      await page.goto(path);
      await expect(page).toHaveURL(new RegExp(path.replace(/\//g, '\\/')));
    }
  });

  test('cannot reach the support portal', async ({ page }) => {
    // Owner is the top of a tenant. Support is above all tenants and must stay separate.
    await page.goto('/support');

    await expect(page).not.toHaveURL(/\/support$/, { timeout: 20_000 });
  });

  test('redirects the bare account path to users', async ({ page }) => {
    await page.goto('/account');

    await expect(page).toHaveURL(/\/account\/users/);
  });
});

test.describe('superadmin role', () => {
  test.use({ storageState: storageStateFor('superadmin') });

  test('reaches the support portal @smoke', async ({ page }) => {
    await page.goto('/support');

    await expect(page.getByRole('heading', { name: 'Support Dashboard' })).toBeVisible();
  });

  test('reaches every support page', async ({ page }) => {
    for (const path of ['/support/tenants', '/support/users', '/support/books', '/support/report-logs']) {
      await page.goto(path);
      await expect(page).toHaveURL(new RegExp(path.replace(/\//g, '\\/')));
    }
  });
});

test.describe('global reviewer role', () => {
  test.use({ storageState: storageStateFor('reviewer') });

  test('reaches content review', async ({ page }) => {
    await page.goto('/global/content-review');

    await expect(page).toHaveURL(/content-review/);
  });

  test('cannot reach tenant administration', async ({ page }) => {
    // A reviewer reads content across tenants but must not administer any of them.
    await page.goto('/account/users');

    await expect(page).not.toHaveURL(/\/account\/users/, { timeout: 20_000 });
  });
});

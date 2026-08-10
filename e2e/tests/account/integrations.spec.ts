import { test, expect } from '../../fixtures/test-fixtures';
import { storageStateFor } from '../../utils/roles';

/**
 * The OAuth handshake itself cannot be automated without driving Google's own consent
 * screen, which is out of scope and hostile to automation. These tests cover everything
 * on this side of the redirect.
 */
test.describe('Google Drive integrations', () => {
  test.use({ storageState: storageStateFor('owner') });

  test.beforeEach(async ({ integrations }) => {
    await integrations.goto();
  });

  test('shows the integrations page @smoke', async ({ page }) => {
    await expect(page.getByRole('heading', { name: 'Manage Integrations' })).toBeVisible();
  });

  test('offers a connect action', async ({ integrations }) => {
    await expect(integrations.connect).toBeVisible();
  });

  test('connecting hands off to Google', async ({ integrations, page }) => {
    // Assert on the outbound authorisation URL rather than following it. This is where a
    // wrong scope or a missing client id would show up.
    const [request] = await Promise.all([
      page.waitForRequest((r) => r.url().includes('accounts.google.com'), { timeout: 20_000 }),
      integrations.connect.click(),
    ]);

    const url = new URL(request.url());
    expect(url.searchParams.get('scope')).toContain('drive.file');
    expect(url.searchParams.get('client_id')).toBeTruthy();
    expect(url.searchParams.get('response_type')).toBe('code');
  });

  test('requests only the non-sensitive Drive scope', async ({ integrations, page }) => {
    // A regression to drive.readonly here would silently reintroduce the restricted-scope
    // verification requirement the whole design avoids.
    const [request] = await Promise.all([
      page.waitForRequest((r) => r.url().includes('accounts.google.com'), { timeout: 20_000 }),
      integrations.connect.click(),
    ]);

    const scope = new URL(request.url()).searchParams.get('scope') ?? '';
    expect(scope).not.toContain('drive.readonly');
    expect(scope).not.toMatch(/auth\/drive(\s|$)/);
  });

  test('an existing integration can be removed', async ({ integrations, page }) => {
    const count = await integrations.removeButtons.count();
    test.skip(count === 0, 'no connected integration in this environment');

    await integrations.removeButtons.first().click();

    await expect(page.locator('mat-dialog-container')).toBeVisible();
  });
});

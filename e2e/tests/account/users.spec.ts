import { test, expect } from '../../fixtures/test-fixtures';
import { storageStateFor } from '../../utils/roles';
import { uniqueEmail } from '../../utils/unique';

test.describe('managing users', () => {
  test.use({ storageState: storageStateFor('owner') });

  test.beforeEach(async ({ accountUsers }) => {
    await accountUsers.goto();
  });

  test('lists the tenant users @smoke', async ({ page }) => {
    await expect(page.locator('table')).toBeVisible();
  });

  test('shows seat usage against the plan limit', async ({ accountUsers }) => {
    await expect(accountUsers.seatCounter).toContainText(/Active Users:\s*\d+\s*\/\s*\d+/);
  });

  test('opens and closes the add-user form', async ({ accountUsers, page }) => {
    await accountUsers.toggleAddUser.click();
    await expect(page.locator('mat-card.add-user-card')).toBeVisible();

    await accountUsers.toggleAddUser.click();
    await expect(page.locator('mat-card.add-user-card')).toBeHidden();
  });

  test('rejects a malformed email', async ({ accountUsers, page }) => {
    await accountUsers.toggleAddUser.click();
    await accountUsers.email.fill('not-an-email');
    await accountUsers.firstName.click();

    await expect(page.getByText('Invalid email format')).toBeVisible();
  });

  test('rejects an address already in use', async ({ accountUsers, page }) => {
    const existing = await page
      .locator('td')
      .filter({ hasText: '@' })
      .first()
      .textContent()
      .catch(() => null);

    test.skip(!existing, 'no existing user to collide with');

    await accountUsers.toggleAddUser.click();
    await accountUsers.email.fill(existing!.trim());
    await accountUsers.firstName.click();

    await expect(page.getByText('This email address is already in use')).toBeVisible({
      timeout: 15_000,
    });
  });

  test('invites a new user', async ({ accountUsers }) => {
    const email = uniqueEmail('invitee');

    await accountUsers.addUser('Grace', 'Hopper', email, 'Adult');

    await expect(accountUsers.rowFor(email)).toBeVisible({ timeout: 20_000 });
  });

  test('toggles inactive users into view', async ({ accountUsers }) => {
    await expect(accountUsers.toggleInactive).toContainText('Show Inactive Users');

    await accountUsers.toggleInactive.click();

    await expect(accountUsers.toggleInactive).toContainText('Hide Inactive Users');
  });
});

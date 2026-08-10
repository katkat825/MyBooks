import { test, expect } from '../../fixtures/test-fixtures';
import { storageStateFor } from '../../utils/roles';
import { unique } from '../../utils/unique';

test.describe('my profile', () => {
  test.use({ storageState: storageStateFor('owner') });

  test.beforeEach(async ({ profile }) => {
    await profile.goto();
  });

  test('loads the current details @smoke', async ({ profile }) => {
    await expect(profile.firstName).not.toBeEmpty();
    await expect(profile.email).not.toBeEmpty();
  });

  test('leaves the password field blank by design', async ({ profile }) => {
    // A pre-filled password box would round-trip the stored value to the browser.
    await expect(profile.password).toBeEmpty();
    await expect(profile.password).toHaveAttribute('placeholder', /leave blank/i);
  });

  test('saves a changed name', async ({ profile, page }) => {
    const newName = unique('Ada');

    await profile.firstName.fill(newName);
    await profile.save.click();
    await page.reload();

    await expect(profile.firstName).toHaveValue(newName);
  });

  test('rejects a malformed email', async ({ profile }) => {
    await profile.email.fill('not-an-email');

    await expect(profile.save).toBeDisabled();
  });

  test('is reachable from the toolbar', async ({ page, nav }) => {
    await page.goto('/books');
    await nav.gotoProfile();

    await expect(page).toHaveURL(/\/profile/);
  });
});

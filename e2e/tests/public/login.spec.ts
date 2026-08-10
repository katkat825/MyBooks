import { test, expect } from '@playwright/test';
import { LoginPage } from '../../pages/login.page';
import { CREDENTIALS } from '../../utils/roles';

test.describe('sign in', () => {
  let login: LoginPage;

  test.beforeEach(async ({ page }) => {
    login = new LoginPage(page);
    await login.goto();
  });

  test('submit stays disabled until the form is valid', async () => {
    await expect(login.submit).toBeDisabled();

    await login.email.fill('not-an-email');
    await login.password.fill('something');
    await expect(login.submit).toBeDisabled();

    await login.email.fill('valid@example.com');
    await expect(login.submit).toBeEnabled();
  });

  test('rejects an unknown account', async () => {
    await login.login('nobody@example.invalid', 'wrong-password');

    await login.expectRejected();
  });

  test('rejects a wrong password', async () => {
    await login.login(CREDENTIALS.owner.email, 'definitely-not-the-password');

    await login.expectRejected();
  });

  test('does not reveal whether an address has an account', async ({ page }) => {
    // Both branches must produce the same message, or the form becomes an account oracle.
    await login.login('nobody@example.invalid', 'wrong-password');
    const unknownAccountError = await login.error.textContent();

    await page.reload();
    await login.login(CREDENTIALS.owner.email, 'definitely-not-the-password');
    const wrongPasswordError = await login.error.textContent();

    expect(unknownAccountError).toBe(wrongPasswordError);
  });

  test('stores no token after a failed attempt', async () => {
    await login.login('nobody@example.invalid', 'wrong-password');
    await login.expectRejected();

    expect(await login.token()).toBeNull();
  });

  test('accepts valid credentials @smoke', async () => {
    await login.login(CREDENTIALS.owner.email, CREDENTIALS.owner.password);

    await login.expectSignedIn();
    expect(await login.token()).not.toBeNull();
  });

  test('offers a password reset route', async ({ page }) => {
    await login.forgotPasswordLink.click();

    await expect(page).toHaveURL(/\/reset-password/);
  });
});

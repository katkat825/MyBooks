import { test, expect } from '@playwright/test';
import { uniqueEmail } from '../../utils/unique';

test.describe('tenant signup', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/signup');
  });

  test('is reachable without an account @smoke', async ({ page }) => {
    await expect(page.getByRole('heading', { name: 'Create Your Account' })).toBeVisible();
    await expect(page.locator('#create-account')).toBeVisible();
  });

  test('blocks submission until every field is filled', async ({ page }) => {
    await expect(page.locator('#create-account')).toBeDisabled();

    await page.getByLabel('First Name').fill('Ada');
    await page.getByLabel('Last Name').fill('Lovelace');
    await expect(page.locator('#create-account')).toBeDisabled();
  });

  test('rejects a malformed email', async ({ page }) => {
    await page.getByLabel('Email Address').fill('not-an-email');
    // The email control validates on blur, not on keystroke.
    await page.getByLabel('Password').click();

    await expect(page.getByText('Invalid email format')).toBeVisible();
  });

  test('enforces a minimum password length', async ({ page }) => {
    await page.getByLabel('Password').fill('short');
    await page.getByLabel('First Name').click();

    await expect(page.getByText('Minimum of 6 characters required.')).toBeVisible();
  });

  test('enables submission once the form is valid', async ({ page }) => {
    await page.getByLabel('First Name').fill('Ada');
    await page.getByLabel('Last Name').fill('Lovelace');
    await page.getByLabel('Email Address').fill(uniqueEmail('tenant'));
    await page.getByLabel('Password').fill('a-long-enough-password');
    await page.getByLabel('First Name').click();

    await expect(page.locator('#create-account')).toBeEnabled();
  });
});

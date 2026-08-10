import { test as setup, expect } from '@playwright/test';
import * as fs from 'fs';
import { CREDENTIALS, Role, storageStateFor } from '../utils/roles';
import { STORAGE_STATE_DIR } from '../playwright.config';

/**
 * Signing in through the real form once per role, rather than minting tokens directly.
 * A forged token would keep passing after the login flow broke, which defeats the point.
 */
const signIn = async (role: Role, page: import('@playwright/test').Page) => {
  const { email, password } = CREDENTIALS[role];

  if (!password) {
    throw new Error(
      `No password configured for the "${role}" role. Copy .env.example to .env and fill it in.`,
    );
  }

  await page.goto('/login');
  await page.locator('.login-container #email').fill(email);
  await page.locator('.login-container #password').fill(password);
  await page.locator('.login-container #login-btn').click();

  // The app routes to /books on success. Landing anywhere else means the credentials or
  // the environment are wrong, and every downstream failure would be misleading.
  await expect(page).toHaveURL(/\/books/, { timeout: 20_000 });
  await expect
    .poll(() => page.evaluate(() => localStorage.getItem('token')))
    .not.toBeNull();

  fs.mkdirSync(STORAGE_STATE_DIR, { recursive: true });
  await page.context().storageState({ path: storageStateFor(role) });
};

setup('authenticate as owner', async ({ page }) => signIn('owner', page));
setup('authenticate as user', async ({ page }) => signIn('user', page));
setup('authenticate as superadmin', async ({ page }) => signIn('superadmin', page));
setup('authenticate as reviewer', async ({ page }) => signIn('reviewer', page));

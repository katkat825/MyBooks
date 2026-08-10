import { test, expect } from '@playwright/test';

/**
 * Everything reachable without a session. These run in the "public" project, which is
 * configured with an empty storage state so a leaked token cannot mask a broken guard.
 */
test.describe('public pages', () => {
  test('home page renders the pitch @smoke', async ({ page }) => {
    await page.goto('/');

    await expect(page.getByRole('heading', { name: 'Welcome to My Book Catalog' })).toBeVisible();
    await expect(page.locator('section.home-wrapper')).toBeVisible();
  });

  test('home page explains how it works', async ({ page }) => {
    await page.goto('/');

    await expect(page.getByRole('heading', { name: 'How It Works' })).toBeVisible();
    await expect(page.locator('.steps .step')).not.toHaveCount(0);
  });

  test('home page offers a route to sign in', async ({ page }) => {
    await page.goto('/');

    await page.locator('.home-wrapper #login-btn').click();

    await expect(page).toHaveURL(/\/login/);
  });

  test('terms of service is readable without an account', async ({ page }) => {
    await page.goto('/terms');

    await expect(page.locator('#accept-terms')).toBeVisible();
  });

  test('privacy policy is readable without an account', async ({ page }) => {
    await page.goto('/privacy');

    await expect(page.locator('body')).toContainText(/privacy/i);
  });

  test('footer links reach both policies', async ({ page }) => {
    await page.goto('/');

    await page.locator('#terms-link').click();
    await expect(page).toHaveURL(/\/terms/);

    await page.locator('#privacy-link').click();
    await expect(page).toHaveURL(/\/privacy/);
  });

  test('home page is usable on a phone @mobile', async ({ page }) => {
    await page.goto('/');

    await expect(page.getByRole('heading', { name: 'Welcome to My Book Catalog' })).toBeVisible();
  });
});

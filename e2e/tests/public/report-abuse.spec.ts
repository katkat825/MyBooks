import { test, expect } from '@playwright/test';
import { uniqueEmail } from '../../utils/unique';

/**
 * Abuse reporting has to work for someone with no account, since the person reporting
 * content is usually not the person hosting it.
 */
test.describe('abuse reporting', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/report-abuse');
  });

  test('is reachable without signing in @smoke', async ({ page }) => {
    await expect(page.locator('#abuse-description')).toBeVisible();
    await expect(page.locator('#submit-report')).toBeVisible();
  });

  test('requires a description', async ({ page }) => {
    await page.locator('#additional-email').fill(uniqueEmail('reporter'));

    await expect(page.locator('#submit-report')).toBeDisabled();
  });

  test('accepts a complete report', async ({ page }) => {
    await page.locator('#abuse-description').fill(
      'Automated end-to-end regression check. Please disregard.',
    );
    await page.locator('#additional-email').fill(uniqueEmail('reporter'));

    await expect(page.locator('#submit-report')).toBeEnabled();
  });

  test('is reachable from the footer of a public page', async ({ page }) => {
    await page.goto('/');
    await page.locator('#report-abuse-btn').click();

    await expect(page).toHaveURL(/\/report-abuse/);
  });
});

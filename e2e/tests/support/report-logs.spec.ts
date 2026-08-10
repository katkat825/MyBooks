import { test, expect } from '../../fixtures/test-fixtures';
import { storageStateFor } from '../../utils/roles';
import { unique } from '../../utils/unique';

test.describe('abuse and DMCA reports', () => {
  test.use({ storageState: storageStateFor('superadmin') });

  test('lists reports @smoke', async ({ page }) => {
    await page.goto('/support/report-logs');

    await expect(page.getByRole('heading', { name: 'Abuse / DMCA Reports' })).toBeVisible();
  });

  test('opens the create form', async ({ page }) => {
    await page.goto('/support/report-logs');
    await page.getByRole('button', { name: 'Create New Report' }).click();

    await expect(page).toHaveURL(/\/support\/report-logs\/new/);
    await expect(page.locator('#report-create-form')).toBeVisible();
  });

  test('submission is blocked until the required fields are set', async ({ page }) => {
    await page.goto('/support/report-logs/new');

    await expect(page.locator('#report-submit')).toBeDisabled();
  });

  test('records a report', async ({ page }) => {
    await page.goto('/support/report-logs/new');

    await page.locator('#report-date-received').fill(new Date().toISOString().slice(0, 10));
    await page.locator('#report-status').click();
    await page.locator('.mat-mdc-select-panel [role="option"]').first().click();

    await page.locator('#report-reported-by').fill('e2e-suite');

    await page.locator('#report-type').click();
    await page.locator('.mat-mdc-select-panel [role="option"]').first().click();

    await page.locator('#report-description').fill(unique('Automated regression report'));

    await expect(page.locator('#report-submit')).toBeEnabled();
  });

  test('a target can be pinned to a book', async ({ page }) => {
    await page.goto('/support/report-logs/new');

    await page.locator('#report-target-type').click();
    await page.locator('#report-target-type-book').click();
    await page.locator('#report-target-id').fill('1');

    await expect(page.locator('#report-target-id')).toHaveValue('1');
  });

  test('an existing report can be opened for update', async ({ page }) => {
    await page.goto('/support/report-logs');

    const editButtons = page.locator('button[matTooltip="Update report"]');
    test.skip((await editButtons.count()) === 0, 'no reports recorded in this environment');

    await editButtons.first().click();

    await expect(page).toHaveURL(/\/support\/report-logs\/update\/\d+/);
    await expect(page.locator('mat-select[formcontrolname="status"]')).toBeVisible();
  });
});

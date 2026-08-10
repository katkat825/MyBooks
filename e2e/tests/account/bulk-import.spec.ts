import { test, expect } from '../../fixtures/test-fixtures';
import { storageStateFor } from '../../utils/roles';

test.describe('bulk import', () => {
  test.use({ storageState: storageStateFor('owner') });

  test.beforeEach(async ({ bulkImport }) => {
    await bulkImport.goto();
  });

  test('shows the import page @smoke', async ({ bulkImport }) => {
    await expect(bulkImport.selectFolder).toBeVisible();
  });

  test('import is disabled until files are chosen', async ({ bulkImport }) => {
    // The Google Picker is a cross-origin iframe and cannot be driven from here, so the
    // pre-selection state is what this suite can assert.
    await expect(bulkImport.startImport).toBeDisabled();
  });

  test('shows an empty state before anything is selected', async ({ bulkImport }) => {
    await expect(bulkImport.emptyState).toBeVisible();
  });

  test('past jobs are viewable', async ({ bulkImport, page }) => {
    await bulkImport.viewJobs.click();

    await expect(page.getByRole('heading', { name: 'Bulk Import Jobs' })).toBeVisible();
  });

  test('the jobs dialog closes cleanly', async ({ bulkImport, page }) => {
    await bulkImport.viewJobs.click();
    await expect(bulkImport.jobsDialog).toBeVisible();

    await page.getByRole('button', { name: 'Close' }).click();

    await expect(bulkImport.jobsDialog).toBeHidden();
  });

  test('opening the picker requires an integration', async ({ bulkImport, page }) => {
    await bulkImport.selectFolder.click();

    // Either the Google picker opens or the app explains that Drive is not connected.
    // Silently doing nothing is the failure this catches.
    await expect
      .poll(async () =>
        (await page.locator('iframe[src*="google"]').count()) +
        (await page.locator('simple-snack-bar, mat-dialog-container').count()),
      { timeout: 20_000 })
      .toBeGreaterThan(0);
  });
});

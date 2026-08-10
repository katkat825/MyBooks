import { test, expect } from '../../fixtures/test-fixtures';
import { storageStateFor } from '../../utils/roles';
import { unique } from '../../utils/unique';

test.describe('genres and series', () => {
  test.use({ storageState: storageStateFor('owner') });

  test.beforeEach(async ({ admin }) => {
    await admin.goto();
  });

  test('shows both tabs @smoke', async ({ page }) => {
    await expect(page.getByRole('tab', { name: 'Manage Genres' })).toBeVisible();
    await expect(page.getByRole('tab', { name: 'Manage Series' })).toBeVisible();
  });

  test('creates a genre', async ({ admin }) => {
    const name = unique('E2E Genre');

    await admin.addGenre.click();
    await admin.genreNameInput.fill(name);
    await admin.saveGenre.click();

    await expect(admin.genreRow(name)).toBeVisible({ timeout: 20_000 });
  });

  test('creates a series', async ({ admin, page }) => {
    const name = unique('E2E Series');

    await page.getByRole('tab', { name: 'Manage Series' }).click();
    await admin.addSeries.click();
    await admin.seriesNameInput.fill(name);
    await admin.saveSeries.click();

    await expect(admin.seriesRow(name)).toBeVisible({ timeout: 20_000 });
  });

  test('a new genre is offered when creating a book', async ({ admin, bookForm, page }) => {
    const name = unique('E2E Genre');

    await admin.addGenre.click();
    await admin.genreNameInput.fill(name);
    await admin.saveGenre.click();
    await expect(admin.genreRow(name)).toBeVisible({ timeout: 20_000 });

    await bookForm.goto();
    await page.locator('mat-select[formcontrolname="genreId"]').click();

    await expect(
      page.locator('.mat-mdc-select-panel').getByRole('option', { name }),
    ).toBeVisible();
  });
});

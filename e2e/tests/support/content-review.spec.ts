import { test, expect } from '../../fixtures/test-fixtures';
import { storageStateFor } from '../../utils/roles';

test.describe('global content review', () => {
  test.use({ storageState: storageStateFor('reviewer') });

  test('lists books for review @smoke', async ({ page }) => {
    await page.goto('/global/content-review');

    await expect(page.getByLabel('Search Books')).toBeVisible();
  });

  test('books can be searched', async ({ page }) => {
    await page.goto('/global/content-review');

    await page.getByPlaceholder('title, author, series, or genre...').fill('zzz-no-match');

    await expect(page.locator('table tbody tr')).toHaveCount(0);
  });

  test('a reviewer can open the support viewer', async ({ page }) => {
    await page.goto('/global/content-review');

    const readButtons = page.locator('button[matTooltip="Read Book"]');
    test.skip((await readButtons.count()) === 0, 'no reviewable books in this environment');

    await readButtons.first().click();

    await expect(page).toHaveURL(/\/support\/book-viewer\/\d+/);
  });
});

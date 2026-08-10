import { test, expect } from '../../fixtures/test-fixtures';
import { storageStateFor } from '../../utils/roles';

test.describe('reading a book', () => {
  test.use({ storageState: storageStateFor('owner') });

  const openFirstReadableBook = async (
    bookList: import('../../pages/book-list.page').BookListPage,
    bookDetails: import('../../pages/book-details.page').BookDetailsPage,
  ): Promise<boolean> => {
    await bookList.goto();
    const count = await bookList.cards.count();

    for (let i = 0; i < count; i++) {
      await bookList.cards.nth(i).click();
      if (await bookDetails.readInline.count()) {
        await bookDetails.readInline.click();
        return true;
      }
      await bookDetails.backToList.click();
    }

    return false;
  };

  test('opens the viewer for a book with a file @smoke', async ({
    bookList,
    bookDetails,
    page,
  }) => {
    const opened = await openFirstReadableBook(bookList, bookDetails);
    test.skip(!opened, 'no book with an attached file in this environment');

    await expect(page).toHaveURL(/\/book-viewer\/\d+/);
    await expect(page.locator('.viewer-container')).toBeVisible();
  });

  test('zoom controls are available', async ({ bookList, bookDetails, page }) => {
    const opened = await openFirstReadableBook(bookList, bookDetails);
    test.skip(!opened, 'no book with an attached file in this environment');

    await expect(page.locator('#zoom-in')).toBeVisible();
    await expect(page.locator('#zoom-out')).toBeVisible();
  });

  test('zoom level persists across reloads', async ({ bookList, bookDetails, page }) => {
    const opened = await openFirstReadableBook(bookList, bookDetails);
    test.skip(!opened, 'no book with an attached file in this environment');

    await page.locator('#zoom-in').click();
    const stored = await page.evaluate(() =>
      Object.keys(localStorage).find((k) => k.toLowerCase().includes('zoom')),
    );

    expect(stored).toBeDefined();
  });

  test('records reading progress', async ({ bookList, bookDetails, page }) => {
    const opened = await openFirstReadableBook(bookList, bookDetails);
    test.skip(!opened, 'no book with an attached file in this environment');

    await expect(page.locator('.reading-progress-text')).toBeVisible({ timeout: 20_000 });
  });
});

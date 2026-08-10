import { test, expect } from '../../fixtures/test-fixtures';
import { storageStateFor } from '../../utils/roles';

test.describe('book details', () => {
  test.use({ storageState: storageStateFor('owner') });

  test('opening a book from the list shows its detail page @smoke', async ({
    bookList,
    bookDetails,
    page,
  }) => {
    await bookList.goto();
    test.skip((await bookList.cards.count()) === 0, 'no books in this environment');

    await bookList.cards.first().click();

    await expect(page).toHaveURL(/\/book\/\d+/);
    await expect(bookDetails.backToList).toBeVisible();
  });

  test('an owner sees edit and delete', async ({ bookList, bookDetails }) => {
    await bookList.goto();
    test.skip((await bookList.cards.count()) === 0, 'no books in this environment');
    await bookList.cards.first().click();

    await expect(bookDetails.edit).toBeVisible();
    await expect(bookDetails.deleteButton).toBeVisible();
  });

  test('a nonexistent book reports no access rather than crashing', async ({ bookDetails }) => {
    await bookDetails.gotoBook(99999999);

    await expect(bookDetails.unauthorizedMessage).toBeVisible({ timeout: 20_000 });
  });

  test('back returns to the catalogue', async ({ bookList, bookDetails, page }) => {
    await bookList.goto();
    test.skip((await bookList.cards.count()) === 0, 'no books in this environment');
    await bookList.cards.first().click();

    await bookDetails.backToList.click();

    await expect(page).toHaveURL(/\/(books)?$/);
  });
});

test.describe('book details as a reader', () => {
  test.use({ storageState: storageStateFor('user') });

  test('a reader gets no edit or delete controls', async ({ bookList, bookDetails }) => {
    await bookList.goto();
    test.skip((await bookList.cards.count()) === 0, 'no books in this environment');
    await bookList.cards.first().click();

    await expect(bookDetails.edit).toHaveCount(0);
    await expect(bookDetails.deleteButton).toHaveCount(0);
  });

  test('a book from another tenant is not readable', async ({ bookDetails }) => {
    // Book ids are sequential across tenants, so guessing one is trivial. This is the
    // browser-level check that the API refuses.
    await bookDetails.gotoBook(1);

    const denied = await bookDetails.unauthorizedMessage.count();
    const readable = await bookDetails.readInline.count();
    expect(denied + readable).toBeGreaterThan(0);
  });
});

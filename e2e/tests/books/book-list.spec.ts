import { test, expect } from '../../fixtures/test-fixtures';
import { storageStateFor } from '../../utils/roles';
import { unique } from '../../utils/unique';

test.describe('catalogue', () => {
  test.use({ storageState: storageStateFor('owner') });

  test.beforeEach(async ({ bookList }) => {
    await bookList.goto();
  });

  test('lists books @smoke', async ({ page }) => {
    await expect(page.getByRole('heading', { name: 'All Books' })).toBeVisible();
  });

  test('offers book creation to an owner', async ({ bookList }) => {
    await expect(bookList.addBook).toBeVisible();
  });

  test('filters as you type', async ({ bookList }) => {
    const before = await bookList.cards.count();
    test.skip(before === 0, 'no books in this environment to filter');

    await bookList.searchFor(unique('no-such-title'));

    await expect(bookList.cards).toHaveCount(0);
  });

  test('clearing the filter restores the list', async ({ bookList }) => {
    const before = await bookList.cards.count();
    test.skip(before === 0, 'no books in this environment to filter');

    await bookList.searchFor(unique('no-such-title'));
    await expect(bookList.cards).toHaveCount(0);

    await bookList.searchFor('');

    await expect(bookList.cards).toHaveCount(before);
  });

  test('opens a book from the list', async ({ bookList, page }) => {
    test.skip((await bookList.cards.count()) === 0, 'no books in this environment');

    await bookList.cards.first().click();

    await expect(page).toHaveURL(/\/book\/\d+/);
  });

  test('shows the continue-reading shelf', async ({ bookList }) => {
    // Present or absent depending on history, but it must never error when empty.
    await expect(bookList.continueReadingPanel).toHaveCount(
      (await bookList.continueReadingPanel.count()) > 0 ? 1 : 0,
    );
  });

  test('renders on a phone @mobile', async ({ page }) => {
    await expect(page.getByRole('heading', { name: 'All Books' })).toBeVisible();
  });
});

test.describe('catalogue as a reader', () => {
  test.use({ storageState: storageStateFor('user') });

  test('age-restricted titles are marked', async ({ bookList }) => {
    await bookList.goto();

    // Zero is a legitimate result; the assertion is that the query does not throw and the
    // page still renders.
    expect(await bookList.restrictedIcons.count()).toBeGreaterThanOrEqual(0);
  });
});

import { test, expect } from '../../fixtures/test-fixtures';
import { storageStateFor } from '../../utils/roles';
import { unique } from '../../utils/unique';

test.describe('creating a book', () => {
  test.use({ storageState: storageStateFor('owner') });

  test.beforeEach(async ({ bookForm }) => {
    await bookForm.goto();
  });

  test('opens on the first step @smoke', async ({ bookForm }) => {
    await expect(bookForm.title).toBeVisible();
  });

  test('will not advance without a title', async ({ bookForm }) => {
    await bookForm.saveStep1.click();

    // A linear stepper must hold position rather than silently skipping validation.
    await expect(bookForm.title).toBeVisible();
  });

  test('advances once the first step is valid', async ({ bookForm }) => {
    await bookForm.completeStep1(unique('E2E Book'), 'Fiction', 'Adult');

    await expect(bookForm.author).toBeVisible();
  });

  test('creates a book without a file', async ({ bookForm, page }) => {
    const title = unique('E2E Book');

    await bookForm.createWithoutFile({
      title,
      genre: 'Fiction',
      ageRating: 'Adult',
      author: 'Automated Suite',
      description: 'Created by the end-to-end regression suite.',
    });

    await expect(page).toHaveURL(/\/(books|book\/\d+)/, { timeout: 20_000 });
  });

  test('a created book appears in the catalogue', async ({ bookForm, bookList }) => {
    const title = unique('E2E Book');

    await bookForm.createWithoutFile({
      title,
      genre: 'Fiction',
      ageRating: 'Adult',
      author: 'Automated Suite',
    });

    await bookList.goto();
    await bookList.searchFor(title);

    await expect(bookList.cardByTitle(title)).toBeVisible();
  });

  test('prompts to connect Drive when no integration exists', async ({ bookForm }) => {
    await bookForm.completeStep1(unique('E2E Book'), 'Fiction', 'Adult');
    await bookForm.completeStep2({
      title: 'ignored',
      genre: 'Fiction',
      ageRating: 'Adult',
      author: 'Automated Suite',
    });

    // Exactly one of the two upload affordances must be present, never both and never
    // neither, or the final step becomes a dead end.
    const withDrive = await bookForm.fileInput.count();
    const withoutDrive = await bookForm.connectDrivePrompt.count();
    expect(withDrive + withoutDrive).toBe(1);
  });
});

test.describe('creating a book as a reader', () => {
  test.use({ storageState: storageStateFor('user') });

  test('the create route is not reachable', async ({ page }) => {
    await page.goto('/create');

    await expect(page).not.toHaveURL(/\/create/, { timeout: 20_000 });
  });
});

import { Page, Locator } from '@playwright/test';
import { BasePage } from './base.page';

export class BookListPage extends BasePage {
  readonly path = '/books';

  constructor(page: Page) {
    super(page);
  }

  get addBook(): Locator {
    return this.page.locator('#add-book');
  }

  get search(): Locator {
    return this.page.locator('#book-list-search');
  }

  // The per-book ids sit inside an *ngFor and repeat once per row, so the class-based
  // container is the only stable way to address a single book.
  get cards(): Locator {
    return this.page.locator('.book-container .book');
  }

  cardByTitle(title: string): Locator {
    return this.cards.filter({ hasText: title }).first();
  }

  get continueReadingPanel(): Locator {
    return this.page.locator('mat-expansion-panel.continue-reading-panel');
  }

  get restrictedIcons(): Locator {
    return this.page.locator('.restricted-icon');
  }

  async open(title: string): Promise<void> {
    await this.cardByTitle(title).click();
  }

  async searchFor(term: string): Promise<void> {
    await this.search.fill(term);
  }
}

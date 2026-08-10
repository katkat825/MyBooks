import { Page, Locator } from '@playwright/test';
import { BasePage } from './base.page';

export class BookDetailsPage extends BasePage {
  readonly path = '/book';

  constructor(page: Page) {
    super(page);
  }

  async gotoBook(id: number | string): Promise<void> {
    await this.page.goto(`/book/${id}`);
    await this.waitUntilReady();
  }

  get backToList(): Locator {
    return this.page.locator('#book-list');
  }

  get download(): Locator {
    return this.page.locator('#download-file');
  }

  get readInline(): Locator {
    return this.page.locator('#read-inline');
  }

  get edit(): Locator {
    return this.page.locator('#edit-book');
  }

  get deleteButton(): Locator {
    return this.page.locator('#danger-button');
  }

  get unauthorizedMessage(): Locator {
    return this.page.locator('div.unauthorized-message[role="alert"]');
  }
}

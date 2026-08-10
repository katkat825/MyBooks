import { Page, Locator, expect } from '@playwright/test';
import { BasePage } from './base.page';

export interface BookDraft {
  title: string;
  genre: string;
  ageRating: string;
  author: string;
  isbn?: string;
  description?: string;
}

/**
 * A three-step linear mat-stepper. Each step must validate before the next becomes
 * reachable, so the helpers below advance one step at a time rather than filling blind.
 */
export class BookFormPage extends BasePage {
  readonly path = '/create';

  constructor(page: Page) {
    super(page);
  }

  get title(): Locator {
    return this.page.locator('#book-form-title');
  }

  get author(): Locator {
    return this.page.locator('#book-form-author');
  }

  get isbn(): Locator {
    return this.page.locator('#isbn');
  }

  get description(): Locator {
    return this.page.locator('#book-form-description');
  }

  get saveStep1(): Locator {
    return this.page.locator('#save-step1');
  }

  get saveStep2(): Locator {
    return this.page.locator('#save-step2');
  }

  get fileInput(): Locator {
    return this.page.locator('#book-form-file');
  }

  get uploadFile(): Locator {
    return this.page.locator('#upload-file');
  }

  get skipFile(): Locator {
    return this.page.locator('#skip-file');
  }

  get skipFileNoIntegration(): Locator {
    return this.page.locator('#skip-file-no-integration');
  }

  get connectDrivePrompt(): Locator {
    return this.page.locator('#connect-drive');
  }

  async completeStep1(title: string, genre: string, ageRating: string): Promise<void> {
    await this.title.fill(title);
    await this.chooseMatOption(this.matSelect('genreId'), genre);
    await this.chooseMatOption(this.matSelect('ageCategoryId'), ageRating);
    await expect(this.saveStep1).toBeEnabled();
    await this.saveStep1.click();
  }

  async completeStep2(draft: BookDraft): Promise<void> {
    await this.author.fill(draft.author);
    if (draft.isbn) await this.isbn.fill(draft.isbn);
    if (draft.description) await this.description.fill(draft.description);
    await this.saveStep2.click();
  }

  /** Creates a book and skips the file step, which is the only path that needs no Drive. */
  async createWithoutFile(draft: BookDraft): Promise<void> {
    await this.completeStep1(draft.title, draft.genre, draft.ageRating);
    await this.completeStep2(draft);

    const skip = (await this.skipFile.count())
      ? this.skipFile
      : this.skipFileNoIntegration;
    await skip.click();
  }
}

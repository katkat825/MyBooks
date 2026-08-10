import { Page, Locator } from '@playwright/test';
import { BasePage } from './base.page';

export class BulkImportPage extends BasePage {
  readonly path = '/account/bulk-import';

  constructor(page: Page) {
    super(page);
  }

  get viewJobs(): Locator {
    return this.page.locator('#view-jobs');
  }

  get selectFolder(): Locator {
    return this.page.locator('#select-folder');
  }

  get startImport(): Locator {
    return this.page.locator('#start-bulk-import');
  }

  get globalGenreSelect(): Locator {
    return this.page.locator('#global-genre-select');
  }

  get emptyState(): Locator {
    return this.page.getByText('No EPUB or PDF files selected for import.');
  }

  get jobsDialog(): Locator {
    return this.page.locator('mat-dialog-container');
  }
}

import { Page, Locator } from '@playwright/test';
import { BasePage } from './base.page';

export class IntegrationsPage extends BasePage {
  readonly path = '/account/integrations';

  constructor(page: Page) {
    super(page);
  }

  /** The connect button carries no id, so it is addressed by its label. */
  get connect(): Locator {
    return this.page.getByRole('button', { name: /Connect Google Drive/i });
  }

  get removeButtons(): Locator {
    return this.page.locator('#remove-integration');
  }

  get table(): Locator {
    return this.page.locator('table');
  }

  rowFor(accountEmail: string): Locator {
    return this.page.locator('tr').filter({ hasText: accountEmail });
  }
}

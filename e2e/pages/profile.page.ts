import { Page, Locator } from '@playwright/test';
import { BasePage } from './base.page';

export class ProfilePage extends BasePage {
  readonly path = '/profile';

  constructor(page: Page) {
    super(page);
  }

  get firstName(): Locator {
    return this.page.locator('#first-name');
  }

  get lastName(): Locator {
    return this.page.locator('#last-name');
  }

  get email(): Locator {
    return this.page.locator('#email-username');
  }

  get password(): Locator {
    return this.page.locator('#password');
  }

  get save(): Locator {
    return this.page.locator('#save-changes');
  }

  get error(): Locator {
    return this.page.locator('div.error-message[role="alert"]');
  }
}

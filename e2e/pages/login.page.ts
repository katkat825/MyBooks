import { Page, Locator, expect } from '@playwright/test';
import { BasePage } from './base.page';

export class LoginPage extends BasePage {
  readonly path = '/login';

  constructor(page: Page) {
    super(page);
  }

  // #login-btn also exists on the home page and inside the toolbar menu, so every
  // locator here is scoped to the login card.
  private readonly root = () => this.page.locator('.login-container');

  get email(): Locator {
    return this.root().locator('#email');
  }

  get password(): Locator {
    return this.root().locator('#password');
  }

  get submit(): Locator {
    return this.root().locator('#login-btn');
  }

  get error(): Locator {
    return this.page.locator('p.error-message[role="alert"]');
  }

  get forgotPasswordLink(): Locator {
    return this.page.locator('#forgot-password-link');
  }

  async login(email: string, password: string): Promise<void> {
    await this.email.fill(email);
    await this.password.fill(password);
    await this.submit.click();
  }

  async expectSignedIn(): Promise<void> {
    await expect(this.page).toHaveURL(/\/books/);
  }

  async expectRejected(): Promise<void> {
    await expect(this.error).toBeVisible();
    await expect(this.page).toHaveURL(/\/login/);
  }

  async token(): Promise<string | null> {
    return this.page.evaluate(() => localStorage.getItem('token'));
  }
}

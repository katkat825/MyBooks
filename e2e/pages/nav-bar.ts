import { Page, Locator, expect } from '@playwright/test';

/**
 * The toolbar menus are mat-menu overlays: nothing inside them exists in the DOM until the
 * trigger is clicked, and several of their ids collide with ids on the page underneath.
 * Every locator here is scoped to the open panel for that reason.
 */
export class NavBar {
  constructor(private readonly page: Page) {}

  private get panel(): Locator {
    return this.page.locator('.mat-mdc-menu-panel');
  }

  async openAccountMenu(): Promise<void> {
    await this.page.locator('#account-menu').click();
    await expect(this.panel.first()).toBeVisible();
  }

  async openOwnerMenu(): Promise<void> {
    await this.openAccountMenu();
    await this.panel.locator('#owner-menu').click();
    await expect(this.panel.last()).toBeVisible();
  }

  async logout(): Promise<void> {
    await this.openAccountMenu();
    await this.panel.locator('#logout-btn').click();
  }

  async gotoProfile(): Promise<void> {
    await this.openAccountMenu();
    await this.panel.locator('#my-profile').click();
  }

  async gotoManageUsers(): Promise<void> {
    await this.openOwnerMenu();
    await this.panel.locator('#manage-users').click();
  }

  async gotoIntegrations(): Promise<void> {
    await this.openOwnerMenu();
    await this.panel.locator('#manage-integrations').click();
  }

  async gotoBulkImport(): Promise<void> {
    await this.openOwnerMenu();
    await this.panel.locator('#bulk-import').click();
  }

  async setTheme(theme: 'light' | 'dark' | 'contrast'): Promise<void> {
    await this.openAccountMenu();
    await this.panel.locator('#theme-menu').click();
    await this.panel.locator(`#${theme}-theme`).click();
  }

  ownerMenuTrigger(): Locator {
    return this.panel.locator('#owner-menu');
  }

  supportLink(): Locator {
    return this.panel.locator('#support-user');
  }

  globalReviewerLink(): Locator {
    return this.panel.locator('#global-reviewer');
  }

  impersonationBanner(): Locator {
    return this.page.locator('footer.impersonation-banner');
  }
}

import { Page, Locator, expect } from '@playwright/test';

export abstract class BasePage {
  protected constructor(protected readonly page: Page) {}

  abstract readonly path: string;

  async goto(): Promise<void> {
    await this.page.goto(this.path);
    await this.waitUntilReady();
  }

  /**
   * The app shows a full-screen overlay during any global load. Acting while it is up
   * produces intercepted-click failures that read like flake but are not.
   */
  async waitUntilReady(): Promise<void> {
    const overlay = this.page.locator('#loading-message');
    if (await overlay.count()) {
      await expect(overlay).toBeHidden({ timeout: 30_000 });
    }
  }

  /**
   * Angular Material renders select options into a detached overlay, so the option is not
   * a descendant of the trigger and cannot be located through it.
   */
  protected async chooseMatOption(trigger: Locator, optionText: string): Promise<void> {
    await trigger.click();
    const panel = this.page.locator('.mat-mdc-select-panel');
    await expect(panel).toBeVisible();
    await panel.getByRole('option', { name: optionText, exact: false }).first().click();
    await expect(panel).toBeHidden();
  }

  protected matSelect(formControlName: string): Locator {
    // Angular lowercases binding attributes in the rendered DOM.
    return this.page.locator(`mat-select[formcontrolname="${formControlName}"]`);
  }
}

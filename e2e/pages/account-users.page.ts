import { Page, Locator, expect } from '@playwright/test';
import { BasePage } from './base.page';

export class AccountUsersPage extends BasePage {
  readonly path = '/account/users';

  constructor(page: Page) {
    super(page);
  }

  get toggleInactive(): Locator {
    return this.page.locator('#toggle-inactive-users');
  }

  get toggleAddUser(): Locator {
    return this.page.locator('#toggle-add-user');
  }

  private readonly addCard = () => this.page.locator('mat-card.add-user-card');

  get firstName(): Locator {
    return this.addCard().locator('#first-name');
  }

  get lastName(): Locator {
    return this.addCard().locator('#last-name');
  }

  get email(): Locator {
    return this.addCard().locator('#email');
  }

  get saveNewUser(): Locator {
    return this.page.locator('#save-new-user');
  }

  get seatCounter(): Locator {
    return this.page.locator('div.user-limits');
  }

  rowFor(email: string): Locator {
    return this.page.locator('tr').filter({ hasText: email });
  }

  async addUser(firstName: string, lastName: string, email: string, ageRating: string): Promise<void> {
    await this.toggleAddUser.click();
    await expect(this.addCard()).toBeVisible();
    await this.firstName.fill(firstName);
    await this.lastName.fill(lastName);
    await this.email.fill(email);
    await this.chooseMatOption(this.matSelect('ageCategoryId'), ageRating);
    await this.saveNewUser.click();
  }
}

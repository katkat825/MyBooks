import { test as base, expect, Page } from '@playwright/test';
import { Role, storageStateFor } from '../utils/roles';
import { LoginPage } from '../pages/login.page';
import { BookListPage } from '../pages/book-list.page';
import { BookFormPage } from '../pages/book-form.page';
import { BookDetailsPage } from '../pages/book-details.page';
import { ProfilePage } from '../pages/profile.page';
import { AccountUsersPage } from '../pages/account-users.page';
import { IntegrationsPage } from '../pages/integrations.page';
import { BulkImportPage } from '../pages/bulk-import.page';
import { AdminPage } from '../pages/admin.page';
import { NavBar } from '../pages/nav-bar';

type Pages = {
  loginPage: LoginPage;
  bookList: BookListPage;
  bookForm: BookFormPage;
  bookDetails: BookDetailsPage;
  profile: ProfilePage;
  accountUsers: AccountUsersPage;
  integrations: IntegrationsPage;
  bulkImport: BulkImportPage;
  admin: AdminPage;
  nav: NavBar;
};

type Roles = {
  /** A page already signed in as the given role, for cross-role assertions in one test. */
  pageAs: (role: Role) => Promise<Page>;
};

export const test = base.extend<Pages & Roles>({
  loginPage: async ({ page }, use) => use(new LoginPage(page)),
  bookList: async ({ page }, use) => use(new BookListPage(page)),
  bookForm: async ({ page }, use) => use(new BookFormPage(page)),
  bookDetails: async ({ page }, use) => use(new BookDetailsPage(page)),
  profile: async ({ page }, use) => use(new ProfilePage(page)),
  accountUsers: async ({ page }, use) => use(new AccountUsersPage(page)),
  integrations: async ({ page }, use) => use(new IntegrationsPage(page)),
  bulkImport: async ({ page }, use) => use(new BulkImportPage(page)),
  admin: async ({ page }, use) => use(new AdminPage(page)),
  nav: async ({ page }, use) => use(new NavBar(page)),

  pageAs: async ({ browser }, use) => {
    const contexts: import('@playwright/test').BrowserContext[] = [];

    await use(async (role: Role) => {
      const context = await browser.newContext({ storageState: storageStateFor(role) });
      contexts.push(context);
      return context.newPage();
    });

    await Promise.all(contexts.map((c) => c.close()));
  },
});

export { expect };

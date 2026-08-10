import { Page, Locator } from '@playwright/test';
import { BasePage } from './base.page';

/**
 * Both tab bodies are rendered at once, so every id on this page exists twice. Each
 * locator is scoped to its component to avoid strict-mode violations.
 */
export class AdminPage extends BasePage {
  readonly path = '/account/genres-series';

  constructor(page: Page) {
    super(page);
  }

  private readonly genres = () => this.page.locator('app-admin-genres');
  private readonly series = () => this.page.locator('app-admin-series');

  get addGenre(): Locator {
    return this.genres().locator('#add-genre');
  }

  get genreNameInput(): Locator {
    return this.genres().locator('.create-form input[formcontrolname="name"]');
  }

  get saveGenre(): Locator {
    return this.genres().locator('.create-form #save-button');
  }

  get addSeries(): Locator {
    return this.series().getByRole('button', { name: /^Add Series$/ });
  }

  get seriesNameInput(): Locator {
    return this.series().locator('.create-form input[formcontrolname="name"]');
  }

  get saveSeries(): Locator {
    return this.series().locator('.create-form #save-button');
  }

  genreRow(name: string): Locator {
    return this.genres().locator('tr').filter({ hasText: name });
  }

  seriesRow(name: string): Locator {
    return this.series().locator('tr').filter({ hasText: name });
  }
}

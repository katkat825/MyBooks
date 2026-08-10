import { defineConfig, devices } from '@playwright/test';
import * as path from 'path';
import * as dotenv from 'dotenv';

dotenv.config({ path: path.resolve(__dirname, '.env') });

export const STORAGE_STATE_DIR = path.resolve(__dirname, '.auth');

/**
 * The Angular dev server listens on 8080 (angular.json -> serve.options.port) and proxies
 * /api/* to the individual services via proxy.conf.json. Point BASE_URL somewhere else to
 * run the same suite against a deployed environment.
 */
const baseURL = process.env.BASE_URL ?? 'http://localhost:8080';

export default defineConfig({
  testDir: './tests',
  outputDir: './test-results',
  fullyParallel: true,

  // A committed .only is almost always an accident that would silently shrink CI coverage.
  forbidOnly: !!process.env.CI,

  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 4 : undefined,
  timeout: 45_000,
  expect: { timeout: 10_000 },

  reporter: [
    ['list'],
    ['html', { outputFolder: 'playwright-report', open: 'never' }],
    ['junit', { outputFile: 'test-results/junit.xml' }],
  ],

  use: {
    baseURL,
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
    actionTimeout: 15_000,
    navigationTimeout: 30_000,
    testIdAttribute: 'data-testid',
  },

  projects: [
    {
      // Signs in once per role and writes the storage states the other projects reuse.
      name: 'setup',
      testMatch: /auth\.setup\.ts/,
    },
    {
      name: 'authenticated',
      use: { ...devices['Desktop Chrome'] },
      dependencies: ['setup'],
      // Specs declare their own storageState per role; the project only supplies the browser.
      testIgnore: [/public\//, /auth\.setup\.ts/],
    },
    {
      // Anything reachable without a session runs with no stored credentials at all, so a
      // leaked cookie cannot make a public-access test pass by accident.
      name: 'public',
      use: { ...devices['Desktop Chrome'], storageState: { cookies: [], origins: [] } },
      testMatch: /public\//,
    },
    {
      name: 'mobile',
      use: { ...devices['Pixel 7'] },
      dependencies: ['setup'],
      grep: /@mobile/,
      testIgnore: [/auth\.setup\.ts/],
    },
  ],

  webServer: process.env.CI
    ? undefined
    : {
        command: 'npm start --prefix ../MyBooks.UI',
        url: baseURL,
        reuseExistingServer: true,
        timeout: 180_000,
      },
});

import * as path from 'path';
import { STORAGE_STATE_DIR } from '../playwright.config';

/**
 * The application defines seven roles, but only four are reachable through the UI with
 * distinct navigation. Admin and Editor differ from Owner only in permissions that the
 * API enforces, so they are covered by unit tests rather than by a browser.
 */
export type Role = 'owner' | 'user' | 'superadmin' | 'reviewer';

export interface Credentials {
  email: string;
  password: string;
}

export const CREDENTIALS: Record<Role, Credentials> = {
  owner: {
    email: process.env.OWNER_EMAIL ?? 'owner@example.com',
    password: process.env.OWNER_PASSWORD ?? '',
  },
  user: {
    email: process.env.USER_EMAIL ?? 'reader@example.com',
    password: process.env.USER_PASSWORD ?? '',
  },
  superadmin: {
    email: process.env.SUPERADMIN_EMAIL ?? 'superadmin@example.com',
    password: process.env.SUPERADMIN_PASSWORD ?? '',
  },
  reviewer: {
    email: process.env.REVIEWER_EMAIL ?? 'reviewer@example.com',
    password: process.env.REVIEWER_PASSWORD ?? '',
  },
};

export const storageStateFor = (role: Role): string =>
  path.join(STORAGE_STATE_DIR, `${role}.json`);

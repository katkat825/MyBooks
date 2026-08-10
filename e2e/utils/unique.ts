/**
 * Tests run in parallel against a shared database, so anything a test creates needs a
 * name no other worker will collide with.
 */
export const unique = (prefix: string): string =>
  `${prefix}-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 8)}`;

export const uniqueEmail = (prefix = 'e2e'): string =>
  `${unique(prefix)}@example.invalid`;

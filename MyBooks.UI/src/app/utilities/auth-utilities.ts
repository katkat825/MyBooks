/**
 * auth-utilities.ts
 *
 * Utility functions for handling authentication tokens.
 */

/**
 * Checks if a JWT token is expired.
 * @param token - The JWT token as a string.
 * @returns true if the token is expired or invalid, false otherwise.
 */
export function isTokenExpired(token: string): boolean {
  if (!token) {
    return true;
  }

  try {
    const tokenParts = token.split('.');
    if (tokenParts.length !== 3) {
      console.error('Invalid token format.');
      return true;
    }

    let base64Url = tokenParts[1];
    let base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');

    const padLength = (4 - (base64.length % 4)) % 4;
    base64 += '='.repeat(padLength);

    const jsonPayload = atob(base64);
    const payload = JSON.parse(jsonPayload);

    if (!payload.exp) {
      return false;
    }

    const now = Math.floor(Date.now() / 1000);
    return now > payload.exp;
  } catch (error) {
    console.error('Error decoding token:', error);
    return true;
  }
}

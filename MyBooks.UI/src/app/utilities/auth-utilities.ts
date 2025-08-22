/**
 * Robust JWT expiry check with ms/s handling and small clock skew.
 */
export function isTokenExpired(token: string): boolean {
  if (!token) return true;

  try {
    const parts = token.split('.');
    if (parts.length !== 3) return true;

    // base64url decode
    let base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
    base64 += '='.repeat((4 - (base64.length % 4)) % 4);
    const payload = JSON.parse(atob(base64));

    // No exp -> treat as non‑expiring
    if (payload?.exp == null) return false;

    // Normalize exp to seconds
    let exp = typeof payload.exp === 'string' ? parseInt(payload.exp, 10) : payload.exp;
    if (Number.isNaN(exp)) return true;
    if (exp > 1e12) exp = Math.floor(exp / 1000); // exp was in ms

    const now = Math.floor(Date.now() / 1000);
    const leeway = 30; // seconds of clock skew tolerance

    // Optional: honor nbf (not-before) if present
    let nbf = payload.nbf;
    if (typeof nbf === 'string') nbf = parseInt(nbf, 10);
    if (typeof nbf === 'number' && nbf > 1e12) nbf = Math.floor(nbf / 1000);

    if (typeof nbf === 'number' && now + leeway < nbf) {
      // Token not valid yet; treat like expired
      return true;
    }

    return now >= (exp - leeway);
  } catch (e) {
    console.error('Error decoding token:', e);
    return true;
  }
}

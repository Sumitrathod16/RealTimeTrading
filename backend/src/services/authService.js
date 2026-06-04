import { config } from '../config.js';

let cachedToken = null;
let tokenFetchedAt = null;
const TOKEN_TTL_MS = 50 * 60 * 1000;

export function getToken() {
  return cachedToken;
}

export function getAuthStatus() {
  if (!config.userId || !config.password) {
    return { ok: false, message: 'Credentials not configured (.env)' };
  }
  if (!cachedToken) {
    return { ok: false, message: 'Not authenticated' };
  }
  return { ok: true, message: 'Authenticated', fetchedAt: tokenFetchedAt };
}

export async function authenticate(force = false) {
  if (!force && cachedToken && tokenFetchedAt && Date.now() - tokenFetchedAt < TOKEN_TTL_MS) {
    return cachedToken;
  }

  if (!config.userId || !config.accountId || !config.password) {
    throw new Error('Missing USER_ID, ACCOUNT_ID, or PASSWORD in environment');
  }

  const payloads = [
    { userId: config.userId, accountId: config.accountId, password: config.password },
    { UserId: config.userId, AccountId: config.accountId, Password: config.password },
    { username: config.userId, account: config.accountId, password: config.password },
  ];

  let lastError;
  for (const body of payloads) {
    try {
      const res = await fetch(config.authUrl, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
        body: JSON.stringify(body),
      });
      const text = await res.text();
      if (!res.ok) {
        lastError = new Error(`Auth HTTP ${res.status}: ${text.slice(0, 200)}`);
        continue;
      }
      const token = extractToken(text);
      if (token) {
        cachedToken = token;
        tokenFetchedAt = Date.now();
        console.log('[Auth] Token obtained successfully');
        return token;
      }
      lastError = new Error('No token in auth response');
    } catch (err) {
      lastError = err;
    }
  }
  throw lastError || new Error('Authentication failed');
}

function extractToken(text) {
  try {
    const data = JSON.parse(text);
    const candidates = [
      data.token,
      data.Token,
      data.accessToken,
      data.access_token,
      data.AccessToken,
      data.data?.token,
      data.result?.token,
    ];
    for (const t of candidates) {
      if (typeof t === 'string' && t.length > 0) return t;
    }
  } catch {
    if (text && !text.startsWith('{') && text.length < 500) return text.trim();
  }
  return null;
}

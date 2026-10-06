import type { AuthResponse } from "./authApi";

const AUTH_SESSION_KEY = "finhub.auth.session";

/**
 * @brief Represents the authentication session stored in the browser's sessionStorage.
 */
export interface AuthSession {
  userId: string;
  accessToken: string | null;
  refreshToken: string | null;
  expiresAt: number | null;
  requiresEmailConfirmation: boolean;
}

/**
 * @brief Converts an AuthResponse object to an AuthSession object.
 * @param response The AuthResponse object to convert.
 * @returns An AuthSession object containing the relevant authentication information.
 */
function toAuthSession(response: AuthResponse): AuthSession {
  return {
    userId: response.userId,
    accessToken: response.accessToken,
    refreshToken: response.refreshToken,
    expiresAt:
      response.expiresIn === null
        ? null
        : Date.now() + response.expiresIn * 1000,
    requiresEmailConfirmation: response.requiresEmailConfirmation,
  };
}

/**
 * Saves the authentication session to the browser's sessionStorage.
 * @param response The AuthResponse object containing the authentication information to save.
 * @returns An AuthSession object representing the saved authentication session.
 */
export function saveAuthSession(response: AuthResponse): AuthSession {
  const session = toAuthSession(response);
  sessionStorage.setItem(AUTH_SESSION_KEY, JSON.stringify(session));
  return session;
}

/**
 * Retrieves the authentication session from the browser's sessionStorage.
 * @returns The AuthSession object if it exists and is valid, or null if it does not exist or is invalid.
 */
export function getAuthSession(): AuthSession | null {
  const rawSession = sessionStorage.getItem(AUTH_SESSION_KEY);
  if (!rawSession) {
    return null;
  }

  try {
    const session = JSON.parse(rawSession) as AuthSession;
    if (
      !session.userId ||
      (session.expiresAt !== null && session.expiresAt <= Date.now())
    ) {
      clearAuthSession();
      return null;
    }
    return session;
  } catch {
    clearAuthSession();
    return null;
  }
}

/**
 * @brief Clears the authentication session from the browser's sessionStorage.
 */
export function clearAuthSession(): void {
  sessionStorage.removeItem(AUTH_SESSION_KEY);
}

/**
 * @brief Retrieves the access token from the authentication session.
 */
export function getAccessToken(): string | null {
  return getAuthSession()?.accessToken ?? null;
}

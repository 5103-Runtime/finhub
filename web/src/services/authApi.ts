import { apiGet, apiPost } from "./apiClient";
export { ApiError } from "./apiClient";

/**
 * @brief Represents the data required to register a new user.
 */
export interface RegisterRequest {
  email: string;
  password: string;
}

/**
 * @brief Represents the data required to log in an existing user.
 */
export interface LoginRequest {
  email: string;
  password: string;
}

/**
 * @brief Represents the response returned after a successful user registration.
 */
export interface AuthResponse {
  userId: string;
  requiresEmailConfirmation: boolean;
  requiresProfileSetup: boolean;
}

/**
 * @brief Represents the current authenticated user's session information.
 */
export interface AuthSession {
  authenticated: boolean;
  userId: string | null;
  requiresProfileSetup: boolean;
}

/**
 * @brief Registers a new user through the authentication API.
 * @param request User registration information containing an email and password.
 * @returns A promise containing the newly registered user's information.
 * @throws ApiError If the registration request fails.
 */
export function register(request: RegisterRequest) {
  return apiPost<RegisterRequest, AuthResponse>("/api/auth/register", request);
}

/**
 * Logs in a user through the authentication API.
 * @param request User login information containing an email and password.
 * @returns A promise containing the logged-in user's information.
 * @throws ApiError If the login request fails.
 */
export function login(request: LoginRequest) {
  return apiPost<LoginRequest, AuthResponse>("/api/auth/login", request);
}

/**
 * Retrieves the current authenticated user's session information from the authentication API.
 * @returns The current authenticated user's session information.
 */
export async function getSession() {
  const session = await apiGet<AuthSession>("/api/auth/session");
  if (session.authenticated) {
    return session;
  }

  try {
    await refresh();
    return await apiGet<AuthSession>("/api/auth/session");
  } catch {
    return session;
  }
}

/**
 * Refreshes the access token using the HttpOnly refresh-token cookie.
 * @returns A promise containing the refreshed user's information.
 */
export function refresh() {
  return apiPost<undefined, void>("/api/auth/refresh", undefined);
}

/**
 * Logs out the current authenticated user through the authentication API.
 * @returns A promise that resolves when the user is successfully logged out.
 */
export function logout() {
  return apiPost<undefined, void>("/api/auth/logout", undefined);
}

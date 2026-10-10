import { apiGet, apiPatch, apiPost } from "./apiClient";
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
 * @brief Represents the data required to update the authenticated user's password.
 */
export interface UpdatePasswordRequest {
  currentPassword: string;
  password: string;
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
 * @returns A promise containing the logged-in user's information and session flags.
 * @throws ApiError If the login request fails.
 */
export function login(request: LoginRequest) {
  return apiPost<LoginRequest, AuthResponse>("/api/auth/login", request);
}

/**
 * Retrieves the current authenticated user's session information from the authentication API.
 * @returns A promise containing the current session information.
 * @throws ApiError If the session request fails.
 */
export function getSession() {
  return apiGet<AuthSession>("/api/auth/session");
}

/**
 * Logs out the current authenticated user through the authentication API.
 * @returns A promise that resolves when the logout request succeeds.
 * @throws ApiError If the logout request fails.
 */
export function logout() {
  return apiPost<undefined, void>("/api/auth/logout", undefined);
}

/**
 * Updates the authenticated user's password through the authentication API.
 * @param request The current and new password values.
 * @returns A promise containing the updated authentication response.
 * @throws ApiError If the current password is invalid or the update request fails.
 */
export function updatePassword(request: UpdatePasswordRequest) {
  return apiPatch<UpdatePasswordRequest, AuthResponse>(
    "/api/auth/password",
    request,
  );
}

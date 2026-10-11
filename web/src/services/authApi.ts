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
export function getSession() {
  return apiGet<AuthSession>("/api/auth/session");
}

/**
 * Logs out the current authenticated user through the authentication API.
 * @returns A promise that resolves when the user is successfully logged out.
 */
export function logout() {
  return apiPost<undefined, void>("/api/auth/logout", undefined);
}


/**
 * @brief Represents the data required to request a password reset email.
 */
export interface ForgotPasswordRequest {
  email: string;
}

/**
 * @brief Represents a simple confirmation message returned by the API.
 */
export interface MessageResponse {
  message: string;
}

/**
 * @brief Requests a password reset email through the authentication API.
 * @param request The email address to send the reset link to.
 * @returns A promise containing a confirmation message.
 * @throws ApiError If the request fails.
 */
export function requestPasswordReset(request: ForgotPasswordRequest) {
  return apiPost<ForgotPasswordRequest, MessageResponse>(
    "/api/auth/forgot-password",
    request,
  );
}

/**
 * @brief Represents the data required to set a new password using a recovery token.
 */
export interface ResetPasswordRequest {
  accessToken: string;
  newPassword: string;
}

/**
 * @brief Sets a new password using the recovery token from a password reset email.
 * @param request The recovery token and the new password.
 * @returns A promise containing a confirmation message.
 * @throws ApiError If the token is invalid or expired, or the request fails.
 */
export function resetPassword(request: ResetPasswordRequest) {
  return apiPost<ResetPasswordRequest, MessageResponse>(
    "/api/auth/reset-password",
    request,
  );
}

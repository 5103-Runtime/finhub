import { apiPost } from "./apiClient";
export { ApiError } from "./apiClient";

/**
 * @brief Represents the data required to register a new user.
 */
export interface RegisterRequest {
  email: string;
  password: string;
}

/**
 * @brief Represents the response returned after a successful user registration.
 */
export interface RegisterResponse {
  userId: string;
  accessToken: string | null;
  refreshToken: string | null;
  expiresIn: number | null;
  requiresEmailConfirmation: boolean;
}

/**
 * @brief Registers a new user through the authentication API.
 *
 * @param request User registration information containing an email and password.
 *
 * @returns A promise containing the newly registered user's information.
 *
 * @throws ApiError If the registration request fails.
 */
export function register(request: RegisterRequest) {
  return apiPost<RegisterRequest, RegisterResponse>(
    "/api/auth/register",
    request,
  );
}

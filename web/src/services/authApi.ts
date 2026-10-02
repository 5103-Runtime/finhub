
import { apiPost } from "./apiClient";
export { ApiError } from "./apiClient";

//request body for sign-up
export interface SignUpRequest {
  email: string;
  password: string;
}

//successful response
export interface SignUpResponse {
  userId: string;
  accessToken: string | null;
  refreshToken: string | null;
  expiresIn: number | null;
  requiresEmailConfirmation: boolean;
}

//POST, throws ApiError on failure
export function signUp(request: SignUpRequest) {
  return apiPost<SignUpRequest, SignUpResponse>("/api/auth/register", request);
}
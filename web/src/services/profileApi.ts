import { apiGet, apiPut } from "./apiClient";

/**
 * @brief Request body for updating the user's profile.
 */
export interface UpdateProfileRequest {
  firstName: string;
  lastName: string;
  preferredName?: string;
  dateOfBirth: string;
}

/**
 * @brief Response body returned when retrieving or updating the user's profile.
 */
export interface ProfileResponse extends UpdateProfileRequest {
  id: string;
}

/**
 * @brief Retrieves the authenticated user's profile.
 * @returns A promise containing the authenticated user's profile.
 * @throws ApiError If the request fails or the user is not authenticated.
 */
export function getProfile() {
  return apiGet<ProfileResponse>("/api/profile");
}

/**
 * @brief Updates the user's profile with the provided information.
 * @param request The request body containing the updated profile information.
 * @returns A promise containing the updated profile information.
 * @throws ApiError If the request fails or the user is not authenticated.
 */
export function updateProfile(request: UpdateProfileRequest) {
  return apiPut<UpdateProfileRequest, ProfileResponse>("/api/profile", request);
}

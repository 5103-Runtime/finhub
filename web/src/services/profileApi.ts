import { apiPut } from "./apiClient";

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
 * @brief Response body for updating the user's profile.
 */
export interface ProfileResponse extends UpdateProfileRequest {
  id: string;
}

/**
 * @brief Updates the user's profile with the provided information.
 * @param request The request body containing the updated profile information.
 * @returns The response body containing the updated profile information.
 */
export function updateProfile(request: UpdateProfileRequest) {
  return apiPut<UpdateProfileRequest, ProfileResponse>(
    "/api/profile",
    request,
  );
}

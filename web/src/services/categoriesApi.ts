import { apiGet } from "./apiClient";

/**
 * @brief Response returned for a category.
 */
export interface CategoryResponse {
  id: string;
  name: string;
  isSystem: boolean;
  createdAt: string;
  updatedAt: string;
}

/**
 * @brief Retrieves system categories and the authenticated user's custom categories.
 * @returns A promise containing the categories available to the user.
 * @throws ApiError If the request fails or the user is not authenticated.
 */
export function getCategories(): Promise<CategoryResponse[]> {
  return apiGet<CategoryResponse[]>("/api/categories");
}

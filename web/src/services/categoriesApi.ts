import { apiDelete, apiGet, apiPost, apiPut } from "./apiClient";

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

export type CategoryRequest = {
  name: string;
};

/**
 * @brief Retrieves system categories and the authenticated user's custom categories.
 * @returns A promise containing the categories available to the user.
 * @throws ApiError If the request fails or the user is not authenticated.
 */
export function getCategories(): Promise<CategoryResponse[]> {
  return apiGet<CategoryResponse[]>("/api/categories");
}

export function createCategory(request: CategoryRequest) {
  return apiPost<CategoryRequest, CategoryResponse>("/api/categories", request);
}

export function updateCategory(id: string, request: CategoryRequest) {
  return apiPut<CategoryRequest, CategoryResponse>(
    `/api/categories/${id}`,
    request,
  );
}

export function deleteCategory(id: string): Promise<void> {
  return apiDelete(`/api/categories/${id}`);
}

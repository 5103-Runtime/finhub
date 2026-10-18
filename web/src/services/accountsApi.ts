import { apiDelete, apiGet, apiPost, apiPut } from "./apiClient";

/**
 * Supported financial account types.
 */
export type AccountType = "chequing" | "savings" | "credit_card";

/**
 * Supported account currencies.
 */
export type AccountCurrency = "CAD" | "USD";

/**
 * Response returned for a financial account.
 */
export interface AccountResponse {
  id: string;
  name: string;
  institution: string | null;
  accountType: AccountType;
  currency: AccountCurrency;
  createdAt: string;
  updatedAt: string;
}

/**
 * Request body used to create a financial account.
 */
export interface CreateAccountRequest {
  name: string;
  institution?: string;
  accountType: AccountType;
  currency?: AccountCurrency;
}

/**
 * Request body used to update an account's editable fields.
 */
export interface UpdateAccountRequest {
  name: string;
  institution?: string;
}

/**
 * Retrieves all financial accounts belonging to the authenticated user.
 * @returns A promise containing the user's accounts ordered by creation time.
 * @throws ApiError If the request fails or the user is not authenticated.
 */
export function getAccounts(): Promise<AccountResponse[]> {
  return apiGet<AccountResponse[]>("/api/accounts");
}

/**
 * Creates a financial account for the authenticated user.
 * @param request The account details to create.
 * @returns A promise containing the created account.
 * @throws ApiError If validation fails, the request fails, or the user is not authenticated.
 */
export function createAccount(
  request: CreateAccountRequest,
): Promise<AccountResponse> {
  return apiPost<CreateAccountRequest, AccountResponse>(
    "/api/accounts",
    request,
  );
}

/**
 * Updates the editable fields of an authenticated user's account.
 * @param id The ID of the account to update.
 * @param request The account details to update.
 * @returns A promise containing the updated account.
 * @throws ApiError If validation fails, the account is not found, or the request fails.
 */
export function updateAccount(
  id: string,
  request: UpdateAccountRequest,
): Promise<AccountResponse> {
  return apiPut<UpdateAccountRequest, AccountResponse>(
    `/api/accounts/${id}`,
    request,
  );
}

/**
 * Deletes an authenticated user's account.
 * @param id The ID of the account to delete.
 * @returns A promise that resolves when the account has been deleted.
 * @throws ApiError If the account is not found or the request fails.
 */
export function deleteAccount(id: string): Promise<void> {
  return apiDelete(`/api/accounts/${id}`);
}

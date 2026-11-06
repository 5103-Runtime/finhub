import { apiDelete, apiGet, apiPost, apiPut } from "./apiClient";

/**
 * @brief Response returned for a ledger transaction.
 */
export interface TransactionResponse {
  id: string;
  accountId: string;
  categoryId: string | null;
  // Date only, formatted as YYYY-MM-DD
  transactionDate: string;
  description: string;
  // Signed amount: positive for money in, negative for money out
  amount: number;
  balanceAfter: number | null;
  statementLine: number | null;
  createdAt: string;
  updatedAt: string;
  potentialDuplicate: boolean;
}

/**
 * @brief Request body used to manually create a transaction.
 */
export interface CreateTransactionRequest {
  accountId: string;
  categoryId?: string | null;
  transactionDate: string;
  description: string;
  amount: number;
  confirmPotentialDuplicate?: boolean;
}

/**
 * @brief Request body used to update a transaction's editable fields.
 * The account a transaction belongs to cannot be changed.
 */
export interface UpdateTransactionRequest {
  categoryId?: string | null;
  transactionDate: string;
  description: string;
  amount: number;
  confirmPotentialDuplicate?: boolean;
}

/**
 * @brief Optional filters used when retrieving the authenticated user's transactions.
 */
export interface TransactionFilters {
  accountId?: string;
  categoryId?: string;
  uncategorized?: boolean;
  fromDate?: string;
  toDate?: string;
  minAmount?: string;
  maxAmount?: string;
  search?: string;
}

/**
 * @brief Defines the supported fields that transactions can be sorted by
 *        when requesting transactions from the API.
 */
export type TransactionSortField = "date" | "description" | "amount";

/**
 * @brief Defines the sort direction for transaction results.
 *        "asc" sorts in ascending order, while "desc" sorts in descending order.
 */
export type SortOrder = "asc" | "desc";

/**
 * @brief Retrieves the authenticated user's transactions, newest first.
 * @returns A promise containing the user's transactions.
 * @throws ApiError If the request fails or the user is not authenticated.
 */
export function getTransactions(
  filters: TransactionFilters = {},
  sortBy?: TransactionSortField,
  sortOrder?: SortOrder,
): Promise<TransactionResponse[]> {
  const query = new URLSearchParams();
  for (const [key, value] of Object.entries(filters)) {
    if (value !== undefined && value !== "" && value !== false) {
      query.set(key, String(value));
    }
  }

  if (sortBy) {
    query.set("sortBy", sortBy);
  }

  if (sortOrder) {
    query.set("sortOrder", sortOrder);
  }

  const queryString = query.toString();
  return apiGet<TransactionResponse[]>(
    `/api/transactions${queryString ? `?${queryString}` : ""}`,
  );
}

/**
 * @brief Manually creates a transaction in one of the authenticated user's accounts.
 * @param request The transaction details to create.
 * @returns A promise containing the created transaction.
 * @throws ApiError If validation fails, the account is not found, or the request fails.
 */
export function createTransaction(
  request: CreateTransactionRequest,
): Promise<TransactionResponse> {
  return apiPost<CreateTransactionRequest, TransactionResponse>(
    "/api/transactions",
    request,
  );
}

/**
 * @brief Updates the editable fields of one of the authenticated user's transactions.
 * @param id The ID of the transaction to update.
 * @param request The transaction details to update.
 * @returns A promise containing the updated transaction.
 * @throws ApiError If validation fails, the transaction is not found, or the request fails.
 */
export function updateTransaction(
  id: string,
  request: UpdateTransactionRequest,
): Promise<TransactionResponse> {
  return apiPut<UpdateTransactionRequest, TransactionResponse>(
    `/api/transactions/${id}`,
    request,
  );
}

/**
 * @brief Deletes one of the authenticated user's transactions.
 * @param id The ID of the transaction to delete.
 * @returns A promise that resolves when the transaction has been deleted.
 * @throws ApiError If the transaction is not found or the request fails.
 */
export function deleteTransaction(id: string): Promise<void> {
  return apiDelete(`/api/transactions/${id}`);
}

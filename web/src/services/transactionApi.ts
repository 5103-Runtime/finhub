import { apiDelete, apiGet, apiPost, apiPut } from "./apiClient";

/**
 * @brief Response returned for a ledger transaction.
 */
export interface TransactionResponse {
  id: string;
  accountId: string;
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
  transactionDate: string;
  description: string;
  amount: number;
}

/**
 * @brief Retrieves the authenticated user's transactions, newest first.
 * @returns A promise containing the user's transactions.
 * @throws ApiError If the request fails or the user is not authenticated.
 */
export function getTransactions(): Promise<TransactionResponse[]> {
  return apiGet<TransactionResponse[]>("/api/transactions");
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

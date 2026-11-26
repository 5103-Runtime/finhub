import { apiPost, apiPostForm } from "./apiClient";
import type { AccountType } from "./accountsApi";
import type { TransactionResponse } from "./transactionApi";

/**
 * @brief One candidate transaction parsed from a statement.
 */
export interface ParsedTransactionResponse {
  statementLine: number;
  // Date only, formatted as YYYY-MM-DD
  transactionDate: string;
  description: string;
  amount: number;
  balanceAfter: number | null;
  potentialDuplicate: boolean;
  categoryId: string | null;
  categoryWasExplicitlySet: boolean;
}

/**
 * @brief Response returned when a statement is parsed.
 */
export interface ParsedStatementResponse {
  bank: string;
  accountType: AccountType;
  skippedRows: number;
  transactions: ParsedTransactionResponse[];
}

/**
 * @brief Response returned when a guest previews a statement.
 */
export interface PreviewStatementResponse {
  bank: string;
  accountType: AccountType;
  skippedRows: number;
  totalTransactions: number;
  transactions: Omit<
    ParsedTransactionResponse,
    "potentialDuplicate" | "categoryId" | "categoryWasExplicitlySet"
  >[];
}

/**
 * @brief One confirmed statement line to save as a transaction.
 */
export interface ImportTransactionLine {
  statementLine: number;
  transactionDate: string;
  description: string;
  amount: number;
  balanceAfter: number | null;
  categoryId: string | null;
  confirmPotentialDuplicate: boolean;
}

/**
 * @brief Request body used to save the confirmed lines of a parsed statement.
 */
export interface ImportStatementRequest {
  accountId: string;
  transactions: ImportTransactionLine[];
}

/**
 * @brief Uploads a CSV statement and returns its lines as candidate transactions.
 * Nothing is saved,the caller reviews the candidates and then calls importStatement.
 * @param file The CSV export from the bank.
 * @param accountId The account the statement belongs to.
 * @returns A promise containing the parsed statement.
 * @throws ApiError if the file cannot be read, the account is not found, or the request fails.
 */
export function parseStatement(
  file: File,
  accountId: string,
): Promise<ParsedStatementResponse> {
  const form = new FormData();
  form.append("file", file);
  form.append("accountId", accountId);
  return apiPostForm<ParsedStatementResponse>("/api/statements/parse", form);
}

/**
 * @brief Uploads a CSV statement for a guest preview. The API does not save the upload or its transactions.
 * @param file The CSV export from the bank.
 * @returns A promise containing the parsed statement preview.
 * @throws ApiError if the file cannot be read or the request fails.
 */
export function previewStatement(
  file: File,
): Promise<PreviewStatementResponse> {
  const form = new FormData();
  form.append("file", file);
  return apiPostForm<PreviewStatementResponse>(
    "/api/statements/preview",
    form,
  );
}

/**
 * @brief Saves the confirmed lines of a parsed statement.
 * @param request The account and the lines to save.
 * @returns A promise containing the saved transactions.
 * @throws ApiError If a line is a unconfirmed duplicate, a category is not found, or the request fails.
 */
export function importStatement(
  request: ImportStatementRequest,
): Promise<TransactionResponse[]> {
  return apiPost<ImportStatementRequest, TransactionResponse[]>(
    "/api/statements/import",
    request,
  );
}

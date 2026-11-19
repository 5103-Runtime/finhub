import { apiDelete, apiPost } from "./apiClient";

/**
 * Request body used to create an internal transfer.
 */
export interface CreateTransferRequest {
  fromAccountId: string;
  toAccountId: string;
  transferDate: string;
  description: string;
  amount: number;
  sourceCategoryId?: string | null;
  destinationCategoryId?: string | null;
  sourceCategoryWasExplicitlySet?: boolean;
  destinationCategoryWasExplicitlySet?: boolean;
  fromTransactionId?: string;
  toTransactionId?: string;
  confirmPotentialDuplicate?: boolean;
}

/**
 * Response returned for an internal transfer.
 */
export interface TransferResponse {
  id: string;
  fromAccountId: string;
  toAccountId: string;
  amount: number;
  transferDate: string;
  description: string;
  fromTransactionId: string;
  toTransactionId: string;
}

/**
 * Response returned when previewing categorization for transfer ledger lines.
 */
export interface TransferCategorizationPreviewResponse {
  outgoingCategoryId: string | null;
  incomingCategoryId: string | null;
  automaticCategoryApplied: boolean;
}

/**
 * Creates an internal transfer, optionally reusing two existing unlinked transactions.
 */
export function createTransfer(
  request: CreateTransferRequest,
): Promise<TransferResponse> {
  return apiPost<CreateTransferRequest, TransferResponse>(
    "/api/transfers",
    request,
  );
}

/**
 * Previews categorization for a transfer that will create two ledger lines.
 */
export function previewTransferCategorization(
  request: CreateTransferRequest,
): Promise<TransferCategorizationPreviewResponse> {
  return apiPost<CreateTransferRequest, TransferCategorizationPreviewResponse>(
    "/api/transfers/preview-categorization",
    request,
  );
}

/**
 * Deletes a transfer and both linked ledger transactions.
 */
export function deleteTransfer(id: string): Promise<void> {
  return apiDelete(`/api/transfers/${id}`);
}

/**
 * Unlinks both ledger lines from a transfer and preserves them as transactions.
 */
export function unlinkTransfer(id: string): Promise<void> {
  return apiPost<Record<string, never>, void>(
    `/api/transfers/${id}/unlink`,
    {},
  );
}

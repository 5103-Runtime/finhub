import { apiGet, apiPost, apiPut } from "./apiClient";

export const INSIGHTS_CHANGED_EVENT = "finhub:insights-changed";

/**
 * @brief Signals that unread insight data may have changed.
 */
export function notifyInsightsChanged(): void {
  window.dispatchEvent(new Event(INSIGHTS_CHANGED_EVENT));
}

/**
 * Represents one computed insight returned as an alert.
 */
export interface InsightAlertResponse {
  id: string;
  insightType: "category_spike" | "unusual_purchase";
  severity: "warning" | "critical";
  categoryId?: string;
  transactionId?: string;
  currency?: string;
  periodStart?: string;
  amount: number;
  baseline: number;
  isRead: boolean;
}

/**
 * Represents the authenticated user's preference for one insight type.
 */
export interface InsightPreferenceResponse {
  id: string;
  insightType: "category_spike" | "unusual_purchase";
  isEnabled: boolean;
  threshold: number | null;
  createdAt: string;
  updatedAt: string;
}

/**
 * Describes values used to create or update an insight preference.
 */
export interface UpdateInsightPreferenceRequest {
  isEnabled: boolean;
  threshold: number | null;
}

/**
 * Identifies an insight and supplies values used to change its state.
 */
export interface UpdateInsightStateRequest {
  insightType: "category_spike" | "unusual_purchase";
  categoryId?: string;
  transactionId?: string;
  currency?: string;
  periodStart?: string;
}

/**
 * Loads unread alerts for the authenticated user.
 * @param months The number of calendar months to evaluate.
 * @returns The unread alert read model.
 */
export function getUnreadAlerts(months = 6): Promise<InsightAlertResponse[]> {
  return apiGet<InsightAlertResponse[]>(`/api/alerts/unread?months=${months}`);
}

/**
 * Loads visible insight history for the authenticated user.
 * @param months The number of calendar months to evaluate.
 * @returns The visible alert history.
 */
export function getAlertHistory(months = 6): Promise<InsightAlertResponse[]> {
  return apiGet<InsightAlertResponse[]>(`/api/alerts/history?months=${months}`);
}

/**
 * Marks an alert as read.
 * @param request The insight subject to mark read.
 * @returns The updated alert state response.
 */
export function markAlertRead(
  request: UpdateInsightStateRequest,
): Promise<InsightAlertResponse> {
  return apiPost<UpdateInsightStateRequest, InsightAlertResponse>(
    "/api/alerts/read",
    request,
  );
}

/**
 * Dismisses an alert.
 * @param request The insight subject and current amount to dismiss.
 * @returns The updated dismissed-alert response.
 */
export function dismissAlert(
  request: UpdateInsightStateRequest,
): Promise<InsightAlertResponse> {
  return apiPost<UpdateInsightStateRequest, InsightAlertResponse>(
    "/api/alerts/dismiss",
    request,
  );
}

/**
 * Loads the authenticated user's insight preferences.
 * @returns The configured insight preferences.
 */
export function getInsightPreferences(): Promise<InsightPreferenceResponse[]> {
  return apiGet<InsightPreferenceResponse[]>("/api/preferences");
}

/**
 * Creates or updates one authenticated-user insight preference.
 * @param insightType The insight type to configure.
 * @param request The enabled state and optional threshold.
 * @returns The saved preference.
 */
export function updateInsightPreference(
  insightType: "category_spike" | "unusual_purchase",
  request: UpdateInsightPreferenceRequest,
): Promise<InsightPreferenceResponse> {
  return apiPut<UpdateInsightPreferenceRequest, InsightPreferenceResponse>(
    `/api/preferences/${insightType}`,
    request,
  );
}

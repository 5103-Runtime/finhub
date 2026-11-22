import { apiDelete, apiGet, apiPost, apiPut } from "./apiClient";

/**
 * @brief Whether a rule matches money coming in (amount > 0) or going out (amount < 0).
 */
export type AmountDirection = "in" | "out";

/**
 * @brief Response returned for a categorization rule.
 */
export interface CategorizationRuleResponse {
  id: string;
  categoryId: string;
  accountId: string | null;
  descriptionContains: string | null;
  amountDirection: AmountDirection | null;
  priority: number;
  isEnabled: boolean;
  createdAt: string;
  updatedAt: string;
}

/**
 * @brief Request body used to create or update a categorization rule.
 * At least one of accountId, descriptionContains or amountDirection is required.
 */
export type CategorizationRuleRequest = {
  categoryId: string;
  accountId: string | null;
  descriptionContains: string | null;
  amountDirection: AmountDirection | null;
  priority: number;
};

/**
 * @brief Retrieves the user's rules ordered by priority (first match wins).
 */
export function getCategorizationRules(): Promise<CategorizationRuleResponse[]> {
  return apiGet<CategorizationRuleResponse[]>("/api/categorization-rules");
}

/**
 * @brief Creates a new categorization rule
 * @param request The CategorizationRuleRequest object with information on created rule
 */
export function createCategorizationRule(
  request: CategorizationRuleRequest,
): Promise<CategorizationRuleResponse> {
  return apiPost<CategorizationRuleRequest, CategorizationRuleResponse>(
    "/api/categorization-rules",
    request,
  );
}

/**
 * @brief Updates selected categorization rule
 * @param id Id of rule to update
 * @param request CategorizationRuleRequest object with updated rule information
 */
export function updateCategorizationRule(
  id: string,
  request: CategorizationRuleRequest,
): Promise<CategorizationRuleResponse> {
  return apiPut<CategorizationRuleRequest, CategorizationRuleResponse>(
    `/api/categorization-rules/${id}`,
    request,
  );
}

/**
 * @brief Deletes selected categorization rule
 * @param id Id of rule to delete
 */
export function deleteCategorizationRule(id: string): Promise<void> {
  return apiDelete(`/api/categorization-rules/${id}`);
}

/**
 * @brief Enables selected categorization rule
 * @param id Id of rule to enable
 */
export function enableCategorizationRule(
  id: string,
): Promise<CategorizationRuleResponse> {
  return apiPost<Record<string, never>, CategorizationRuleResponse>(
    `/api/categorization-rules/${id}/enable`,
    {},
  );
}

/**
 * @brief Disables selected categorization rule
 * @param id Id of rule to disable
 */
export function disableCategorizationRule(
  id: string,
): Promise<CategorizationRuleResponse> {
  return apiPost<Record<string, never>, CategorizationRuleResponse>(
    `/api/categorization-rules/${id}/disable`,
    {},
  );
}

/**
 * @brief Saves a new rule order. The list must contain every rule ID exactly once,
 * from highest to lowest priority.
 * @param ruleIds List of rules to be reordered
 */
export function reorderCategorizationRules(
  ruleIds: string[],
): Promise<CategorizationRuleResponse[]> {
  return apiPut<{ ruleIds: string[] }, CategorizationRuleResponse[]>(
    "/api/categorization-rules/reorder",
    { ruleIds },
  );
}

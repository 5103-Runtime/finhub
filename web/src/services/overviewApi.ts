import { apiGet } from "./apiClient";
import type { AccountCurrency, AccountType } from "./accountsApi";

/**
 * Represents the calculated balance for one financial account.
 */
export interface OverviewAccountBalance {
  accountId: string;
  name: string;
  accountType: AccountType;
  currency: AccountCurrency;
  balance: number;
}

/**
 * Represents income, spending, and net movement for one currency.
 */
export interface OverviewCurrencySummary {
  currency: string;
  income: number;
  spending: number;
  net: number;
}

/**
 * Represents one monthly point in the dashboard cash-flow trend.
 */
export interface OverviewMonthlyPoint {
  month: string;
  label: string;
  currency: string;
  income: number;
  spending: number;
  net: number;
}

/**
 * Represents spending grouped by category for the dashboard period.
 */
export interface OverviewCategorySpending {
  categoryId: string;
  categoryName: string;
  currency: string;
  amount: number;
}

/**
 * Represents the complete dashboard read model returned by the overview API.
 */
export interface OverviewDashboardResponse {
  periodStart: string;
  periodEnd: string;
  months: number;
  currencies: string[];
  accounts: OverviewAccountBalance[];
  balances: OverviewCurrencySummary[];
  balancesBeforeCurrentMonth: OverviewCurrencySummary[];
  currentPeriod: OverviewCurrencySummary[];
  previousPeriod: OverviewCurrencySummary[];
  monthlyTrend: OverviewMonthlyPoint[];
  categorySpending: OverviewCategorySpending[];
}

/**
 * Loads the authenticated user's overview dashboard aggregates.
 *
 * @param months The number of calendar months to include, from 3 through 12.
 * @returns A promise containing the dashboard read model.
 */
export function getOverview(months = 6): Promise<OverviewDashboardResponse> {
  return apiGet<OverviewDashboardResponse>(`/api/overview?months=${months}`);
}

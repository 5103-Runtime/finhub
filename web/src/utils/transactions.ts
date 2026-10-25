import type { AccountCurrency } from "@app/services/accountsApi";
import { formatCurrency } from "@app/utils/finance";

/**
 * @brief Gets today's date in the user's local time zone.
 * @returns The date formatted as YYYY-MM-DD, the format a date input expects.
 */
export function getTodayDate() {
  const now = new Date();
  const month = String(now.getMonth() + 1).padStart(2, "0");
  const day = String(now.getDate()).padStart(2, "0");
  return `${now.getFullYear()}-${month}-${day}`;
}

/**
 * @brief Formats a YYYY-MM-DD transaction date for display.
 * The time is added so the date is read in local time instead of UTC,
 * which would show the previous day in Canadian time zones.
 * @param isoDate The date formatted as YYYY-MM-DD.
 * @returns A short localized date such as "Oct 20, 2026".
 */
export function formatTransactionDate(isoDate: string) {
  const date = new Date(`${isoDate}T00:00:00`);
  if (Number.isNaN(date.getTime())) {
    return isoDate;
  }

  return date.toLocaleDateString("en-CA", {
    year: "numeric",
    month: "short",
    day: "numeric",
  });
}

/**
 * @brief Formats a signed transaction amount with the minus sign before the
 * currency symbol (-US$12.50, -$12.50). formatCurrency is left unchanged:
 * it only receives the absolute value here.
 * @param amount The signed amount: negative for money out.
 * @param currency The account's currency.
 * @returns A formatted currency string.
 */
export function formatSignedCurrency(
  amount: number,
  currency: AccountCurrency,
) {
  // Check the rounded value so tiny negatives don't show "-$0.00".
  const sign = Math.round(amount * 100) < 0 ? "-" : "";
  return `${sign}${formatCurrency(Math.abs(amount), currency)}`;
}

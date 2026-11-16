/**
 * Formats a monetary value using the account's supported currency.
 * CAD uses the local dollar symbol, while USD is explicitly prefixed with
 * US$ to avoid ambiguity.
 * @param amount The monetary value to format.
 * @param currency The currency code for the value.
 * @returns A localized currency string.
 */
export function formatCurrency(amount: number, currency: "CAD" | "USD") {
  if (currency === "USD") {
    return `US$${new Intl.NumberFormat("en-CA", {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    }).format(amount)}`;
  }

  return new Intl.NumberFormat("en-CA", {
    style: "currency",
    currency: "CAD",
    currencyDisplay: "narrowSymbol",
  }).format(amount);
}

/**
 * Formats a dashboard value when the API returns a currency code as data.
 */
export function formatDashboardCurrency(value: number, currency: string) {
  return formatCurrency(value, currency === "USD" ? "USD" : "CAD");
}

/**
 * Calculates the percentage change between two numeric values.
 * Uses the magnitude of the previous value so changes from negative balances
 * keep the expected direction.
 * @param currentValue The current period value.
 * @param previousValue The comparison period value.
 * @returns The percentage change from the previous value.
 */
export function getPercentageChange(
  currentValue: number,
  previousValue: number,
) {
  return ((currentValue - previousValue) / Math.abs(previousValue)) * 100;
}

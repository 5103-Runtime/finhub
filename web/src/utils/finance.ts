/**
 * Formats CAD values for the Overview metric cards.
 */
export const currencyFormatter = new Intl.NumberFormat("en-CA", {
  style: "currency",
  currency: "CAD",
});

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
 * Calculates the percentage change between two numeric values.
 * @param currentValue The current period value.
 * @param previousValue The comparison period value.
 * @returns The percentage change from the previous value.
 */
export function getPercentageChange(
  currentValue: number,
  previousValue: number,
) {
  return ((currentValue - previousValue) / previousValue) * 100;
}

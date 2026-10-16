export const currencyFormatter = new Intl.NumberFormat("en-CA", {
  style: "currency",
  currency: "CAD",
});

export function getPercentageChange(
  currentValue: number,
  previousValue: number,
) {
  return ((currentValue - previousValue) / previousValue) * 100;
}

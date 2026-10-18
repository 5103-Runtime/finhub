import type { AccountType } from "@app/services/accountsApi";
import type { AccountResponse } from "@app/services/accountsApi";
import { formatCurrency } from "@app/utils/finance";
import AccountBalanceRounded from "@mui/icons-material/AccountBalanceRounded";
import AutoGraphRounded from "@mui/icons-material/AutoGraphRounded";
import CreditCardRounded from "@mui/icons-material/CreditCardRounded";
import type { SvgIconComponent } from "@mui/icons-material";

/**
 * Display labels for supported financial account types.
 */
export const accountTypeLabels: Record<AccountType, string> = {
  chequing: "Chequing",
  savings: "Savings",
  credit_card: "Credit card",
};

/**
 * Account types in the order used when presenting grouped accounts.
 */
export const accountTypeOrder: AccountType[] = [
  "chequing",
  "savings",
  "credit_card",
];

/**
 * Icons used to represent each financial account type.
 */
export const accountTypeIcons: Record<AccountType, SvgIconComponent> = {
  chequing: AccountBalanceRounded,
  savings: AutoGraphRounded,
  credit_card: CreditCardRounded,
};

// Temporary fallback until the API provides a trusted exchange rate.
const USD_TO_CAD_EXCHANGE_RATE = 1.4;

/**
 * Returns the current account balance until transaction-backed balances exist.
 * @param account The account whose balance should be read.
 * @returns The account balance, or zero before transaction-backed balances are available.
 */
export function getAccountBalance(account: AccountResponse): number {
  return account.balance ?? 0;
}

/**
 * Converts an account balance to CAD for cross-currency comparisons.
 * @param account The account whose balance should be converted.
 * @returns The account balance represented in CAD.
 */
export function getAccountBalanceInCad(account: AccountResponse): number {
  return account.currency === "USD"
    ? getAccountBalance(account) * USD_TO_CAD_EXCHANGE_RATE
    : getAccountBalance(account);
}

/**
 * Formats an account balance using its currency.
 * @param account The account whose currency should be used.
 * @returns A localized currency string for the account balance.
 */
export function formatAccountBalance(account: AccountResponse): string {
  return formatCurrency(getAccountBalance(account), account.currency);
}

/**
 * Compares accounts by display name using a case-insensitive alphabetical order.
 */
export function compareAccountsByName(
  left: AccountResponse,
  right: AccountResponse,
): number {
  return left.name.localeCompare(right.name, undefined, {
    sensitivity: "base",
  });
}

/**
 * Returns the plural display label for an account type.
 * @param accountType The account type to label.
 * @returns The plural account type label.
 */
export function getAccountTypePluralLabel(accountType: AccountType): string {
  return accountType === "credit_card"
    ? "Credit cards"
    : accountTypeLabels[accountType];
}

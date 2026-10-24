import z from "zod";
import { getTodayDate } from "@app/utils/transactions";

const MAX_AMOUNT = 999999999999.99; //For Future ref, look for different way to set

/**
 * @brief Validates the fields used to create a transaction.
 * the amount stays as text in the form and is converted to a number on submit
 */
export const transactionSchema = z.object({
  accountId: z.string().min(1, "Choose an account."),
  transactionDate: z.string().min(1, "Date is required."),
  description: z
    .string()
    .trim()
    .min(1, "Description is required.")
    .max(500, "Description must be 500 characters or less."),
  amount: z
    .string()
    .trim()
    .min(1, "Amount is required.")
    .regex(
      /^-?\d+(\.\d{1,2})?$/,
      "Enter an amount with up to 2 decimals, such as -12.50.",
    )
    .refine(
      (value) => Math.abs(Number(value)) <= MAX_AMOUNT,
      "Amount is too large.",
    ),
});

/**
 * @brief the Typscript type for the transaction create form data.
 */
export type TransactionFormData = z.infer<typeof transactionSchema>;

/**
 * @brief Validates the fields used to edit a transaction. The account cannot be changed.
 */
export const transactionEditSchema = transactionSchema.omit({
  accountId: true,
});

/**
 * @brief The TypeScript type for the transaction inline edit form data.
 */
export type TransactionEditFormData = z.infer<typeof transactionEditSchema>;

/**
 * @brief Builds the default values for the transaction create form, dated today.
 * @returns Empty form values.
 */
export function getEmptyTransactionForm(): TransactionFormData {
  return {
    accountId: "",
    transactionDate: getTodayDate(),
    description: "",
    amount: "",
  };
}
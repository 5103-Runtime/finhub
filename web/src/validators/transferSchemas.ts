import z from "zod";
import { getTodayDate } from "@app/utils/transactions";

/**
 * Validates the fields used to create an internal transfer.
 */
export const transferSchema = z
  .object({
    transferMode: z.enum(["new", "existing"]),
    fromAccountId: z.string().min(1, "Choose a source account."),
    toAccountId: z.string().min(1, "Choose a destination account."),
    transferDate: z.string().min(1, "Date is required."),
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
        "Enter a positive amount with up to 2 decimals, such as 12.50.",
      )
      .refine(
        (value) => Math.abs(Number(value)) <= 999_999_999_999.99,
        "Amount is too large.",
      ),
    categoryId: z.string(),
    fromTransactionId: z.string(),
    toTransactionId: z.string(),
  })
  .superRefine((data, context) => {
    if (
      data.transferMode === "existing" &&
      !data.fromTransactionId &&
      !data.toTransactionId
    ) {
      for (const path of ["fromTransactionId", "toTransactionId"] as const) {
        context.addIssue({
          code: "custom",
          path: [path],
          message: "Choose at least one existing transaction.",
        });
      }
    }

    if (data.fromAccountId && Number(data.amount) <= 0) {
      context.addIssue({
        code: "custom",
        path: ["amount"],
        message: "Transfer amount must be greater than zero.",
      });
    }
  });

export type TransferFormData = z.infer<typeof transferSchema>;

/**
 * Builds the default values for the transfer form.
 */
export function getEmptyTransferForm(
  fromAccountId = "",
  toAccountId = "",
): TransferFormData {
  return {
    transferMode: "new",
    fromAccountId,
    toAccountId,
    transferDate: getTodayDate(),
    description: "",
    amount: "",
    categoryId: "",
    fromTransactionId: "",
    toTransactionId: "",
  };
}

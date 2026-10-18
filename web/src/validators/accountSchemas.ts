import z from "zod";

/**
 * Validates the fields used to create or update a financial account.
 */
export const accountSchema = z.object({
  name: z
    .string()
    .trim()
    .min(1, "Account name is required.")
    .max(100, "Account name must be 100 characters or less."),
  institution: z
    .string()
    .trim()
    .max(100, "Institution must be 100 characters or less."),
  accountType: z.enum(["chequing", "savings", "credit_card"]),
  currency: z.enum(["CAD", "USD"]),
});

export type AccountFormData = z.infer<typeof accountSchema>;

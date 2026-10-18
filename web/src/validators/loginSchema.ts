import z from "zod";
import { emailSchema } from "./emailSchema";

/**
 * Validates the fields used to login to a FinHub account.
 */
export const loginSchema = z.object({
  email: emailSchema,
  password: z
    .string()
    .min(1, "Password is required")
    .max(128, "Password must be 128 characters or less"),
});
/**
 * The TypeScript type for the account login form data, derived from the loginSchema.
 */
export type LoginFormData = z.infer<typeof loginSchema>;

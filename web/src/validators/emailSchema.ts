import z from "zod";

/**
 * Validates a single email address.
 */
export const emailSchema = z
  .email("Enter a valid email address")
  .max(254, "Email must be 254 characters or less");

/**
 * Form schema for pages that only collect an email (e.g. forgot password).
 */
export const emailFormSchema = z.object({
  email: emailSchema,
});

/**
 * The TypeScript type for the forgot password data, derived from the emailFormSchema.
 */
export type EmailFormData = z.infer<typeof emailFormSchema>;

import z from "zod";

/**
 * Validates a new password against the application's password requirements:
 * 8-128 characters, including uppercase, lowercase, numeric, and special characters.
 */
export const newPasswordSchema = z
  .string()
  .min(8, "Password must be at least 8 characters long.")
  .max(128, "Password must be 128 characters or less.")
  .regex(/[A-Z]/, "Password must contain at least one uppercase letter.")
  .regex(/[a-z]/, "Password must contain at least one lowercase letter.")
  .regex(/[0-9]/, "Password must contain at least one number.")
  .regex(
    /[^A-Za-z0-9]/,
    "Password must contain at least one special character.",
  );

/**
 * Validates the complete change-password form, including confirmation that
 * the new password fields match.
 */
export const passwordSchema = z
  .object({
    currentPassword: z.string().min(1, "Current password is required"),
    password: newPasswordSchema,
    confirmPassword: z.string().min(1, "Please re-enter your password"),
  })
  .refine((data) => data.password === data.confirmPassword, {
    message: "Passwords do not match.",
    path: ["confirmPassword"],
  });

/**
 * Represents the validated values submitted by the change-password form.
 */
export type PasswordFormData = z.infer<typeof passwordSchema>;

import z from "zod";
import { newPasswordSchema } from "./passwordSchemas";
import { emailSchema } from "./emailSchema";

/** 
 * Validates an email address, password and a confirm password field
 */
export const registerSchema = z
  .object({
    email: emailSchema,
    password: newPasswordSchema,
    confirmPassword: z.string().min(1, "Please re-enter your password"),
  })
  .refine((data) => data.password === data.confirmPassword, {
    message: "Passwords do not match.",
    path: ["confirmPassword"],
  });
/**
 * The TypeScript type for the register form data, derived from the registerSchema.
 */
export type RegisterFormData = z.infer<typeof registerSchema>;
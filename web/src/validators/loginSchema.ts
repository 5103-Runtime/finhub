import z from "zod";
import { emailSchema } from "./emailSchema";

export const loginSchema = z.object({
  email: emailSchema,
  password: z
    .string()
    .min(1, "Password is required")
    .max(128, "Password must be 128 characters or less"),
});

export type LoginFormData = z.infer<typeof loginSchema>;

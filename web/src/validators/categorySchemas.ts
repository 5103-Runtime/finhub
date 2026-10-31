import z from "zod";

export const categorySchema = z.object({
  name: z
    .string()
    .trim()
    .min(1, "Category name is required.")
    .max(50, "Category name must be 50 characters or less."),
});

export type CategoryFormData = z.infer<typeof categorySchema>;

export const emptyCategoryForm: CategoryFormData = { name: "" };

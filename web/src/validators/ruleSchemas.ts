import z from "zod";

/**
 * Validates a ruleset for a category. Must have at least one category and one condition to evaluate
 */
export const ruleSchema = z
  .object({
    categoryId: z.string().min(1, "Choose a category."),
    accountId: z.string(),
    descriptionContains: z
      .string()
      .trim()
      .max(500, "Text must be 500 characters or less."),
    amountDirection: z.enum(["", "in", "out"]),
  })
  .superRefine((data, ctx) => {
    if (!data.accountId && !data.descriptionContains && !data.amountDirection) {
      ctx.addIssue({
        code: "custom",
        path: ["descriptionContains"],
        message:
          "Add at least one condition: an account, description text, or money in/out.",
      });
    }
  });

/**
 * Represents the validated values submitted by a rule form
 */
export type RuleFormData = z.infer<typeof ruleSchema>;

/**
 * Represents an empty rule form
 */
export const emptyRuleForm: RuleFormData = {
  categoryId: "",
  accountId: "",
  descriptionContains: "",
  amountDirection: "",
};

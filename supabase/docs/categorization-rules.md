# Categorization rules

`public.categorization_rules` holds the rules a user defines to assign a category to matching transactions. A rule targets a category and may be limited to one account; see [Categories](./categories.md) and [Financial accounts](./financial-accounts.md).

## Design

- **`user_id` references `auth.users`**, `on delete cascade`. Deleting a user removes their rules.
- **`category_id` is the category a matching transaction receives**, `on delete cascade`. Deleting a custom category removes the rules that target it.
- **A rule has up to three criteria, and a transaction must meet every one that is set.** Null means the criterion is not checked; at least one must be set.
  - `account_id` limits the rule to one account, `on delete cascade`.
  - `description_contains` is text the transaction description must contain, 1 to 500 characters, trimmed.
  - `amount_direction` is `in` for positive amounts or `out` for negative amounts. A zero amount matches neither.
- **`priority` is unique per user; the lowest value is checked first.** The constraint is `deferrable initially deferred`, so two rules can swap priorities inside one transaction and a duplicate is rejected at commit. Its index also covers the per-user lookup.
- `is_enabled` turns a rule off without deleting it. A disabled rule keeps its priority.
- `validate_categorization_rule()` rejects a category that is neither a system category nor the rule owner's, and an account that is not the rule owner's.
- `categorization_rules_category_id_idx` and `categorization_rules_account_id_idx` cover the foreign keys and their cascading deletes.

Deliberately left out:

- Merchant text. A transaction carries one text column, `description`, so a merchant criterion would search the same text as `description_contains`.
- Match modes other than contains.
- Applying the rules. Evaluation belongs to the API.

## EF Core

Do not declare the `(user_id, priority)` index as unique in the EF model. With `IsUnique()`, swapping two priorities fails in `SaveChanges` with a circular dependency error before any SQL is sent; without it, the swap saves and the database still rejects duplicates. Inside an explicit transaction a duplicate priority is reported by the commit as a `PostgresException`, not by `SaveChanges` as a `DbUpdateException`.

## Security

Row Level Security is enabled with select, insert, update, and delete policies, each scoped to `(select auth.uid()) = user_id`. The API bypasses RLS, so it must filter by the authenticated user itself.

## Verify The Migration

In the Supabase Dashboard:

1. Open **Table Editor** and confirm `public.categorization_rules` exists.
2. Open the table policies and confirm Row Level Security is enabled with select, insert, update, and delete policies.
3. Insert a rule with a description criterion and confirm `created_at` and `updated_at` are set.
4. Insert a rule with no account, description, or direction and confirm the insert is rejected.
5. Insert a second rule with the same `user_id` and `priority` and confirm the insert is rejected.
6. Insert a rule that targets another user's custom category and confirm the insert is rejected.
7. Delete a custom category and confirm the rules that target it are gone.

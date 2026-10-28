# Categories

`public.categories` holds shared system categories and optional custom categories
owned by users. Transactions reference a category; see [Transactions](./transactions.md).

## Design

- **`user_id` is nullable.** System categories have no owner; custom categories reference `auth.users` and are owned by one user.
- **`is_system` distinguishes shared and custom categories.** System categories are shared by all users; custom categories belong to the user in `user_id`.
- **`name` is 1 to 50 characters and trimmed.** The database rejects stray leading or trailing whitespace. Names may contain spaces, and system names cannot be reused by custom categories.
- Custom category names are unique per user, case-insensitively. System names cannot be reused by custom categories.
- System categories are seeded by [the system category migration](../migrations/20261026213000_seed_system_categories.sql).
- A transaction has at most one category through its nullable `category_id` foreign key. Null means the transaction is uncategorized.
- Deleting a custom category sets `transactions.category_id` to null rather than deleting transactions.
- `categories_user_id_idx` and `categories_user_name_idx` support ownership and category lookups.

The default system categories are:

- Groceries
- Dining
- Transportation
- Housing
- Utilities
- Entertainment
- Income
- Savings
- Other

Deliberately left out:

- Automatic categorization. Categories are assigned manually for now; import rules can be added later.

## Security

Row Level Security is enabled with select, insert, update, and delete policies, each scoped to system categories or the authenticated user's own categories. The API bypasses RLS, so it must filter categories by the authenticated user itself.

## Verify The Migration

In the Supabase Dashboard:

1. Open **Table Editor** and confirm `public.categories` exists.
2. Open the table policies and confirm Row Level Security is enabled with select, insert, update, and delete policies.
3. Confirm the system category rows have `is_system = true` and `user_id = null`.
4. Confirm custom categories are restricted to their owners and system category update/delete requests return `409 Conflict`.
5. Confirm a transaction can use a system category or a category owned by its account owner.
6. Delete a custom category and confirm its transactions remain while their `category_id` values become null.
7. Use the API to create, update, list, and delete a custom category.

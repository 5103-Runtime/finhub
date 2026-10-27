# Categories

`public.categories` holds shared system categories and optional custom categories
owned by users. Transactions reference a category; see [Transactions](./transactions.md).

## Design

- **`user_id` is nullable.** System categories have no owner; custom categories reference `auth.users` and are owned by one user.
- **`is_system` distinguishes shared and custom categories.** System categories are shared by all users; custom categories belong to the user in `user_id`.
- **`name` is 1 to 50 characters, trimmed, and must be one word.** The database rejects stray spaces and multi-word names.
- **`slug` is a lowercase, stable identifier** using lowercase letters, numbers, and hyphens.
- System category slugs are unique. Custom category names are unique per user, case-insensitively.
- System categories are seeded by [the system category migration](../migrations/20261026213000_seed_system_categories.sql).
- A transaction has at most one category through its nullable `category_id` foreign key. Null means the transaction is uncategorized.
- Deleting a custom category sets `transactions.category_id` to null rather than deleting transactions.
- `categories_user_id_idx`, `categories_system_slug_idx`, and `categories_user_name_idx` support ownership and category lookups.

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
- Category API endpoints. Custom category management belongs to the API layer and can be added in a later change.

## Security

Row Level Security allows authenticated users to view system categories and their
own custom categories. Insert, update, and delete policies are limited to custom
categories owned by the authenticated user. A database trigger also prevents
system categories from being modified or deleted. The API bypasses RLS, so it
must apply the same ownership filtering explicitly.

## Verify The Migration

In the Supabase Dashboard:

1. Open **Table Editor** and confirm `public.categories` exists.
2. Confirm the system category rows are present and have `is_system = true` and `user_id` set to null.
3. Open the table policies and confirm users can view system categories but can manage only their own custom categories.
4. Confirm an authenticated user cannot update or delete a system category.
5. Create a custom category for a test user and confirm another user cannot view, update, or delete it.
6. Assign a system or owned custom category to a transaction, then clear the category and confirm null is allowed.
7. Delete a custom category and confirm its transactions remain while their `category_id` values become null.

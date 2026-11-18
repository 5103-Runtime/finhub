# Insights

Insights are computed by the API from transactions on each request and are not stored. Three tables hold what the user controls: `public.insight_preferences`, `public.insight_states`, and `public.insight_mutes`.

## Design

Every table references `auth.users` through `user_id`, `on delete cascade`. Deleting a user removes their rows.

### Preferences

- **One row per user per `insight_type`**, either `category_spike` or `unusual_purchase`. A missing row means the type is enabled with the API default.
- `is_enabled` turns a type off without losing its threshold.
- **`threshold` is null or greater than zero.** Null means the API default applies.
  - For `category_spike` it is the percent above the trailing average.
  - For `unusual_purchase` it is the multiple of the category median.

### States

- **One row per insight the user has read or dismissed.** A row must have `read_at` or `dismissed_at` set.
- **An insight is identified by its subject, which depends on `insight_type`.**
  - `category_spike`: `category_id`, `currency`, and `period_start`, the first day of the month. One row per user for each combination.
  - `unusual_purchase`: `transaction_id`. One row per user per transaction.
- `category_id` and `transaction_id` are `on delete cascade`. Deleting a category or transaction removes its states.
- **`dismissed_value` is set with `dismissed_at` and cleared with it.** It is the positive amount the insight showed when it was dismissed: the period spend for `category_spike`, the purchase amount for `unusual_purchase`. The API shows a dismissed insight again when its current amount is materially higher.
- `validate_insight_state()` rejects a category that is neither a system category nor the state owner's, and a transaction on an account that is not the state owner's.
- `insight_states_category_id_idx` and `insight_states_transaction_id_idx` cover the category and transaction foreign keys and their cascading deletes. The unique constraints start with `user_id` and cover the user foreign key.

### Mutes

- **A mute excludes one category or one account from every insight type.** Exactly one of `category_id` and `account_id` is set, and each target can be muted once per user.
- Both references are `on delete cascade`. Deleting a category or account removes its mute.
- `validate_insight_mute()` rejects a category that is neither a system category nor the mute owner's. A composite foreign key to `financial_accounts (id, user_id)` rejects an account that is not the mute owner's.
- A mute is inserted or deleted, never updated, so the table has no `updated_at`.

Deliberately left out:

- An insights table. Nothing is generated ahead of time, so nothing can go stale when a transaction changes.
- An opt-in list of watched categories or accounts. Everything is watched unless muted.
- Default thresholds. They belong to the API.
- A severity. The API derives it from the current amount.
- States for spending with no category. A `category_spike` needs a category to point at.

## Security

Row Level Security is enabled on all three tables, each policy scoped to `(select auth.uid()) = user_id`. `insight_mutes` has no update policy. The API bypasses RLS, so it must filter by the authenticated user itself. Both trigger functions set an empty `search_path`.

## Verify The Migration

In the Supabase Dashboard:

1. Open **Table Editor** and confirm the three tables exist.
2. Open the table policies and confirm Row Level Security is enabled on each.
3. Insert a preference and confirm `created_at` and `updated_at` are set.
4. Insert a second preference with the same `user_id` and `insight_type` and confirm the insert is rejected.
5. Insert a `category_spike` state with a `transaction_id`, then an `unusual_purchase` state with a `category_id`, and confirm both inserts are rejected.
6. Insert a state with neither `read_at` nor `dismissed_at`, then one with `dismissed_at` but no `dismissed_value`, and confirm both inserts are rejected.
7. Insert a state that targets another user's transaction and confirm the insert is rejected.
8. Delete a transaction that has a state and confirm the state is gone.
9. Insert a mute with both a category and an account, then with neither, and confirm both inserts are rejected.
10. Insert a mute that targets another user's account and confirm the insert is rejected.
11. Delete a muted custom category and confirm its mute is gone.

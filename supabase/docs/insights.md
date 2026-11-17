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

- **One row per user per `insight_key`**, holding `read_at` and `dismissed_at`.
- `insight_key` is built by the API from the insight type, its subject, and its period. It is 1 to 200 characters, trimmed.

### Mutes

- **A mute excludes one category or one account from every insight type.** Exactly one of `category_id` and `account_id` is set, and each target can be muted once per user.
- Both references are `on delete cascade`. Deleting a category or account removes its mute.
- `validate_insight_mute()` rejects a category that is neither a system category nor the mute owner's, and an account that is not the mute owner's.
- A mute is inserted or deleted, never updated, so the table has no `updated_at`.

Deliberately left out:

- An insights table. Nothing is generated ahead of time, so nothing can go stale when a transaction changes.
- An opt-in list of watched categories or accounts. Everything is watched unless muted.
- Default thresholds. They belong to the API.

## Security

Row Level Security is enabled on all three tables, each policy scoped to `(select auth.uid()) = user_id`. `insight_mutes` has no update policy. The API bypasses RLS, so it must filter by the authenticated user itself.

## Verify The Migration

In the Supabase Dashboard:

1. Open **Table Editor** and confirm the three tables exist.
2. Open the table policies and confirm Row Level Security is enabled on each.
3. Insert a preference and confirm `created_at` and `updated_at` are set.
4. Insert a second preference with the same `user_id` and `insight_type` and confirm the insert is rejected.
5. Insert a mute with both a category and an account, then with neither, and confirm both inserts are rejected.
6. Insert a mute that targets another user's account and confirm the insert is rejected.
7. Delete a muted custom category and confirm its mute is gone.

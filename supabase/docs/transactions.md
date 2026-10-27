# Transactions

`public.transactions` holds the ledger lines of a financial account: lines imported from a statement and lines the user enters by hand.

## Design

- **`account_id` references `public.financial_accounts`**, `on delete cascade`. Deleting an account removes its transactions; deleting a user removes both through the same chain.
- **`amount` is signed `numeric(14, 2)`.** Money in is positive, money out is negative, for every account type. Credit card statements that list charges as positive are inverted by the import mapping before the API sees them.
- **`balance_after` is the balance the statement reports after this line**, when the format provides one (most PDFs, some CSVs). Null otherwise. It follows the same sign convention as `amount`: a credit card balance owed is negative, so within one statement the previous line's balance plus `amount` gives `balance_after`.
- **`statement_line` is the line's position on the source statement**, so same-day lines keep the statement's order. Null for entries the user adds by hand.
- `description` is the statement's text for the line, 1 to 500 characters, trimmed. The DB rejects stray whitespace; the API trims before inserting.
- `transactions_account_id_idx` covers the foreign key and the per-account queries.

Deliberately left out:

- A stored balance. Balances derive from imported statements; see [Financial accounts](./financial-accounts.md).
- Duplicate detection. Overlapping imports are compared in the API (FIN-8), not enforced by the schema.
- Categories are optional and arrive through the nullable `category_id` foreign key; see [Categories](./categories.md).

## Security

Row Level Security is enabled with select, insert, update, and delete policies, each scoped through the owning account: `account_id in (select id from public.financial_accounts where user_id = (select auth.uid()))`. The API connects as `postgres`, bypasses RLS, and must filter by the authenticated user itself.

## Verify The Migration

In the Supabase Dashboard:

1. Open **Table Editor** and confirm `public.transactions` exists.
2. Open the table policies and confirm Row Level Security is enabled with select, insert, update, and delete policies.
3. Insert a row for an existing account and confirm `created_at` and `updated_at` are set, then edit it and confirm `updated_at` changes.
4. Insert a row with a description padded with spaces and confirm the insert is rejected.
5. Delete the account and confirm its transactions are gone.

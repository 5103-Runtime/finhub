# Financial accounts

`public.financial_accounts` holds the chequing, savings, and credit card accounts a user imports statements for. Transactions will reference an account.

## Design

- **`user_id` references `auth.users`, not `public.profiles`.**
- `on delete cascade` means deleting a user removes their accounts, and later their transactions through the same chain. Front-end should HEAVILY warn upon deleting data / accounts.
- **`account_type` is one of `chequing`, `savings`, or `credit_card`.** The type matters beyond display: credit card statements may invert the sign of every amount depending on the input format.
- `name` is 1 to 100 characters and must arrive trimmed. The DB rejects a name with stray spaces; the API should trim before inserting.
- `institution` is nullable and follows the same trim rule. Null means no bank recorded; the API maps a blank value to null before insert. The value is a label only; file mappings match on a statement’s signature, not on this column.
- `currency` is a three-letter ISO 4217 code, default `CAD`. A check constraint keeps lowercase codes and symbols out; the API validates against the supported list.

Deliberately left out:

- A stored balance. The account carries no balance of its own; the API derives balances from the account's imported statements.

## Security

Row Level Security is enabled with select, insert, update, and delete policies, each scoped to `(select auth.uid()) = user_id`. The API bypasses RLS, so it must filter by the authenticated user itself.

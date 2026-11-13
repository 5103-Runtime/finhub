# Transfers

`public.transfers` represents an internal movement of money between two accounts owned by the same user. Each transfer is paired with exactly two rows in `public.transactions`: one outgoing row from the source account and one incoming row in the destination account.

## Design

- **`user_id` references `auth.users`, while `from_account_id` and `to_account_id` reference `public.financial_accounts` through composite ownership foreign keys.** Both accounts must belong to the transfer owner, use the same currency, and be different accounts. An account with an existing transfer cannot be deleted until the transfer is deleted. Deleting the user cascades through the user's transfers.
- **`amount` is a positive `numeric(14, 2)`.** The linked source transaction stores `-amount`; the linked destination transaction stores `amount`.
- **`transfer_date` and `description` are copied to both ledger rows.** Descriptions must be trimmed and contain 1 to 500 characters.
- **`transactions.transfer_id` is nullable.** Null means the transaction is not associated with a transfer. Non-null values link the transaction to its parent transfer.
- **Categories remain transaction-specific.** The two transfer transactions may have different categories, including a category on only one side. Transfer rows are excluded from income and spending analytics by the API because they represent account-to-account movement.
- **Transfers are deleted as a unit.** Deleting a transfer cascades to both linked transactions. Individual transfer transactions must not be deleted because that would leave an incomplete ledger pair.
- **Transfer ledger integrity is deferred until the transaction completes.** This allows the transfer row and both existing transaction rows to be updated atomically, while requiring exactly one matching outgoing and incoming transaction at commit time. A regular transaction can be linked during transfer creation; an already-linked transaction cannot be reassigned or have its account changed.

Deliberately left out:

- Transfer creation, update, and deletion endpoints. The API owns the authenticated-user checks and the atomic operation that creates or deletes a transfer with its two transaction rows.
- Front-end transfer flows. The database supports the relationship and integrity rules; the user interface is implemented separately.

## Security

Row Level Security is enabled with select, insert, update, and delete policies. Every transfer policy requires `user_id` to match the authenticated user. Composite foreign keys verify that both referenced accounts belong to that user, and the currency-validation trigger verifies that the accounts use the same currency. Transaction policies verify that a linked transfer belongs to the authenticated user.

The API connects as `postgres` and therefore bypasses RLS. API endpoints must still validate the authenticated user before reading or changing transfers and their linked transactions.

## Integrity protections

Database constraints and triggers enforce:

- Positive transfer amounts.
- Distinct source and destination accounts.
- Matching user ownership and currency.
- Exactly two linked transaction rows.
- One negative source-account row.
- One positive destination-account row.
- Matching account IDs, dates, descriptions, and amounts.
- No direct changes to an already-linked transaction's account or transfer linkage. Date, description, and amount changes are allowed only when they match the updated parent transfer and both ledger rows are valid at commit time.

Category changes are intentionally allowed because categories belong to individual transactions rather than to the transfer itself.

## Verify The Migration

In the Supabase Dashboard:

1. Confirm `public.transfers` exists and `public.transactions.transfer_id` is present.
2. Confirm transfer indexes, RLS policies, constraints, and triggers exist.

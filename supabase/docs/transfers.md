# Transfers

`public.transfers` represents an internal movement of money between two accounts owned by the same user. Each transfer is paired with exactly two rows in `public.transactions`: one outgoing row from the source account and one incoming row in the destination account.

## Design

- **`user_id` references `auth.users`, while `from_account_id` and `to_account_id` reference `public.financial_accounts` through composite ownership foreign keys.** Both accounts must belong to the transfer owner, use the same currency, and be different accounts. An account with an existing transfer cannot be deleted until the transfer is deleted. Deleting the user cascades through the user's transfers.
- **`amount` is a positive `numeric(14, 2)`.** The linked source transaction stores `-amount`; the linked destination transaction stores `amount`.
- **`transfer_date` and `description` describe the transfer itself.** Linked ledger rows may have different dates and descriptions, each trimmed and 1 to 500 characters.
- **`transactions.transfer_id` is nullable.** Null means the transaction is not associated with a transfer. Non-null values link the transaction to its parent transfer.
- **Categories remain transaction-specific.** The two transfer transactions may have different categories, including a category on only one side. Transfer rows are excluded from income and spending analytics by the API because they represent account-to-account movement.
- **Transfers can be deleted or unlinked.** Deleting a transfer cascades to both linked transactions. Unlinking removes the relationship while preserving both transactions as regular ledger rows. Individual linked transactions must not be deleted because that would leave an incomplete ledger pair.
- **Transfer ledger integrity is deferred until the transaction completes.** This allows the transfer row and both existing transaction rows to be updated atomically, while requiring exactly one matching outgoing and incoming transaction at commit time. A regular transaction can be linked during transfer creation; an already-linked transaction cannot be reassigned or have its account changed. Editing a linked transaction changes only that ledger row's date, description, and category; changing its amount updates the transfer amount and paired ledger amount.

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
- Matching account IDs and amounts.
- No direct changes to an already-linked transaction's account or transfer linkage. Date and description changes are allowed independently on each ledger row. Amount changes must preserve one negative source row and one positive destination row with the transfer amount.

Category changes are intentionally allowed because categories belong to individual transactions rather than to the transfer itself.

## Verify The Migration

In the Supabase Dashboard:

1. Confirm `public.transfers` exists and `public.transactions.transfer_id` is present.
2. Confirm transfer indexes, RLS policies, constraints, and triggers exist.
3. Apply `_add_transfer_unlink_support` and `_allow_transfer_legs_different_details` after `_create_transfers` when upgrading an existing database.

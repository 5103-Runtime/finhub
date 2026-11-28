# Statement mappings

`public.statement_mappings` holds how each bank's CSV export maps onto ledger transactions. Mappings are shared by all users; the fields are described under [Statement import](../../api/docs/statement-import.md#mappings).

The API does not read this table yet. It still loads the JSON files under `api/FinHub.Api/Statements/Csv/Mappings/` (FIN-84).

## Design

- **One row per `bank` and `account_type`.** `bank` is a key of lowercase letters, digits, and underscores, such as `rbc`. `account_type` is `chequing`, `savings`, or `credit_card`, the same set `financial_accounts` allows.
- **One column per mapping field.** `match`, `description_columns`, `account_type_values`, and `import_statuses` are `text[]`. Nothing is stored as JSON.
- **A list has at least one entry, and no entry is null, empty, or starts or ends with a space, tab, or line break.** `is_statement_mapping_list()` checks this. The parser trims header cells and values before comparing them, so such an entry could never match.
- **No entry appears twice in a list.** The same function checks this. A repeated description column would double every description. The comparison is exact, so `Visa` and `visa` are different entries.
- **Every column a mapping reads is in `match`:** `date_column`, `amount_column`, each of `description_columns`, and the optional `account_type_column`, `account_column`, `balance_column`, and `status_column`. The comparison is exact, including case.
- **`account_type_column` and `account_type_values` are set together or are both null.** The same holds for `status_column` and `import_statuses`. An unused list is null, never empty.
- `date_format` is a .NET format string. The database only requires it to be non-empty, with no space, tab, or line break at either end.
- `invert` has no default. Every mapping states it.
- The migration inserts the five mappings: `rbc` chequing, savings, and credit card, and `scotiabank` chequing and credit card.

Deliberately left out:

- A check that two mappings cannot both describe one file. That rule compares pairs of rows and belongs to the API's mapping catalog.
- A check that `date_format` can read a full date. It is a .NET format, so the API checks it.
- A check for other whitespace at either end of an entry or of `date_format`, such as a no-break space. The parser trims everything .NET's `Trim()` does, so it is left to the API.

## Security

Row Level Security is enabled with no policies, so `anon` and `authenticated` can neither read nor write the table. The API bypasses RLS and is the only client, so it must restrict changes to administrators itself. `is_statement_mapping_list()` sets an empty `search_path`.

## Verify The Migration

In the Supabase Dashboard:

1. Open **Table Editor** and confirm `public.statement_mappings` exists with five rows.
2. Open the table policies and confirm Row Level Security is enabled with no policies.
3. Insert a second row with `bank = 'rbc'` and `account_type = 'chequing'` and confirm the insert is rejected.
4. Insert a row whose `bank` is `RBC`, then one whose `account_type` is `investment`, and confirm both inserts are rejected.
5. Insert a row whose `match` has a blank entry, one whose `description_columns` is empty, and one whose `description_columns` repeats a column, and confirm all three inserts are rejected.
6. Insert a row whose `date_column` is not in `match` and confirm the insert is rejected.
7. Insert a row with a `status_column` but no `import_statuses`, then one with `account_type_values` but no `account_type_column`, and confirm both inserts are rejected.
8. Update a row and confirm `updated_at` changes.

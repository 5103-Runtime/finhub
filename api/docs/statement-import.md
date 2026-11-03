# Statement import

FinHub parses bank statements in the API, not the browser. The user uploads a CSV export, the API picks a mapping from the header (and the account type column, where a bank shares one header across account types) and returns the lines as candidate transactions. Nothing is saved until the user has reviewed and confirmed them.

## Design

- **Parsing is server-side.** One parser, one set of tests, and the duplicate check (FIN-42) runs in the same place as the data.
- **Bank formats are data, not code.** Each export shape is a JSON mapping selected by its header. Supporting a new bank means adding a mapping and a sample file.
- **Parsing and saving are separate.** The parser produces candidates; saving goes through the existing transactions API after the user confirms, so the review page (FIN-6) and duplicate warning (FIN-8) apply to imports unchanged.
- **Sign convention is the ledger's.** Money in is positive regardless of how the bank writes it; `invert` in the mapping absorbs credit card exports that list charges as positive.
- **CSV only before sprint 5.** The parser sits behind an interface that takes a file stream, so another format is a new implementation, not a change to the endpoint.
- **Only posted rows are imported.** A pending charge can change amount or date, or disappear, before it posts; importing it would double-count once the posted line arrives. The next export carries the posted version, and any status the mapping does not list is skipped the same way.

## Endpoint

`POST /api/statements/parse` takes a `multipart/form-data` body with two fields: `file`, the export, and `accountId`, the account the statement belongs to. It returns the candidates and saves nothing:

```json
{
  "bank": "rbc",
  "accountType": "chequing",
  "skippedRows": 0,
  "transactions": [
    { "statementLine": 2, "transactionDate": "2026-10-01", "description": "DEPOSIT", "amount": 209.77, "balanceAfter": null, "potentialDuplicate": false }
  ]
}
```

`potentialDuplicate` is true when the account already holds a transaction with the same date, description, and amount, the same check `POST /api/transactions` makes before saving (FIN-42), so the review page can warn before the user confirms the line. The endpoint reports the detected bank and account type but does not compare them with the account, since a Scotiabank savings export reads as `chequing`; the review page can show them so a file dropped on the wrong account is caught before anything is saved. The account must belong to the user (404 `account_not_found`), the file must be a `.csv` (400 `unsupported_file_type`), and a request body over 1 MiB is refused with a 400 validation error before it is read; what the parser rejects is listed under [Errors](#errors).

## Not covered

- Requesting support for a new bank: on an unrecognized header, the UI could collect the bank name and a sample so a mapping can be added.
- Manual column mapping and Administrator editing of mappings (FIN-19).
- Guest preview (FIN-7).
- USD accounts. RBC's `USD$` column is ignored, so a row with no CAD amount is rejected.

## Sample files

[`test/fixtures/statements/`](../../test/fixtures/statements/) holds an export for every supported layout, with every value replaced but the bank's real header and formatting kept. Never commit a real statement.

| File | Shape |
|---|---|
| `rbc-chequing-statement.csv` | Header row, signed `CAD$` column, no balance |
| `rbc-credit-statement.csv` | Same header; charges negative, payments positive |
| `rbc-mastercard-statement.csv` | Same header; `Account Type` is `MasterCard` |
| `scotiabank-chequing-statement.csv` | Header row, signed `Amount`, running `Balance`, newest first |
| `scotiabank-credit-statement.csv` | Header row, charges positive, payments negative, no balance |

Savings exports share the chequing layout on both banks. RBC names the account type in each row, so its savings exports get the `savings` mapping; a Scotiabank savings export cannot be told from chequing and comes back as `chequing`. The Scotiabank exports put a banner in the `Filter` column of the first data row only; the parser ignores that column.

## Mappings

One JSON file per bank and account type, shipped inside the API. Adding a bank is a new file, not code. Mappings are checked when the API starts: unknown or duplicate keys, missing or null fields, a blank bank or account type, a date format that cannot read a full date, a column outside `match`, a blank or null list entry, an empty `descriptionColumns`, or two mappings whose match lists nest without an account type to tell them apart all fail, and the catalog test catches them in CI. A file whose columns satisfy two mappings at once is rejected as an unknown format.

| Field | Meaning |
|---|---|
| `match` | Header cells that must all be present to select this mapping. |
| `accountTypeColumn`, `accountTypeValues` | Optional. Distinguishes exports that share a header, such as RBC `Chequing`, `Savings`, `Visa`, and `MasterCard`. |
| `accountColumn` | Optional. Column identifying the account a row belongs to. A file that mixes accounts is rejected. |
| `dateColumn`, `dateFormat` | .NET format string, parsed with `InvariantCulture`. |
| `descriptionColumns` | Joined with one space, trimmed, flattened to one line, capped at 500. |
| `amountColumn` | Signed amount as the bank writes it. |
| `invert` | Flip every sign. Set for credit card exports that list charges as positive. |
| `balanceColumn` | Running balance column, or null. Follows the same sign convention as the amount after `invert`. |
| `statusColumn`, `importStatuses` | Optional. Rows whose status is not listed are skipped, so Scotiabank credit imports `posted` and drops `pending`; a row with no status is rejected. |

### RBC

```json
{
  "bank": "rbc", "accountType": "chequing",
  "match": ["Account Type", "Account Number", "Transaction Date", "Cheque Number", "Description 1", "Description 2", "CAD$", "USD$"],
  "accountTypeColumn": "Account Type", "accountTypeValues": ["Chequing"],
  "accountColumn": "Account Number",
  "dateColumn": "Transaction Date", "dateFormat": "M/d/yyyy",
  "descriptionColumns": ["Description 1", "Description 2"],
  "amountColumn": "CAD$", "invert": false, "balanceColumn": null
}
```

Savings uses the same header with `accountTypeValues: ["Savings"]`. Credit uses it with `["Visa", "MasterCard"]` and keeps `invert: false`, since RBC already writes charges negative.

Export one account at a time; an RBC download holding several accounts is rejected.

### Scotiabank

```json
{
  "bank": "scotiabank", "accountType": "chequing",
  "match": ["Filter", "Date", "Description", "Sub-description", "Type of Transaction", "Amount", "Balance"],
  "dateColumn": "Date", "dateFormat": "M/d/yyyy",
  "descriptionColumns": ["Description", "Sub-description"],
  "amountColumn": "Amount", "invert": false, "balanceColumn": "Balance"
}
```

```json
{
  "bank": "scotiabank", "accountType": "credit_card",
  "match": ["Filter", "Date", "Description", "Sub-description", "Status", "Type of Transaction", "Amount", "Associated card number"],
  "dateColumn": "Date", "dateFormat": "M/d/yyyy",
  "descriptionColumns": ["Description", "Sub-description"],
  "amountColumn": "Amount", "invert": true, "balanceColumn": null,
  "statusColumn": "Status", "importStatuses": ["posted"]
}
```

## Errors

The parser stops at the first problem and reports a stable code; the endpoint returns it as a 400 carrying `errorCode`, `error`, and `line`, null when none is known. Lines count from the first line of the file.

| Code | When | Line |
|---|---|---|
| `invalid_statement_file` | Empty, blank, or header only, not UTF-8, not well-formed CSV, more than 100,000 rows or 1,000,000 cells, or a mapped column appears twice in the header. | For malformed CSV, where the problem was found. |
| `unknown_statement_format` | No mapping matches the header, two do, or the first row names no supported account type. | None. |
| `invalid_statement_row` | A row has a different number of cells than the header, its date, amount, or description is missing or malformed, or its account type, account number, or status cell is blank. | The row. |
| `multiple_accounts` | A row belongs to a different account than the first row. | The row. |

A row whose status is not blank but not in `importStatuses` is skipped and counted in `skippedRows`; it is not an error.

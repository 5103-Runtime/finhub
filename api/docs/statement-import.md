# Statement import

FinHub parses bank statements in the API, not the browser. The user uploads a CSV export, the API picks a mapping from the header (and the account type column, where a bank shares one header across account types) and returns the lines as candidate transactions. Nothing is saved until the user has reviewed and confirmed them.

## Design

- **Parsing is server-side.** One parser, one set of tests, and the duplicate check (FIN-42) runs in the same place as the data.
- **Bank formats are data, not code.** Each export shape is a JSON mapping selected by its header. Supporting a new bank means adding a mapping and a sample file.
- **Parsing and saving are separate.** The parser produces candidates; saving goes through the existing transactions API after the user confirms, so the review page (FIN-6) and duplicate warning (FIN-8) apply to imports unchanged.
- **Sign convention is the ledger's.** Money in is positive regardless of how the bank writes it; `invert` in the mapping absorbs credit card exports that list charges as positive.
- **CSV only before sprint 5.** The parser sits behind an interface that takes a file stream, so another format is a new implementation, not a change to the endpoint.
- **Pending rows are skipped.** A pending charge can change amount or date, or disappear, before it posts; importing it would double-count once the posted line arrives. The next export carries the posted version.

## Not covered

- Requesting support for a new bank: on an unrecognized header, the UI could collect the bank name and a sample so a mapping can be added.
- Manual column mapping and Administrator editing of mappings (FIN-19).
- Guest preview (FIN-7).

## Sample files

[`test/fixtures/statements/`](../../test/fixtures/statements/) holds one export per bank and account type, with every value replaced but the bank's real header and formatting kept. Never commit a real statement.

| File | Shape |
|---|---|
| `rbc-chequing-statement.csv` | Header row, signed `CAD$` column, no balance |
| `rbc-credit-statement.csv` | Same header; charges negative, payments positive |
| `scotiabank-chequing-statement.csv` | Header row, signed `Amount`, running `Balance`, newest first |
| `scotiabank-credit-statement.csv` | Header row, charges positive, payments negative, no balance |

Savings exports share the chequing layout on both banks. The Scotiabank exports put a banner in the `Filter` column of the first data row only; the parser ignores that column.

## Mappings

One JSON file per bank and account type, shipped inside the API. Adding a bank is a new file, not code.

| Field | Meaning |
|---|---|
| `match` | Header cells that must all be present to select this mapping. |
| `accountTypeColumn`, `accountTypeValue` | Optional. Distinguishes exports that share a header, such as RBC `Chequing` vs `Visa`. |
| `dateColumn`, `dateFormat` | .NET format string, parsed with `InvariantCulture`. |
| `descriptionColumns` | Joined with one space, trimmed, capped at 500. |
| `amountColumn` | Signed amount as the bank writes it. |
| `invert` | Flip every sign. Set for credit card exports that list charges as positive. |
| `balanceColumn` | Running balance column, or null. Follows the same sign convention as the amount after `invert`. |
| `statusColumn`, `skipStatuses` | Optional. Rows whose status is listed are dropped, such as Scotiabank credit `pending`. |

```json
{
  "bank": "rbc", "accountType": "chequing",
  "match": ["Account Type", "Account Number", "Transaction Date", "Cheque Number", "Description 1", "Description 2", "CAD$", "USD$"],
  "accountTypeColumn": "Account Type", "accountTypeValue": "Chequing",
  "dateColumn": "Transaction Date", "dateFormat": "M/d/yyyy",
  "descriptionColumns": ["Description 1", "Description 2"],
  "amountColumn": "CAD$", "invert": false, "balanceColumn": null
}
```

RBC credit uses the same header with `accountTypeValue: "Visa"` and keeps `invert: false`, since RBC already writes charges negative.

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
  "statusColumn": "Status", "skipStatuses": ["pending"]
}
```

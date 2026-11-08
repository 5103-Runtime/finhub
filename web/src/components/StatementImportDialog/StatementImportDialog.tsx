import { useState, type CSSProperties } from "react";
import {
  Box,
  Button,
  Checkbox,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Typography,
} from "@mui/material";
import { DialogBase } from "@app/components/Dialog/DialogBase";
import { FormAlert } from "@app/components/Form/FormAlert";
import { CategoryOptionsList } from "@app/components/Category/CategoryMenuList";
import StatementUpload from "@app/components/StatementUpload";
import type { AccountResponse } from "@app/services/accountsApi";
import {
  importStatement,
  parseStatement,
  type ParsedStatementResponse,
  type ParsedTransactionResponse,
} from "@app/services/statementApi";
import { accountTypeLabels } from "@app/utils/accounts";
import { checkInstitution, getBankLabel } from "@app/utils/banks";
import { getErrorMessage } from "@app/utils/formErrors";
import {
  formatSignedCurrency,
  formatTransactionDate,
} from "@app/utils/transactions";
import type { CategoryOption } from "@app/components/Transactions/categoryTypes";
import "./StatementImportDialog.css";

type StatementImportDialogProps = {
  open: boolean;
  accounts: AccountResponse[];
  categoryOptions: CategoryOption[];
  defaultAccountId: string;
  onClose: () => void;
  onImported: (accountId: string, count: number) => void | Promise<void>;
};

type PreviewRow = ParsedTransactionResponse & {
  include: boolean;
  categoryId: string;
};

export default function StatementImportDialog({
  open,
  accounts,
  categoryOptions,
  defaultAccountId,
  onClose,
  onImported,
}: StatementImportDialogProps) {
  const [step, setStep] = useState<"upload" | "preview">("upload");
  const [accountId, setAccountId] = useState(defaultAccountId);
  const [file, setFile] = useState<File | null>(null);
  const [parsed, setParsed] = useState<ParsedStatementResponse | null>(null);
  const [rows, setRows] = useState<PreviewRow[]>([]);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");

  const fallbackAccountId =
    accounts.find((item) => item.id === defaultAccountId)?.id ??
    accounts[0]?.id ??
    "";
  const selectedAccountId = accounts.some((item) => item.id === accountId)
    ? accountId
    : fallbackAccountId;
  const account = accounts.find((item) => item.id === selectedAccountId);
  const selectedRows = rows.filter((row) => row.include);
  const duplicateCount = rows.filter((row) => row.potentialDuplicate).length;
  const allSelected = rows.length > 0 && selectedRows.length === rows.length;
  const columnLabel = (value: string) => value.replace("_", " ");

  const reset = () => {
    setStep("upload");
    setFile(null);
    setParsed(null);
    setRows([]);
    setError("");
    setAccountId(defaultAccountId);
  };

  const handleParse = async () => {
    if (!file || !selectedAccountId) {
      return;
    }

    setBusy(true);
    setError("");
    try {
      const result = await parseStatement(file, selectedAccountId);
      setParsed(result);
      setRows(
        result.transactions.map((transaction) => ({
          ...transaction,
          include: !transaction.potentialDuplicate,
          categoryId: "",
        })),
      );
      setStep("preview");
    } catch (requestError) {
      setError(getErrorMessage(requestError, "Unable to read the statement."));
    } finally {
      setBusy(false);
    }
  };

  const handleImport = async () => {
    if (selectedRows.length === 0 || !selectedAccountId) {
      return;
    }

    setBusy(true);
    setError("");
    try {
      await importStatement({
        accountId: selectedAccountId,
        transactions: selectedRows.map((row) => ({
          statementLine: row.statementLine,
          transactionDate: row.transactionDate,
          description: row.description,
          amount: row.amount,
          balanceAfter: row.balanceAfter,
          categoryId: row.categoryId || null,
          confirmPotentialDuplicate: row.potentialDuplicate,
        })),
      });
      await onImported(selectedAccountId, selectedRows.length);
      onClose();
    } catch (requestError) {
      setError(
        getErrorMessage(requestError, "Unable to import the transactions."),
      );
    } finally {
      setBusy(false);
    }
  };

  const updateRow = (statementLine: number, changes: Partial<PreviewRow>) =>
    setRows((current) =>
      current.map((row) =>
        row.statementLine === statementLine ? { ...row, ...changes } : row,
      ),
    );

  const toggleAll = () =>
    setRows((current) =>
      current.map((row) => ({ ...row, include: !allSelected })),
    );

  const backToUpload = () => {
    setStep("upload");
    setParsed(null);
    setRows([]);
    setError("");
  };

  const uploadActions = (
    <>
      <Button
        className="dialog-cancel-button"
        onClick={onClose}
        disabled={busy}
      >
        Close
      </Button>
      <Button
        className="dialog-primary-button"
        onClick={handleParse}
        disabled={!file || !selectedAccountId}
        loading={busy}
      >
        Preview
      </Button>
    </>
  );

  const previewActions = (
    <>
      <Button
        className="dialog-cancel-button"
        onClick={backToUpload}
        disabled={busy}
      >
        Back
      </Button>
      <Button
        className="dialog-primary-button"
        onClick={handleImport}
        disabled={selectedRows.length === 0}
        loading={busy}
      >
        Import {selectedRows.length}{" "}
        {selectedRows.length === 1 ? "transaction" : "transactions"}
      </Button>
    </>
  );

  const mismatch =
    parsed && account && parsed.accountType !== account.accountType;
  const institutionMismatch =
    parsed &&
    account &&
    checkInstitution(account.institution, parsed.bank) === "mismatch";

  return (
    <DialogBase
      open={open}
      title={step === "upload" ? "Import statement" : "Review transactions"}
      maxWidth={step === "upload" ? "sm" : "md"}
      busy={busy}
      onClose={onClose}
      onExited={reset}
      actions={step === "upload" ? uploadActions : previewActions}
    >
      {step === "upload" ? (
        <Box className="statement-import-upload">
          <FormControl fullWidth size="small">
            <InputLabel id="statement-import-account-label">Account</InputLabel>
            <Select
              labelId="statement-import-account-label"
              label="Account"
              value={selectedAccountId}
              onChange={(event) => setAccountId(event.target.value)}
              disabled={busy}
            >
              {accounts.map((item) => (
                <MenuItem key={item.id} value={item.id}>
                  {item.name}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
          <StatementUpload file={file} onFileChange={setFile} disabled={busy} />
          <FormAlert severity="error">{error}</FormAlert>
        </Box>
      ) : (
        <Box className="statement-preview">
          {parsed && (
            <Typography className="statement-preview-summary">
              Detected: {getBankLabel(parsed.bank)}{" "}
              {columnLabel(parsed.accountType)} export
              {account ? ` for ${account.name}` : ""}
              {account?.institution ? ` (${account.institution})` : ""}
            </Typography>
          )}
          <Typography className="statement-preview-summary">
            {rows.length} {rows.length === 1 ? "line" : "lines"} found in{" "}
            <strong>{file?.name}</strong>
            {parsed && parsed.skippedRows > 0
              ? `, ${parsed.skippedRows} pending ${
                  parsed.skippedRows === 1 ? "row" : "rows"
                } skipped`
              : ""}
            . Nothing is saved until you import.
          </Typography>

          {institutionMismatch && (
            <FormAlert severity="warning">
              This looks like a {getBankLabel(parsed.bank)} export, but{" "}
              {account.name} is saved under {account.institution}.
            </FormAlert>
          )}
          {mismatch && (
            <FormAlert severity="warning">
              This looks like a {columnLabel(parsed.accountType)} export, but{" "}
              {account.name} is a{" "}
              {accountTypeLabels[account.accountType].toLowerCase()} account.
            </FormAlert>
          )}
          {duplicateCount > 0 && (
            <FormAlert severity="warning">
              {duplicateCount}{" "}
              {duplicateCount === 1 ? "line may be" : "lines may be"} already in
              this account and {duplicateCount === 1 ? "is" : "are"} unchecked.
              Tick a line to import it anyway.
            </FormAlert>
          )}

          {rows.length === 0 ? (
            <Typography className="statement-preview-empty">
              This statement has no lines to import.
            </Typography>
          ) : (
            <Box className="statement-preview-table">
              <Box className="statement-preview-row statement-preview-head">
                <Checkbox
                  className="statement-preview-select-all-checkbox"
                  size="small"
                  checked={allSelected}
                  indeterminate={selectedRows.length > 0 && !allSelected}
                  onChange={toggleAll}
                  slotProps={{ input: { "aria-label": "Select all lines" } }}
                />
                <span className="statement-preview-select-all-label">
                  Select All
                </span>
                <span>Date</span>
                <span>Description</span>
                <span>Category</span>
                <span className="statement-preview-amount">Amount</span>
              </Box>
              {rows.map((row) => {
                const selectedCategory =
                  categoryOptions.find(
                    (option) => option.value === row.categoryId,
                  ) ?? categoryOptions[0];
                return (
                  <Box
                    key={row.statementLine}
                    className={`statement-preview-row${row.include ? "" : " excluded"}`}
                  >
                    <Checkbox
                      size="small"
                      checked={row.include}
                      onChange={(event) =>
                        updateRow(row.statementLine, {
                          include: event.target.checked,
                        })
                      }
                      slotProps={{
                        input: {
                          "aria-label": `Import ${row.description}`,
                        },
                      }}
                    />
                    <Typography className="statement-preview-date">
                      {formatTransactionDate(row.transactionDate)}
                    </Typography>
                    <Box className="statement-preview-description-cell">
                      <Typography
                        className="statement-preview-description"
                        title={row.description}
                      >
                        {row.description}
                      </Typography>
                      {row.potentialDuplicate && (
                        <Typography className="statement-preview-duplicate">
                          Possible duplicate
                        </Typography>
                      )}
                    </Box>
                    <Select
                      value={row.categoryId}
                      size="small"
                      displayEmpty
                      disabled={!row.include || busy}
                      aria-label={`Category for ${row.description}`}
                      className="ledger-category-form-select statement-preview-category"
                      onChange={(event) =>
                        updateRow(row.statementLine, {
                          categoryId: event.target.value,
                        })
                      }
                      renderValue={() => selectedCategory.label}
                      MenuProps={{
                        slots: { list: CategoryOptionsList },
                        slotProps: {
                          paper: { className: "ledger-category-menu" },
                        },
                      }}
                      style={
                        {
                          "--category-color": selectedCategory.color,
                        } as CSSProperties
                      }
                    >
                      {categoryOptions.map((option) => (
                        <MenuItem
                          key={option.value || "uncategorized"}
                          value={option.value}
                          className="ledger-category-option"
                          style={
                            {
                              "--category-color": option.color,
                            } as CSSProperties
                          }
                        >
                          {option.label}
                        </MenuItem>
                      ))}
                    </Select>
                    <Typography
                      className={`statement-preview-amount${row.amount > 0 ? " positive" : ""}`}
                    >
                      {formatSignedCurrency(
                        row.amount,
                        account?.currency ?? "CAD",
                      )}
                    </Typography>
                  </Box>
                );
              })}
            </Box>
          )}
          <FormAlert severity="error">{error}</FormAlert>
        </Box>
      )}
    </DialogBase>
  );
}

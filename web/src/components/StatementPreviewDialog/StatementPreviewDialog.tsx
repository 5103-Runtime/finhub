import { Box, Button, Typography } from "@mui/material";
import { Link } from "react-router-dom";
import { DialogBase } from "@app/components/Dialog/DialogBase";
import type { PreviewStatementResponse } from "@app/services/statementApi";
import { accountTypeLabels } from "@app/utils/accounts";
import { getBankLabel } from "@app/utils/banks";
import {
  formatSignedCurrency,
  formatTransactionDate,
} from "@app/utils/transactions";
import "./StatementPreviewDialog.css";

type StatementPreviewDialogProps = {
  preview: PreviewStatementResponse;
  onClose: () => void;
};

export default function StatementPreviewDialog({
  preview,
  onClose,
}: StatementPreviewDialogProps) {
  return (
    <DialogBase
      open
      title="Statement preview"
      maxWidth="md"
      onClose={onClose}
      actions={
        <>
          <Button className="dialog-cancel-button" onClick={onClose}>
            Close
          </Button>
          <Button
            component={Link}
            to="/register"
            className="dialog-primary-button"
          >
            Create an account
          </Button>
        </>
      }
    >
      <Box className="guest-statement-preview">
        <Typography className="guest-preview-summary">
          Detected: {getBankLabel(preview.bank)}{" "}
          {accountTypeLabels[preview.accountType]} export
        </Typography>
        <Typography className="guest-preview-summary">
          {preview.totalTransactions}{" "}
          {preview.totalTransactions === 1 ? "transaction" : "transactions"}{" "}
          found
          {preview.skippedRows > 0
            ? `; ${preview.skippedRows} pending ${
                preview.skippedRows === 1 ? "row" : "rows"
              } skipped`
            : ""}
          . Nothing is saved.
        </Typography>
        {preview.totalTransactions > preview.transactions.length && (
          <Typography className="guest-preview-summary">
            Showing the first {preview.transactions.length} transactions.
          </Typography>
        )}
        {preview.transactions.length === 0 ? (
          <Typography className="guest-preview-empty">
            This statement has no transactions to preview.
          </Typography>
        ) : (
          <Box className="guest-preview-table" role="table">
            <Box className="guest-preview-row guest-preview-head" role="row">
              <span role="columnheader">Date</span>
              <span role="columnheader">Description</span>
              <span role="columnheader" className="guest-preview-amount">
                Amount
              </span>
            </Box>
            {preview.transactions.map((transaction) => (
              <Box
                key={transaction.statementLine}
                className="guest-preview-row"
                role="row"
              >
                <Typography role="cell" className="guest-preview-date">
                  {formatTransactionDate(transaction.transactionDate)}
                </Typography>
                <Typography
                  role="cell"
                  className="guest-preview-description"
                  title={transaction.description}
                >
                  {transaction.description}
                </Typography>
                <Typography
                  role="cell"
                  className={`guest-preview-amount${transaction.amount > 0 ? " positive" : ""}`}
                >
                  {formatSignedCurrency(transaction.amount, "CAD")}
                </Typography>
              </Box>
            ))}
          </Box>
        )}
        <Typography className="guest-preview-privacy">
          Your uploaded statement is only used to create this preview. It is not
          stored.
        </Typography>
      </Box>
    </DialogBase>
  );
}

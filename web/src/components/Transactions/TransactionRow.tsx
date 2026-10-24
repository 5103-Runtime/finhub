import { useState } from "react";
import { Box, IconButton, Typography } from "@mui/material";
import CheckRounded from "@mui/icons-material/CheckRounded";
import CloseRounded from "@mui/icons-material/CloseRounded";
import EditRounded from "@mui/icons-material/EditRounded";
import { useForm, type SubmitHandler } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { TextFormField } from "@app/components/Form";
import { Form } from "@app/components/Form/Form";
import { FormAlert } from "@app/components/Form/FormAlert";
import type { AccountResponse } from "@app/services/accountsApi";
import type { TransactionResponse } from "@app/services/transactionApi";
import {
  transactionEditSchema,
  type TransactionEditFormData,
} from "@app/validators/transactionSchemas";
import {
  formatSignedCurrency,
  formatTransactionDate,
} from "@app/utils/transactions";
import { applyFieldErrors, getErrorMessage } from "@app/utils/formErrors";

type TransactionRowProps = {
  transaction: TransactionResponse;
  account: AccountResponse | undefined;
  onEdit: () => void;
};

/**
/**
 * @brief Read-only ledger row with an edit button.
 */
 */
export function TransactionRow({
  transaction,
  account,
  onEdit,
}: TransactionRowProps) {
  const amountClass =
    transaction.amount > 0
      ? "transaction-amount positive"
      : "transaction-amount";

  return (
    <Box className="transaction-row">
      <Typography className="transaction-date">
        {formatTransactionDate(transaction.transactionDate)}
      </Typography>
      <Typography
        className="transaction-description"
        title={transaction.description}
      >
        {transaction.description}
      </Typography>
      <Typography className="transaction-account">
        {account?.name ?? "Unknown account"}
      </Typography>
      <Typography className={amountClass}>
        {formatSignedCurrency(transaction.amount, account?.currency ?? "CAD")}
      </Typography>
      <Box className="transaction-actions">
        <IconButton
          aria-label={`Edit ${transaction.description}`}
          className="transaction-icon-button"
          onClick={onEdit}
        >
          <EditRounded />
        </IconButton>
      </Box>
    </Box>
  );
}

type TransactionEditRowProps = {
  transaction: TransactionResponse;
  account: AccountResponse | undefined;
  onCancel: () => void;
  onSave: (data: TransactionEditFormData) => Promise<void>;
};

/**
/**
 * @brief Ledger row turned into a form. Enter or the check button saves,
 * Escape or the close button cancels. It is only mounted while editing,
 * so its form always starts from the transaction's current values.
 */
 * Escape or the close button cancels. It is only mounted while editing,
 * so its form always starts from the transaction's current values.
 */
export function TransactionEditRow({
  transaction,
  account,
  onCancel,
  onSave,
}: TransactionEditRowProps) {
  const [serverError, setServerError] = useState("");
  const form = useForm<TransactionEditFormData>({
    resolver: zodResolver(transactionEditSchema),
    defaultValues: {
      transactionDate: transaction.transactionDate,
      description: transaction.description,
      amount: transaction.amount.toFixed(2),
    },
    mode: "onTouched",
  });
  const { isSubmitting } = form.formState;

  const onSubmit: SubmitHandler<TransactionEditFormData> = async (data) => {
    setServerError("");
    try {
      await onSave(data);
    } catch (requestError) {
      applyFieldErrors(requestError, form.setError, [
        "transactionDate",
        "description",
        "amount",
      ]);
      setServerError(
        getErrorMessage(requestError, "Unable to save the transaction."),
      );
    }
  };

  return (
    <Form
      form={form}
      onSubmit={onSubmit}
      className="transaction-row transaction-edit-form"
      onKeyDown={(event) => {
        if (event.key === "Escape") {
          onCancel();
        }
      }}
    >
      <TextFormField<TransactionEditFormData>
        name="transactionDate"
        type="date"
        size="small"
        slotProps={{ htmlInput: { "aria-label": "Date" } }}
      />
      <TextFormField<TransactionEditFormData>
        name="description"
        size="small"
        fullWidth
        autoFocus
        slotProps={{
          htmlInput: { "aria-label": "Description", maxLength: 500 },
        }}
      />
      <Typography className="transaction-account">
        {account?.name ?? "Unknown account"}
      </Typography>
      <TextFormField<TransactionEditFormData>
        name="amount"
        size="small"
        fullWidth
        slotProps={{
          htmlInput: { "aria-label": "Amount", inputMode: "decimal" },
        }}
      />
      <Box className="transaction-actions">
        <IconButton
          type="submit"
          aria-label="Save changes"
          className="transaction-icon-button"
          disabled={isSubmitting}
        >
          <CheckRounded />
        </IconButton>
        <IconButton
          aria-label="Cancel editing"
          className="transaction-icon-button"
          onClick={onCancel}
          disabled={isSubmitting}
        >
          <CloseRounded />
        </IconButton>
      </Box>
      {serverError && (
        <Box className="transaction-row-error">
          <FormAlert severity="error">{serverError}</FormAlert>
        </Box>
      )}
    </Form>
  );
}

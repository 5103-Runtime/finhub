import { useState } from "react";
import { Box, IconButton, Typography } from "@mui/material";
import CheckRounded from "@mui/icons-material/CheckRounded";
import CloseRounded from "@mui/icons-material/CloseRounded";
import DeleteOutlineRounded from "@mui/icons-material/DeleteOutlineRounded";
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
import { ApiError } from "@app/services/apiClient";

type TransactionRowProps = {
  transaction: TransactionResponse;
  account: AccountResponse;
  onEdit: () => void;
  onDelete: () => void;
  deleting?: boolean;
  successMessage?: string;
  potentialDuplicate?: boolean;
};

/**
 * @brief Read-only ledger row with an edit button.
 */
export function TransactionRow({
  transaction,
  account,
  onEdit,
  onDelete,
  deleting = false,
  successMessage,
  potentialDuplicate,
}: TransactionRowProps) {
  const amountClass =
    transaction.amount > 0 ? "ledger-amount positive" : "ledger-amount";

  return (
    <>
      <Box className="ledger-row">
        
        <Typography className="ledger-date">
          {formatTransactionDate(transaction.transactionDate)}
        </Typography>
        <Typography
          className="ledger-description"
          title={transaction.description}
        >
          {transaction.description}
        </Typography>
        
        <Typography className={amountClass}>
          {formatSignedCurrency(transaction.amount, account.currency)}
        </Typography>
        <Box className="ledger-actions">
          <IconButton
            aria-label={`Edit ${transaction.description}`}
            className="ledger-icon-button"
            onClick={onEdit}
            disabled={deleting}
          >
            <EditRounded />
          </IconButton>
          <IconButton
            aria-label={`Delete ${transaction.description}`}
            className="ledger-icon-button ledger-delete"
            onClick={onDelete}
            disabled={deleting}
          >
            <DeleteOutlineRounded />
          </IconButton>
        </Box>
      </Box>
      <Box className="ledger-row-feedback">
        {potentialDuplicate && (
          <FormAlert severity="error">
            This transaction may be a duplicate of an existing transaction.
            Please review your transactions.
          </FormAlert>
        )}
        {successMessage && (
          <FormAlert severity="success">
            {successMessage}
          </FormAlert>
        )}
      </Box>
    </>
  );
}

type TransactionEditRowProps = {
  transaction: TransactionResponse;
  onCancel: () => void;
  onSave: (data: TransactionEditFormData, confirmPotentialDuplicate: boolean) => Promise<void>;
};

/**
 * @brief Ledger row turned into a form. Enter or the check button saves,
 * Escape or the close button cancels. It is only mounted while editing,
 * so its form always starts from the transaction's current values.
 */
export function TransactionEditRow({
  transaction,
  onCancel,
  onSave,
}: TransactionEditRowProps) {
  const [serverError, setServerError] = useState("");
  const [confirmPotentialDuplicate, setConfirmPotentialDuplicate] = useState(false);
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
      await onSave(data, confirmPotentialDuplicate);
    } catch (requestError) {
      if(requestError instanceof ApiError && requestError.errorCode === "potential_duplicate"){
        setServerError("This transaction might be a duplicate. Submit again if you still wish to add transaction.");
        setConfirmPotentialDuplicate(true);
        return;
        }
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
      className="ledger-row ledger-edit-form"
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
      <TextFormField<TransactionEditFormData>
        name="amount"
        size="small"
        fullWidth
        slotProps={{
          htmlInput: { "aria-label": "Amount", inputMode: "decimal" },
        }}
      />
      <Box className="ledger-actions">
        <IconButton
          type="submit"
          aria-label="Save changes"
          className="ledger-icon-button ledger-save"
          disabled={isSubmitting}
        >
          <CheckRounded />
        </IconButton>
        <IconButton
          aria-label="Cancel editing"
          className="ledger-icon-button ledger-cancel"
          onClick={onCancel}
          disabled={isSubmitting}
        >
          <CloseRounded />
        </IconButton>
      </Box>
      {serverError && (
        <Box className="ledger-row-error">
          <FormAlert severity="error">{serverError}</FormAlert>
        </Box>
      )}
    </Form>
  );
}

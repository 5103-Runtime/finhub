import { Box } from "@mui/material";
import type { SubmitHandler, UseFormReturn } from "react-hook-form";
import { FormDialog } from "@app/components/Dialog";
import { SelectFormField, TextFormField } from "@app/components/Form";
import type { TransactionResponse } from "@app/services/transactionApi";
import type { SelectOption } from "@app/components/Form/SelectFormField";
import type { TransferFormData } from "@app/validators/transferSchemas";

type TransactionEntryDialogProps = {
  open: boolean;
  form: UseFormReturn<TransferFormData>;
  isTransferMode: boolean;
  accountOptions: SelectOption[];
  categoryOptions: SelectOption[];
  sourceTransactionOptions: SelectOption[];
  destinationTransactionOptions: SelectOption[];
  selectedExistingTransaction?: TransactionResponse;
  error: string;
  isDuplicate: boolean;
  onSubmit: SubmitHandler<TransferFormData>;
  onClose: () => void;
};

export function TransactionEntryDialog({
  open,
  form,
  isTransferMode,
  accountOptions,
  categoryOptions,
  sourceTransactionOptions,
  destinationTransactionOptions,
  selectedExistingTransaction,
  error,
  isDuplicate,
  onSubmit,
  onClose,
}: TransactionEntryDialogProps) {
  return (
    <FormDialog
      open={open}
      title={isTransferMode ? "Add a transfer" : "Add a transaction"}
      form={form}
      onSubmit={onSubmit}
      onClose={onClose}
      onEntered={() => {
        form.setFocus("description");
        form.clearErrors("description");
      }}
      submitLabel={isTransferMode ? "Add transfer" : "Add transaction"}
      error={error}
      errorSeverity={isDuplicate ? "warning" : "error"}
    >
      <Box className="ledger-transaction-form-grid">
        <SelectFormField<TransferFormData>
          name="fromAccountId"
          label="From account (optional)"
          options={[
            { value: "", label: "Regular transaction" },
            ...accountOptions,
          ]}
          displayEmpty
        />
        <SelectFormField<TransferFormData>
          name="toAccountId"
          label="To account"
          options={accountOptions}
        />
        <Box className="ledger-transaction-form-row ledger-date-description-row">
          <TextFormField<TransferFormData>
            name="transferDate"
            label="Date"
            type="date"
            required
            fullWidth
            slotProps={{ inputLabel: { shrink: true } }}
          />
          <TextFormField<TransferFormData>
            name="description"
            label="Description"
            required
            fullWidth
            slotProps={{ htmlInput: { maxLength: 500 } }}
          />
        </Box>
        <Box className="ledger-transaction-form-row ledger-category-amount-row">
          <SelectFormField<TransferFormData>
            name="categoryId"
            label="Category (optional)"
            options={categoryOptions}
            selectClassName="ledger-category-form-select"
            displayEmpty
          />
          <TextFormField<TransferFormData>
            name="amount"
            label="Amount"
            required
            disabled={Boolean(selectedExistingTransaction)}
            fullWidth
            helperText={
              selectedExistingTransaction
                ? "Amount must match the selected transaction."
                : undefined
            }
            slotProps={{ htmlInput: { inputMode: "decimal" } }}
          />
        </Box>
        {isTransferMode && (
          <>
            <SelectFormField<TransferFormData>
              name="fromTransactionId"
              label="Existing source transaction (optional)"
              options={[
                { value: "", label: "Create a new source transaction" },
                ...sourceTransactionOptions,
              ]}
              displayEmpty
            />
            <SelectFormField<TransferFormData>
              name="toTransactionId"
              label="Existing destination transaction (optional)"
              options={[
                { value: "", label: "Create a new destination transaction" },
                ...destinationTransactionOptions,
              ]}
              displayEmpty
            />
          </>
        )}
      </Box>
    </FormDialog>
  );
}

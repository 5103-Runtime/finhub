import { Box } from "@mui/material";
import type { SubmitHandler, UseFormReturn } from "react-hook-form";
import { FormDialog } from "@app/components/Dialog";
import { SelectFormField, TextFormField } from "@app/components/Form";
import type { SelectOption } from "@app/components/Form/SelectFormField";
import type { TransactionFormData } from "@app/validators/transactionSchemas";

type TransactionEntryDialogProps = {
  open: boolean;
  form: UseFormReturn<TransactionFormData>;
  accountOptions: SelectOption[];
  categoryOptions: SelectOption[];
  error: string;
  isDuplicate: boolean;
  onSubmit: SubmitHandler<TransactionFormData>;
  onClose: () => void;
};

export function TransactionEntryDialog({
  open,
  form,
  accountOptions,
  categoryOptions,
  error,
  isDuplicate,
  onSubmit,
  onClose,
}: TransactionEntryDialogProps) {
  return (
    <FormDialog
      open={open}
      title="Add a transaction"
      form={form}
      onSubmit={onSubmit}
      onClose={onClose}
      onEntered={() => {
        form.setFocus("description");
        form.clearErrors("description");
      }}
      submitLabel="Add transaction"
      error={error}
      errorSeverity={isDuplicate ? "warning" : "error"}
    >
      <Box className="ledger-transaction-form-grid">
        <SelectFormField<TransactionFormData>
          name="accountId"
          label="Account"
          options={accountOptions}
        />
        <Box className="ledger-transaction-form-row ledger-date-description-row">
          <TextFormField<TransactionFormData>
            name="transactionDate"
            label="Date"
            type="date"
            required
            fullWidth
            slotProps={{ inputLabel: { shrink: true } }}
          />
          <TextFormField<TransactionFormData>
            name="description"
            label="Description"
            required
            fullWidth
            slotProps={{ htmlInput: { maxLength: 500 } }}
          />
        </Box>
        <Box className="ledger-transaction-form-row ledger-category-amount-row">
          <SelectFormField<TransactionFormData>
            name="categoryId"
            label="Category (optional)"
            options={categoryOptions}
            selectClassName="ledger-category-form-select"
            displayEmpty
            helperText=" " // Need to align category selector vertically with the amount field
          />
          <TextFormField<TransactionFormData>
            name="amount"
            label="Amount"
            required
            fullWidth
            helperText="Use a negative amount for money out"
            slotProps={{ htmlInput: { inputMode: "decimal" } }}
          />
        </Box>
      </Box>
    </FormDialog>
  );
}

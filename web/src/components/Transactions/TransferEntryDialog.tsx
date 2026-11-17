import {
  Box,
  FormControl,
  FormControlLabel,
  FormLabel,
  FormHelperText,
  Radio,
  RadioGroup,
} from "@mui/material";
import { useEffect, useRef } from "react";
import {
  Controller,
  useWatch,
  type SubmitHandler,
  type UseFormReturn,
} from "react-hook-form";
import { FormDialog } from "@app/components/Dialog";
import { SelectFormField, TextFormField } from "@app/components/Form";
import type { TransactionResponse } from "@app/services/transactionApi";
import type { SelectOption } from "@app/components/Form/SelectFormField";
import { getTodayDate } from "@app/utils/transactions";
import type { TransferFormData } from "@app/validators/transferSchemas";

type TransferEntryDialogProps = {
  open: boolean;
  form: UseFormReturn<TransferFormData>;
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

export function TransferEntryDialog({
  open,
  form,
  accountOptions,
  categoryOptions,
  sourceTransactionOptions,
  destinationTransactionOptions,
  selectedExistingTransaction,
  error,
  isDuplicate,
  onSubmit,
  onClose,
}: TransferEntryDialogProps) {
  const hadSelectedTransaction = useRef(false);
  const previousAccountIds = useRef({
    from: "",
    to: "",
  });
  const transferMode = useWatch({
    control: form.control,
    name: "transferMode",
  });
  const fromAccountId = useWatch({
    control: form.control,
    name: "fromAccountId",
  });
  const toAccountId = useWatch({
    control: form.control,
    name: "toAccountId",
  });
  const fromTransactionId = useWatch({
    control: form.control,
    name: "fromTransactionId",
  });
  const toTransactionId = useWatch({
    control: form.control,
    name: "toTransactionId",
  });
  const canChooseTransactionHandling = Boolean(fromAccountId && toAccountId);

  useEffect(() => {
    const previous = previousAccountIds.current;
    const fromAccountChanged =
      previous.from !== "" && previous.from !== fromAccountId;
    const toAccountChanged = previous.to !== "" && previous.to !== toAccountId;

    if (fromAccountChanged) {
      form.setValue("fromTransactionId", "");
    }
    if (toAccountChanged) {
      form.setValue("toTransactionId", "");
    }

    previousAccountIds.current = {
      from: fromAccountId,
      to: toAccountId,
    };
  }, [form, fromAccountId, toAccountId]);

  useEffect(() => {
    const hasSelectedTransaction = Boolean(
      fromTransactionId || toTransactionId,
    );

    if (hasSelectedTransaction) {
      hadSelectedTransaction.current = true;
      return;
    }

    if (!hadSelectedTransaction.current) {
      return;
    }

    form.setValue("transferDate", getTodayDate(), {
      shouldValidate: false,
    });
    form.setValue("description", "");
    form.setValue("categoryId", "");
    form.setValue("amount", "");
    form.clearErrors([
      "transferDate",
      "description",
      "categoryId",
      "amount",
      "fromTransactionId",
      "toTransactionId",
    ]);
    hadSelectedTransaction.current = false;
  }, [form, fromTransactionId, toTransactionId]);

  useEffect(() => {
    if (canChooseTransactionHandling || transferMode === "new") {
      return;
    }

    form.setValue("transferMode", "new");
    form.setValue("fromTransactionId", "");
    form.setValue("toTransactionId", "");
    form.clearErrors(["fromTransactionId", "toTransactionId"]);
  }, [canChooseTransactionHandling, form, transferMode]);

  useEffect(() => {
    if (fromTransactionId || toTransactionId) {
      form.clearErrors(["fromTransactionId", "toTransactionId"]);
    }
  }, [fromTransactionId, form, toTransactionId]);

  return (
    <FormDialog
      open={open}
      title="Add a transfer"
      form={form}
      onSubmit={onSubmit}
      onClose={onClose}
      onEntered={() => {
        form.setFocus("fromAccountId");
        form.clearErrors("fromAccountId");
      }}
      submitLabel="Add transfer"
      error={error}
      errorSeverity={isDuplicate ? "warning" : "error"}
    >
      <Box className="ledger-transaction-form-grid">
        <Box className="ledger-transaction-form-row ledger-account-row">
          <SelectFormField<TransferFormData>
            name="fromAccountId"
            label="From account"
            options={accountOptions}
            placeholder="From account"
            required
          />
          <SelectFormField<TransferFormData>
            name="toAccountId"
            label="To account"
            options={accountOptions}
            placeholder="To account"
            required
          />
        </Box>
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
                : "Use a positive amount for transfers."
            }
            slotProps={{ htmlInput: { inputMode: "decimal" } }}
          />
        </Box>
        {canChooseTransactionHandling && (
          <>
            <FormControl className="ledger-transfer-mode">
              <FormLabel id="transfer-mode-label">
                Transaction handling
              </FormLabel>
              <Controller
                name="transferMode"
                control={form.control}
                render={({ field }) => (
                  <RadioGroup
                    {...field}
                    aria-labelledby="transfer-mode-label"
                    row
                    onChange={(event) => {
                      const mode = event.target
                        .value as TransferFormData["transferMode"];
                      form.setValue("transferMode", mode, {
                        shouldDirty: true,
                        shouldValidate: false,
                      });
                      if (mode === "new") {
                        form.setValue("fromTransactionId", "");
                        form.setValue("toTransactionId", "");
                        form.clearErrors([
                          "transferMode",
                          "fromTransactionId",
                          "toTransactionId",
                        ]);
                      }
                    }}
                  >
                    <FormControlLabel
                      value="new"
                      control={<Radio />}
                      label="Create new transfer"
                    />
                    <FormControlLabel
                      value="existing"
                      control={<Radio />}
                      label="Link existing transactions"
                    />
                  </RadioGroup>
                )}
              />
              {transferMode === "existing" && (
                <FormHelperText>
                  Choose at least one existing transaction.
                </FormHelperText>
              )}
            </FormControl>
            {transferMode === "existing" && (
              <>
                <SelectFormField<TransferFormData>
                  name="fromTransactionId"
                  label="Source transaction"
                  options={[
                    { value: "", label: "New source transaction" },
                    ...sourceTransactionOptions,
                  ]}
                  placeholder="Source transaction"
                />
                <SelectFormField<TransferFormData>
                  name="toTransactionId"
                  label="Destination transaction"
                  options={[
                    { value: "", label: "New destination transaction" },
                    ...destinationTransactionOptions,
                  ]}
                  placeholder="Destination transaction"
                />
              </>
            )}
          </>
        )}
      </Box>
    </FormDialog>
  );
}

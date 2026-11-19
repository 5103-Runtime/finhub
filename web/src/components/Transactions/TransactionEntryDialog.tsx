import { useEffect, useRef, useState } from "react";
import { Box } from "@mui/material";
import {
  useWatch,
  type SubmitHandler,
  type UseFormReturn,
} from "react-hook-form";
import { FormDialog } from "@app/components/Dialog";
import { SelectFormField, TextFormField } from "@app/components/Form";
import type { SelectOption } from "@app/components/Form/SelectFormField";
import type { TransactionFormData } from "@app/validators/transactionSchemas";
import { previewTransactionCategorization } from "@app/services/transactionApi";

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
  const accountId = useWatch({ control: form.control, name: "accountId" });
  const categoryId = useWatch({ control: form.control, name: "categoryId" });
  const description = useWatch({ control: form.control, name: "description" });
  const amount = useWatch({ control: form.control, name: "amount" });
  const [automaticCategoryId, setAutomaticCategoryId] = useState("");
  const overrideSnapshotRef = useRef("");

  useEffect(() => {
    if (!open) {
      return;
    }

    if (overrideSnapshotRef.current) {
      const isUnchanged =
        `${accountId}|${description.trim()}|${amount.trim()}` ===
        overrideSnapshotRef.current;
      if (isUnchanged) {
        return;
      }
      overrideSnapshotRef.current = "";
      form.setValue("categoryWasExplicitlySet", false);
    }

    if (categoryId && categoryId !== automaticCategoryId) {
      return;
    }

    if (
      !accountId ||
      !description.trim() ||
      !/^-?\d+(\.\d{1,2})?$/.test(amount.trim()) ||
      !Number.isFinite(Number(amount))
    ) {
      if (!automaticCategoryId) {
        return;
      }

      const timer = window.setTimeout(() => {
        if (
          form.getValues("accountId") === accountId &&
          form.getValues("categoryId") === automaticCategoryId &&
          form.getValues("description").trim() === description.trim() &&
          form.getValues("amount").trim() === amount.trim()
        ) {
          form.setValue("categoryId", "", {
            shouldDirty: true,
            shouldValidate: true,
          });
          setAutomaticCategoryId("");
        }
      }, 350);

      return () => window.clearTimeout(timer);
    }

    const timer = window.setTimeout(async () => {
      try {
        const preview = await previewTransactionCategorization({
          accountId,
          categoryId: null,
          transactionDate: form.getValues("transactionDate"),
          description: description.trim(),
          amount: Number(amount),
          confirmPotentialDuplicate: false,
        });
        if (!preview.categoryId || !preview.automaticCategoryApplied) {
          const currentCategoryId = form.getValues("categoryId");
          const currentAccountId = form.getValues("accountId");
          const currentDescription = form.getValues("description").trim();
          const currentAmount = form.getValues("amount").trim();
          if (
            (currentCategoryId && currentCategoryId !== automaticCategoryId) ||
            currentAccountId !== accountId ||
            currentDescription !== description.trim() ||
            currentAmount !== amount.trim()
          ) {
            return;
          }

          if (currentCategoryId === automaticCategoryId) {
            form.setValue("categoryId", "", {
              shouldDirty: true,
              shouldValidate: true,
            });
          }
          setAutomaticCategoryId("");
          return;
        }

        const currentCategoryId = form.getValues("categoryId");
        const currentAccountId = form.getValues("accountId");
        const currentDescription = form.getValues("description").trim();
        const currentAmount = form.getValues("amount").trim();
        if (
          (currentCategoryId && currentCategoryId !== automaticCategoryId) ||
          currentAccountId !== accountId ||
          currentDescription !== description.trim() ||
          currentAmount !== amount.trim()
        ) {
          return;
        }

        setAutomaticCategoryId(preview.categoryId);
        form.setValue("categoryId", preview.categoryId, {
          shouldDirty: true,
          shouldValidate: true,
        });
        form.setValue("categoryWasExplicitlySet", false);
      } catch {
        // The create request will surface validation or API errors normally.
      }
    }, 350);

    return () => window.clearTimeout(timer);
  }, [
    accountId,
    amount,
    automaticCategoryId,
    categoryId,
    description,
    form,
    open,
  ]);

  return (
    <FormDialog
      open={open}
      title="Add a transaction"
      form={form}
      onSubmit={onSubmit}
      onClose={onClose}
      onEntered={() => {
        setAutomaticCategoryId("");
        overrideSnapshotRef.current = "";
        form.setValue("categoryWasExplicitlySet", false);
        form.setFocus("amount");
        form.clearErrors("amount");
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
            name="amount"
            label="Amount"
            required
            fullWidth
            helperText="Use a negative amount for money out"
            slotProps={{ htmlInput: { inputMode: "decimal" } }}
          />
        </Box>
        <Box className="ledger-transaction-form-row ledger-date-description-row ledger-category-description-row">
          <SelectFormField<TransactionFormData>
            name="categoryId"
            label="Category (optional)"
            options={categoryOptions}
            selectClassName="ledger-category-form-select"
            displayEmpty
            onValueChange={(value) => {
              if (value === "") {
                overrideSnapshotRef.current = `${accountId}|${description.trim()}|${amount.trim()}`;
                form.setValue("categoryWasExplicitlySet", true);
              } else {
                overrideSnapshotRef.current = "";
                form.setValue(
                  "categoryWasExplicitlySet",
                  value !== automaticCategoryId,
                );
              }
              if (value !== automaticCategoryId) {
                setAutomaticCategoryId("");
              }
            }}
            helperText={
              automaticCategoryId !== "" && categoryId === automaticCategoryId
                ? "Automatically applied"
                : undefined
            }
          />
          <TextFormField<TransactionFormData>
            name="description"
            label="Description"
            required
            fullWidth
            slotProps={{ htmlInput: { maxLength: 500 } }}
          />
        </Box>
      </Box>
    </FormDialog>
  );
}

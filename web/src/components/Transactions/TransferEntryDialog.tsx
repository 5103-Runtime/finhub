import {
  Box,
  FormControl,
  FormControlLabel,
  FormLabel,
  FormHelperText,
  Radio,
  RadioGroup,
} from "@mui/material";
import { useEffect, useRef, useState } from "react";
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
import { previewTransferCategorization } from "@app/services/transferApi";

type TransferEntryDialogProps = {
  open: boolean;
  form: UseFormReturn<TransferFormData>;
  accountOptions: SelectOption[];
  categoryOptions: SelectOption[];
  sourceTransactionOptions: SelectOption[];
  destinationTransactionOptions: SelectOption[];
  selectedExistingTransaction?: TransactionResponse;
  sourceExistingTransaction?: TransactionResponse;
  destinationExistingTransaction?: TransactionResponse;
  error: string;
  isDuplicate: boolean;
  onSubmit: SubmitHandler<TransferFormData>;
  onClose: () => void;
};

function getTransferCategorySnapshot(
  fromAccountId: string,
  toAccountId: string,
  transferDate: string,
  description: string,
  amount: string,
  fromTransactionId: string,
  toTransactionId: string,
): string {
  return [
    fromAccountId,
    toAccountId,
    transferDate,
    description.trim(),
    amount.trim(),
    fromTransactionId,
    toTransactionId,
  ].join("|");
}

export function TransferEntryDialog({
  open,
  form,
  accountOptions,
  categoryOptions,
  sourceTransactionOptions,
  destinationTransactionOptions,
  selectedExistingTransaction,
  sourceExistingTransaction,
  destinationExistingTransaction,
  error,
  isDuplicate,
  onSubmit,
  onClose,
}: TransferEntryDialogProps) {
  const hadSelectedTransaction = useRef(false);
  const sourceCategoryOverrideSnapshotRef = useRef("");
  const destinationCategoryOverrideSnapshotRef = useRef("");
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
  const description = useWatch({
    control: form.control,
    name: "description",
  });
  const transferDate = useWatch({
    control: form.control,
    name: "transferDate",
  });
  const amount = useWatch({
    control: form.control,
    name: "amount",
  });
  const sourceCategoryId = useWatch({
    control: form.control,
    name: "sourceCategoryId",
  });
  const destinationCategoryId = useWatch({
    control: form.control,
    name: "destinationCategoryId",
  });
  const [automaticSourceCategoryId, setAutomaticSourceCategoryId] =
    useState("");
  const [automaticDestinationCategoryId, setAutomaticDestinationCategoryId] =
    useState("");
  const accountSelectionIsValid =
    Boolean(fromAccountId && toAccountId) &&
    fromAccountId !== toAccountId &&
    !form.formState.errors.fromAccountId &&
    !form.formState.errors.toAccountId;
  const canChooseTransactionHandling = accountSelectionIsValid;

  // Clear the from/to transaction IDs when the from/to account IDs change
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

  // Validate that the from and to accounts are different
  useEffect(() => {
    if (fromAccountId && toAccountId && fromAccountId === toAccountId) {
      form.setError("fromAccountId", {
        type: "manual",
        message: "Accounts must be different.",
      });
      form.setError("toAccountId", {
        type: "manual",
        message: "Accounts must be different.",
      });
      return;
    }

    if (
      form.formState.errors.fromAccountId?.message ===
      "Accounts must be different."
    ) {
      form.clearErrors("fromAccountId");
    }
    if (
      form.formState.errors.toAccountId?.message ===
      "Accounts must be different."
    ) {
      form.clearErrors("toAccountId");
    }
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
    form.setValue("sourceCategoryId", "");
    form.setValue("destinationCategoryId", "");
    form.setValue("sourceCategoryWasExplicitlySet", false);
    form.setValue("destinationCategoryWasExplicitlySet", false);
    form.setValue("amount", "");
    form.clearErrors([
      "transferDate",
      "description",
      "sourceCategoryId",
      "destinationCategoryId",
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

  useEffect(() => {
    if (!open) {
      return;
    }

    const transferSnapshot = getTransferCategorySnapshot(
      fromAccountId,
      toAccountId,
      transferDate,
      description,
      amount,
      fromTransactionId,
      toTransactionId,
    );

    if (sourceCategoryOverrideSnapshotRef.current) {
      if (sourceCategoryOverrideSnapshotRef.current !== transferSnapshot) {
        sourceCategoryOverrideSnapshotRef.current = "";
        form.setValue("sourceCategoryWasExplicitlySet", false);
      }
    }
    if (destinationCategoryOverrideSnapshotRef.current) {
      if (destinationCategoryOverrideSnapshotRef.current !== transferSnapshot) {
        destinationCategoryOverrideSnapshotRef.current = "";
        form.setValue("destinationCategoryWasExplicitlySet", false);
      }
    }

    if (
      !fromAccountId ||
      !toAccountId ||
      fromAccountId === toAccountId ||
      !description.trim() ||
      !/^\d+(\.\d{1,2})?$/.test(amount.trim()) ||
      !Number.isFinite(Number(amount)) ||
      Number(amount) <= 0
    ) {
      const timer = window.setTimeout(() => {
        if (
          !sourceExistingTransaction &&
          (!sourceCategoryId || sourceCategoryId === automaticSourceCategoryId)
        ) {
          form.setValue("sourceCategoryId", "", {
            shouldDirty: true,
            shouldValidate: true,
          });
          form.setValue("sourceCategoryWasExplicitlySet", false);
          sourceCategoryOverrideSnapshotRef.current = "";
          setAutomaticSourceCategoryId("");
        }
        if (
          !destinationExistingTransaction &&
          (!destinationCategoryId ||
            destinationCategoryId === automaticDestinationCategoryId)
        ) {
          form.setValue("destinationCategoryId", "", {
            shouldDirty: true,
            shouldValidate: true,
          });
          form.setValue("destinationCategoryWasExplicitlySet", false);
          destinationCategoryOverrideSnapshotRef.current = "";
          setAutomaticDestinationCategoryId("");
        }
      }, 350);
      return () => window.clearTimeout(timer);
    }

    const timer = window.setTimeout(async () => {
      try {
        const preview = await previewTransferCategorization({
          fromAccountId,
          toAccountId,
          transferDate: form.getValues("transferDate"),
          description: description.trim(),
          amount: Number(amount),
          sourceCategoryId: sourceExistingTransaction
            ? null
            : form.getValues("sourceCategoryWasExplicitlySet")
              ? sourceCategoryId
              : null,
          destinationCategoryId: destinationExistingTransaction
            ? null
            : form.getValues("destinationCategoryWasExplicitlySet")
              ? destinationCategoryId
              : null,
          sourceCategoryWasExplicitlySet: form.getValues(
            "sourceCategoryWasExplicitlySet",
          ),
          destinationCategoryWasExplicitlySet: form.getValues(
            "destinationCategoryWasExplicitlySet",
          ),
          fromTransactionId: fromTransactionId || undefined,
          toTransactionId: toTransactionId || undefined,
          confirmPotentialDuplicate: false,
        });

        if (
          form.getValues("fromAccountId") !== fromAccountId ||
          form.getValues("toAccountId") !== toAccountId ||
          form.getValues("description").trim() !== description.trim() ||
          form.getValues("amount").trim() !== amount.trim()
        ) {
          return;
        }

        if (
          !sourceExistingTransaction &&
          (!sourceCategoryId || sourceCategoryId === automaticSourceCategoryId)
        ) {
          const nextCategoryId = preview.outgoingCategoryId ?? "";
          setAutomaticSourceCategoryId(nextCategoryId);
          form.setValue("sourceCategoryId", preview.outgoingCategoryId ?? "", {
            shouldDirty: preview.automaticCategoryApplied,
            shouldValidate: true,
          });
          form.setValue("sourceCategoryWasExplicitlySet", false);
        }
        if (
          !destinationExistingTransaction &&
          (!destinationCategoryId ||
            destinationCategoryId === automaticDestinationCategoryId)
        ) {
          const nextCategoryId = preview.incomingCategoryId ?? "";
          setAutomaticDestinationCategoryId(nextCategoryId);
          form.setValue("destinationCategoryId", nextCategoryId, {
            shouldDirty: preview.automaticCategoryApplied,
            shouldValidate: true,
          });
          form.setValue("destinationCategoryWasExplicitlySet", false);
        }
      } catch {
        // The create request will surface validation or API errors normally.
      }
    }, 350);

    return () => window.clearTimeout(timer);
  }, [
    amount,
    automaticDestinationCategoryId,
    automaticSourceCategoryId,
    description,
    destinationCategoryId,
    destinationExistingTransaction,
    form,
    fromAccountId,
    fromTransactionId,
    open,
    sourceCategoryId,
    sourceExistingTransaction,
    toAccountId,
    transferMode,
    toTransactionId,
    transferDate,
  ]);

  return (
    <FormDialog
      open={open}
      title="Create a transfer"
      form={form}
      onSubmit={onSubmit}
      onClose={onClose}
      onEntered={() => {
        sourceCategoryOverrideSnapshotRef.current = "";
        destinationCategoryOverrideSnapshotRef.current = "";
        form.setValue("sourceCategoryWasExplicitlySet", false);
        form.setValue("destinationCategoryWasExplicitlySet", false);
        setAutomaticSourceCategoryId("");
        setAutomaticDestinationCategoryId("");
        form.setFocus("fromAccountId");
        form.clearErrors("fromAccountId");
      }}
      submitLabel="Create transfer"
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
        <Box className="ledger-transaction-form-row ledger-account-row">
          <SelectFormField<TransferFormData>
            name="sourceCategoryId"
            label="Source category (optional)"
            options={categoryOptions}
            selectClassName="ledger-category-form-select"
            displayEmpty
            onValueChange={(value) => {
              const isExplicit =
                value === "" ? true : value !== automaticSourceCategoryId;
              if (value === "") {
                sourceCategoryOverrideSnapshotRef.current =
                  getTransferCategorySnapshot(
                    form.getValues("fromAccountId"),
                    form.getValues("toAccountId"),
                    form.getValues("transferDate"),
                    form.getValues("description"),
                    form.getValues("amount"),
                    form.getValues("fromTransactionId"),
                    form.getValues("toTransactionId"),
                  );
              } else {
                sourceCategoryOverrideSnapshotRef.current = "";
              }
              form.setValue("sourceCategoryWasExplicitlySet", isExplicit);
              if (value !== automaticSourceCategoryId) {
                setAutomaticSourceCategoryId("");
              }
            }}
            helperText={
              sourceExistingTransaction
                ? "Existing source category"
                : automaticSourceCategoryId !== "" &&
                    sourceCategoryId === automaticSourceCategoryId
                  ? "Automatically applied"
                  : undefined
            }
          />
          <SelectFormField<TransferFormData>
            name="destinationCategoryId"
            label="Destination category (optional)"
            options={categoryOptions}
            selectClassName="ledger-category-form-select"
            displayEmpty
            onValueChange={(value) => {
              const isExplicit =
                value === "" ? true : value !== automaticDestinationCategoryId;
              if (value === "") {
                destinationCategoryOverrideSnapshotRef.current =
                  getTransferCategorySnapshot(
                    form.getValues("fromAccountId"),
                    form.getValues("toAccountId"),
                    form.getValues("transferDate"),
                    form.getValues("description"),
                    form.getValues("amount"),
                    form.getValues("fromTransactionId"),
                    form.getValues("toTransactionId"),
                  );
              } else {
                destinationCategoryOverrideSnapshotRef.current = "";
              }
              form.setValue("destinationCategoryWasExplicitlySet", isExplicit);
              if (value !== automaticDestinationCategoryId) {
                setAutomaticDestinationCategoryId("");
              }
            }}
            helperText={
              destinationExistingTransaction
                ? "Existing destination category"
                : automaticDestinationCategoryId !== "" &&
                    destinationCategoryId === automaticDestinationCategoryId
                  ? "Automatically applied"
                  : undefined
            }
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
        <Box className="ledger-transaction-form-row">
          <TextFormField<TransferFormData>
            name="description"
            label="Description"
            required
            fullWidth
            slotProps={{ htmlInput: { maxLength: 500 } }}
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

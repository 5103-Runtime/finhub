import { useState, type CSSProperties } from "react";
import {
  Box,
  CircularProgress,
  IconButton,
  MenuItem,
  Divider,
  Select,
  Typography,
} from "@mui/material";
import { useNavigate } from "react-router-dom";
import CheckRounded from "@mui/icons-material/CheckRounded";
import CloseRounded from "@mui/icons-material/CloseRounded";
import DeleteOutlineRounded from "@mui/icons-material/DeleteOutlineRounded";
import EditRounded from "@mui/icons-material/EditRounded";
import SettingsRounded from "@mui/icons-material/SettingsRounded";
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
  categoryOptions: CategoryOption[];
  onCategoryChange: (categoryId: string) => Promise<void>;
};

type CategoryOption = {
  value: string;
  label: string;
  color: string;
  isEmpty: boolean;
};

function getCategorySelectWidth(categoryOptions: CategoryOption[]): string {
  const longestLabelLength = Math.max(
    ...categoryOptions.map((option) => option.label.length),
  );
  return `calc(${longestLabelLength}ch + 48px)`;
}

function CategorySelect({
  value,
  categoryOptions,
  selectedCategory,
  width,
  ariaLabel,
  disabled,
  onChange,
}: {
  value: string | null;
  categoryOptions: CategoryOption[];
  selectedCategory: CategoryOption;
  width: string;
  ariaLabel: string;
  disabled: boolean;
  onChange: (categoryId: string) => Promise<void>;
}) {
  const navigate = useNavigate();
  const manageCategoriesValue = "__manage_categories__";
  return (
    <Select
      value={value ?? ""}
      size="small"
      aria-label={ariaLabel}
      disabled={disabled}
      onChange={(event) => {
        if (event.target.value === manageCategoriesValue) {
          navigate("/categories");
          return;
        }
        void onChange(event.target.value);
      }}
      className="ledger-category-select"
      displayEmpty
      renderValue={(selectedValue) =>
        categoryOptions.find((option) => option.value === selectedValue)
          ?.label ?? "None"
      }
      MenuProps={{
        slotProps: {
          paper: { className: "ledger-category-menu" },
        },
      }}
      data-category-empty={selectedCategory.isEmpty || undefined}
      style={
        {
          "--category-color": selectedCategory.color,
          "--category-select-width": width,
        } as CSSProperties
      }
      variant="outlined"
    >
      {categoryOptions.map((option) => (
        <MenuItem
          key={option.value || "uncategorized"}
          value={option.value}
          className="ledger-category-option"
          style={{ "--category-color": option.color } as CSSProperties}
        >
          {option.label}
        </MenuItem>
      ))}
      <Divider />
      <MenuItem
        value={manageCategoriesValue}
        className="ledger-category-manage-option"
        onClick={() => navigate("/categories")}
      >
        <SettingsRounded fontSize="small" />
        Edit categories
      </MenuItem>
    </Select>
  );
}

function CategoryLoadingDisplay({
  category,
  width,
}: {
  category: CategoryOption;
  width: string;
}) {
  return (
    <Box
      className="ledger-category-select ledger-category-loading"
      aria-label={`Loading category ${category.label}`}
      role="status"
      style={
        {
          "--category-color": category.color,
          "--category-select-width": width,
        } as CSSProperties
      }
    >
      <span>{category.label}</span>
      <CircularProgress size={13} aria-hidden="true" />
    </Box>
  );
}

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
  categoryOptions,
  onCategoryChange,
}: TransactionRowProps) {
  const [categoryUpdating, setCategoryUpdating] = useState(false);
  const amountClass =
    transaction.amount > 0 ? "ledger-amount positive" : "ledger-amount";
  const selectedCategory =
    categoryOptions.find((option) => option.value === transaction.categoryId) ??
    categoryOptions[0];
  const categorySelectWidth = getCategorySelectWidth(categoryOptions);

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
        {categoryUpdating ? (
          <CategoryLoadingDisplay
            category={selectedCategory}
            width={categorySelectWidth}
          />
        ) : (
          <CategorySelect
            value={transaction.categoryId}
            categoryOptions={categoryOptions}
            selectedCategory={selectedCategory}
            width={categorySelectWidth}
            ariaLabel={`Category for ${transaction.description}`}
            disabled={deleting}
            onChange={async (categoryId) => {
              setCategoryUpdating(true);
              try {
                await onCategoryChange(categoryId);
              } finally {
                setCategoryUpdating(false);
              }
            }}
          />
        )}

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
      {(potentialDuplicate || successMessage) && (
        <Box className="ledger-row-feedback">
          {potentialDuplicate && (
            <FormAlert severity="warning">
              This transaction may be a duplicate of an existing transaction.
              Please review your transactions.
            </FormAlert>
          )}
          {successMessage && (
            <FormAlert severity="success">{successMessage}</FormAlert>
          )}
        </Box>
      )}
    </>
  );
}

type TransactionEditRowProps = {
  transaction: TransactionResponse;
  categoryOptions: CategoryOption[];
  onCategoryChange: (categoryId: string) => Promise<void>;
  onCancel: () => void;
  onSave: (
    data: TransactionEditFormData,
    confirmPotentialDuplicate: boolean,
  ) => Promise<void>;
};

function hasTransactionEditChanges(
  transaction: TransactionResponse,
  data: TransactionEditFormData,
): boolean {
  return (
    transaction.transactionDate !== data.transactionDate ||
    transaction.description !== data.description.trim() ||
    transaction.amount !== Number(data.amount)
  );
}

/**
 * @brief Ledger row turned into a form. Enter or the check button saves,
 * Escape or the close button cancels. It is only mounted while editing,
 * so its form always starts from the transaction's current values.
 */
export function TransactionEditRow({
  transaction,
  categoryOptions,
  onCategoryChange,
  onCancel,
  onSave,
}: TransactionEditRowProps) {
  const [categoryUpdating, setCategoryUpdating] = useState(false);
  const [serverError, setServerError] = useState("");
  const [serverErrorSeverity, setServerErrorSeverity] = useState<
    "error" | "warning"
  >("error");
  const [confirmPotentialDuplicate, setConfirmPotentialDuplicate] =
    useState(false);
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
  const selectedCategory =
    categoryOptions.find((option) => option.value === transaction.categoryId) ??
    categoryOptions[0];
  const categorySelectWidth = getCategorySelectWidth(categoryOptions);

  const onSubmit: SubmitHandler<TransactionEditFormData> = async (data) => {
    setServerError("");
    setServerErrorSeverity("error");
    if (!hasTransactionEditChanges(transaction, data)) {
      onCancel();
      return;
    }

    try {
      await onSave(data, confirmPotentialDuplicate);
    } catch (requestError) {
      if (
        requestError instanceof ApiError &&
        requestError.errorCode === "potential_duplicate"
      ) {
        setServerErrorSeverity("warning");
        setServerError(
          "This transaction might be a duplicate. Submit again if you still wish to add transaction.",
        );
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
      {categoryUpdating ? (
        <CategoryLoadingDisplay
          category={selectedCategory}
          width={categorySelectWidth}
        />
      ) : (
        <CategorySelect
          value={transaction.categoryId}
          categoryOptions={categoryOptions}
          selectedCategory={selectedCategory}
          width={categorySelectWidth}
          ariaLabel={`Category for ${transaction.description}`}
          disabled={isSubmitting}
          onChange={async (categoryId) => {
            setCategoryUpdating(true);
            try {
              await onCategoryChange(categoryId);
            } finally {
              setCategoryUpdating(false);
            }
          }}
        />
      )}
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
          <FormAlert severity={serverErrorSeverity}>{serverError}</FormAlert>
        </Box>
      )}
    </Form>
  );
}

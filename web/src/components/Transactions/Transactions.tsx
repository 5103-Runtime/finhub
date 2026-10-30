import { useEffect, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import {
  Alert,
  Box,
  Button,
  ButtonBase,
  Card,
  CardContent,
  Collapse,
  Container,
  Stack,
  Typography,
} from "@mui/material";
import AddRounded from "@mui/icons-material/AddRounded";
import ExpandMoreRounded from "@mui/icons-material/ExpandMoreRounded";
import ReceiptLongRounded from "@mui/icons-material/ReceiptLongRounded";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm, type SubmitHandler } from "react-hook-form";
import Footer from "@app/components/Footer";
import Navbar from "@app/components/Navbar";
import { ConfirmDialog, FormDialog } from "@app/components/Dialog";
import { SelectFormField, TextFormField } from "@app/components/Form";
import { getAccounts, type AccountResponse } from "@app/services/accountsApi";
import {
  createTransaction,
  deleteTransaction,
  getTransactions,
  updateTransaction,
  type TransactionResponse,
} from "@app/services/transactionApi";
import {
  getCategories,
  type CategoryResponse,
} from "@app/services/categoriesApi";
import {
  getEmptyTransactionForm,
  transactionSchema,
  type TransactionEditFormData,
  type TransactionFormData,
} from "@app/validators/transactionSchemas";
import {
  accountTypeIcons,
  accountTypeOrder,
  compareAccountsByName,
  getAccountTypePluralLabel,
} from "@app/utils/accounts";
import { applyFieldErrors, getErrorMessage } from "@app/utils/formErrors";
import { getCategoryColor } from "@app/utils/categories";
import { TransactionEditRow, TransactionRow } from "./TransactionRow";

import "@app/components/Accounts/Accounts.css";
import "./Transactions.css";
import { ApiError } from "@app/services/apiClient";

export default function Transactions() {
  const [searchParams] = useSearchParams();
  const selectedAccountId = searchParams.get("accountId");
  const [transactions, setTransactions] = useState<TransactionResponse[]>([]);
  const [accounts, setAccounts] = useState<AccountResponse[]>([]);
  const [categories, setCategories] = useState<CategoryResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadFailed, setLoadFailed] = useState(false);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [collapsedAccountIds, setCollapsedAccountIds] = useState<Set<string>>(
    new Set(),
  );
  const [serverError, setServerError] = useState("");
  const [successTransactionId, setSuccessTransactionId] = useState<
    string | null
  >(null);
  const [deletingId, setDeletingId] = useState<string | null>(null);
  const [transactionToDelete, setTransactionToDelete] =
    useState<TransactionResponse | null>(null);
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [isDuplicate, setIsDuplicate] = useState(false);

  useEffect(() => {
    let isCurrentRequest = true;

    Promise.all([getTransactions(), getAccounts(), getCategories()])
      .then(([loadedTransactions, loadedAccounts, loadedCategories]) => {
        if (isCurrentRequest) {
          setTransactions(loadedTransactions);
          setAccounts(loadedAccounts);
          setCategories(loadedCategories);
          if (
            selectedAccountId &&
            loadedAccounts.some((account) => account.id === selectedAccountId)
          ) {
            setCollapsedAccountIds(
              new Set(
                loadedAccounts
                  .map((account) => account.id)
                  .filter((accountId) => accountId !== selectedAccountId),
              ),
            );
          }
        }
      })
      .catch((requestError) => {
        if (isCurrentRequest) {
          setLoadFailed(true);
          setServerError(
            getErrorMessage(requestError, "Unable to load transactions."),
          );
        }
      })
      .finally(() => {
        if (isCurrentRequest) {
          setLoading(false);
        }
      });

    return () => {
      isCurrentRequest = false;
    };
  }, [selectedAccountId]);

  useEffect(() => {
    if (!selectedAccountId || loading || !accounts.length) {
      return;
    }

    const accountSection = document.getElementById(
      `ledger-account-${selectedAccountId}`,
    );
    accountSection?.scrollIntoView({ behavior: "smooth", block: "start" });
  }, [accounts, loading, selectedAccountId]);

  const form = useForm<TransactionFormData>({
    resolver: zodResolver(transactionSchema),
    defaultValues: getEmptyTransactionForm(),
    mode: "onTouched",
  });

  const accountOptions = accounts.map((account) => ({
    value: account.id,
    label: account.name,
  }));
  const categoryOptions = [
    { value: "", label: "None", color: "var(--color-mist)", isEmpty: true },
    ...categories.map((category) => ({
      value: category.id,
      label: category.name,
      color: getCategoryColor(category.name),
      isEmpty: false,
    })),
  ];

  // Group the transactions by account. The API order (newest first) is kept
  // inside each account.
  const transactionsByAccount = new Map<string, TransactionResponse[]>();
  for (const transaction of transactions) {
    const accountTransactions = transactionsByAccount.get(
      transaction.accountId,
    );
    if (accountTransactions) {
      accountTransactions.push(transaction);
    } else {
      transactionsByAccount.set(transaction.accountId, [transaction]);
    }
  }

  // Same rule as the Accounts page: only add type headings when there are
  // several account types to separate.
  const accountTypeCount = new Set(
    accounts.map((account) => account.accountType),
  ).size;
  const shouldGroupAccounts = accounts.length > 1 && accountTypeCount > 1;

  // Refetch after a save so the list keeps the API's ordering (newest first).
  const reloadTransactions = async (): Promise<boolean> => {
    try {
      setTransactions(await getTransactions());
      return true;
    } catch (requestError) {
      setServerError(
        getErrorMessage(
          requestError,
          "Saved, but unable to refresh the transactions.",
        ),
      );
      return false;
    }
  };

  const handleCategoryChange = async (
    transaction: TransactionResponse,
    categoryId: string,
  ) => {
    const updatedCategoryId = categoryId || null;

    try {
      await updateTransaction(transaction.id, {
        categoryId: updatedCategoryId,
        transactionDate: transaction.transactionDate,
        description: transaction.description,
        amount: transaction.amount,
      });
      await reloadTransactions();
    } catch (requestError) {
      setServerError(
        getErrorMessage(
          requestError,
          "Unable to update the transaction category.",
        ),
      );
    }
  };

  const toggleAccount = (accountId: string) => {
    setCollapsedAccountIds((current) => {
      const next = new Set(current);
      if (next.has(accountId)) {
        next.delete(accountId);
      } else {
        next.add(accountId);
      }
      return next;
    });
  };

  const openCreateModal = () => {
    form.reset({
      ...getEmptyTransactionForm(),
      accountId: accounts[0]?.id ?? "",
    });
    setServerError("");
    setSuccessTransactionId(null);
    setEditingId(null);
    setIsDuplicate(false);
    setIsModalOpen(true);
  };

  // Clear the error too, so a failed add does not leave a stale page banner.
  const closeModal = () => {
    setIsModalOpen(false);
    setServerError("");
    setIsDuplicate(false);
  };

  const onSubmit: SubmitHandler<TransactionFormData> = async (data) => {
    setServerError("");
    setSuccessTransactionId(null);
    try {
      await createTransaction({
        accountId: data.accountId,
        categoryId: data.categoryId || null,
        transactionDate: data.transactionDate,
        description: data.description,
        amount: Number(data.amount),
        confirmPotentialDuplicate: isDuplicate,
      });
      closeModal();
      // Make sure the account the new transaction belongs to is open.
      setCollapsedAccountIds((current) => {
        const next = new Set(current);
        next.delete(data.accountId);
        return next;
      });
      await reloadTransactions();

      setIsDuplicate(false);
    } catch (requestError) {
      if (
        requestError instanceof ApiError &&
        requestError.errorCode === "potential_duplicate"
      ) {
        setServerError(
          "This transaction might be a duplicate. Submit again if you still wish to add transaction.",
        );
        setIsDuplicate(true);
        return;
      }

      applyFieldErrors(
        requestError,
        form.setError,
        Object.keys(getEmptyTransactionForm()),
      );
      setServerError(
        getErrorMessage(requestError, "Unable to save the transaction."),
      );
    }
  };

  // Errors are thrown on purpose: the edit row catches them and shows them inline.
  const handleSaveEdit = async (
    id: string,
    data: TransactionEditFormData,
    confirmPotentialDuplicate: boolean,
  ) => {
    setServerError("");
    setSuccessTransactionId(null);
    await updateTransaction(id, {
      transactionDate: data.transactionDate,
      description: data.description,
      amount: Number(data.amount),
      confirmPotentialDuplicate,
    });
    if (await reloadTransactions()) {
      setEditingId(null);
      setSuccessTransactionId(id);
    } else {
      throw new Error("Saved, but unable to refresh the transactions.");
    }
  };

  const handleDelete = async () => {
    if (!transactionToDelete) {
      return;
    }

    const transaction = transactionToDelete;
    setDeletingId(transaction.id);
    setServerError("");
    try {
      await deleteTransaction(transaction.id);
      setTransactions((current) =>
        current.filter((item) => item.id !== transaction.id),
      );
      setIsDeleteModalOpen(false);
    } catch (requestError) {
      setServerError(
        getErrorMessage(requestError, "Unable to delete the transaction."),
      );
    } finally {
      setDeletingId(null);
    }
  };

  // One collapsible card per account, holding that account's transactions.
  const renderAccountSection = (account: AccountResponse) => {
    const accountTransactions = transactionsByAccount.get(account.id) ?? [];
    const isCollapsed = collapsedAccountIds.has(account.id);
    const Icon = accountTypeIcons[account.accountType];
    const panelId = `ledger-account-${account.id}`;

    return (
      <Box
        id={`ledger-account-${account.id}`}
        key={account.id}
        className="ledger-account"
      >
        <ButtonBase
          className="ledger-account-header"
          onClick={() => toggleAccount(account.id)}
          aria-expanded={!isCollapsed}
          aria-controls={panelId}
        >
          <Box className={`account-management-icon ${account.accountType}`}>
            <Icon />
          </Box>
          <Box className="ledger-account-info">
            <Typography className="ledger-account-name">
              {account.name}
            </Typography>
            <Typography className="ledger-account-count">
              {accountTransactions.length}{" "}
              {accountTransactions.length === 1
                ? "transaction"
                : "transactions"}
            </Typography>
          </Box>
          <ExpandMoreRounded
            className={`ledger-chevron${isCollapsed ? " collapsed" : ""}`}
          />
        </ButtonBase>
        <Collapse in={!isCollapsed} id={panelId}>
          {accountTransactions.length === 0 ? (
            <Typography className="ledger-account-empty">
              No transactions in this account yet.
            </Typography>
          ) : (
            <Box className="ledger-list">
              <Box className="ledger-head">
                <span>Date</span>
                <span>Description</span>
                <span>Category</span>
                <span className="ledger-head-amount">Amount</span>
                <span aria-hidden="true" />
              </Box>
              {accountTransactions.map((transaction) =>
                editingId === transaction.id ? (
                  <TransactionEditRow
                    key={transaction.id}
                    transaction={transaction}
                    categoryOptions={categoryOptions}
                    onCategoryChange={(categoryId) =>
                      handleCategoryChange(transaction, categoryId)
                    }
                    onCancel={() => setEditingId(null)}
                    onSave={(data, confirmPotentialDuplicate) =>
                      handleSaveEdit(
                        transaction.id,
                        data,
                        confirmPotentialDuplicate,
                      )
                    }
                  />
                ) : (
                  <TransactionRow
                    key={transaction.id}
                    transaction={transaction}
                    account={account}
                    categoryOptions={categoryOptions}
                    onCategoryChange={(categoryId) =>
                      handleCategoryChange(transaction, categoryId)
                    }
                    onDelete={() => {
                      setServerError("");
                      setSuccessTransactionId(null);
                      setTransactionToDelete(transaction);
                      setIsDeleteModalOpen(true);
                    }}
                    deleting={deletingId === transaction.id}
                    successMessage={
                      successTransactionId === transaction.id
                        ? "Transaction updated successfully."
                        : undefined
                    }
                    onEdit={() => {
                      setServerError("");
                      setSuccessTransactionId(null);
                      setEditingId(transaction.id);
                    }}
                    potentialDuplicate={transaction.potentialDuplicate}
                  />
                ),
              )}
            </Box>
          )}
        </Collapse>
      </Box>
    );
  };

  const renderContent = () => {
    if (loading) {
      return (
        <Typography className="accounts-empty">
          Loading transactions...
        </Typography>
      );
    }

    // A failed load leaves the lists empty, so don't show the "no accounts" or
    // "no transactions" empty states, which would be wrong.
    if (loadFailed) {
      return (
        <Typography className="accounts-empty">
          Transactions could not be loaded.
        </Typography>
      );
    }

    if (accounts.length === 0) {
      return (
        <Card className="accounts-empty-card">
          <CardContent>
            <ReceiptLongRounded />
            <Typography className="empty-title">No accounts yet</Typography>
            <Typography className="accounts-empty">
              Add an account before you add transactions.
            </Typography>
            <Button
              component={Link}
              to="/accounts"
              className="accounts-primary-button empty-action"
            >
              Go to accounts
            </Button>
          </CardContent>
        </Card>
      );
    }

    if (transactions.length === 0) {
      return (
        <Card className="accounts-empty-card">
          <CardContent>
            <ReceiptLongRounded />
            <Typography className="empty-title">No transactions yet</Typography>
            <Typography className="accounts-empty">
              Add your first transaction to get started.
            </Typography>
            <Button
              className="accounts-primary-button empty-action"
              startIcon={<AddRounded />}
              onClick={openCreateModal}
            >
              Add transaction
            </Button>
          </CardContent>
        </Card>
      );
    }

    if (shouldGroupAccounts) {
      return (
        <Stack spacing={4}>
          {accountTypeOrder.map((accountType) => {
            const groupedAccounts = accounts
              .filter((account) => account.accountType === accountType)
              .sort(compareAccountsByName);
            if (groupedAccounts.length === 0) {
              return null;
            }
            return (
              <Box key={accountType} className="account-group">
                <Typography className="account-group-title">
                  {getAccountTypePluralLabel(accountType)} (
                  {groupedAccounts.length})
                </Typography>
                <Stack spacing={1.5}>
                  {groupedAccounts.map(renderAccountSection)}
                </Stack>
              </Box>
            );
          })}
        </Stack>
      );
    }

    return (
      <Stack spacing={1.5}>
        {[...accounts].sort(compareAccountsByName).map(renderAccountSection)}
      </Stack>
    );
  };

  return (
    <Box className="accounts-page">
      <Container maxWidth="xl" className="accounts-shell">
        <Navbar />
        <Box component="main">
          <Box className="accounts-welcome">
            <Box>
              <Typography className="accounts-kicker">YOUR LEDGER</Typography>
              <Typography component="h1" className="accounts-title">
                Transactions
              </Typography>
            </Box>
            {transactions.length > 0 && (
              <Button
                className="accounts-add-button"
                startIcon={<AddRounded />}
                onClick={openCreateModal}
              >
                Add transaction
              </Button>
            )}
          </Box>

          {serverError && !isModalOpen && (
            <Alert
              className="accounts-error"
              severity="error"
              variant="outlined"
            >
              {serverError}
            </Alert>
          )}

          <Box component="section" className="accounts-list-section">
            <Box className="section-heading">
              <Typography component="h2" className="section-title">
                Transactions ({transactions.length})
              </Typography>
            </Box>
            {renderContent()}
          </Box>
        </Box>
        <Footer />
      </Container>

      <FormDialog
        open={isModalOpen}
        title="Add a transaction"
        form={form}
        onSubmit={onSubmit}
        onClose={closeModal}
        submitLabel="Add transaction"
        error={serverError}
        errorSeverity={isDuplicate ? "warning" : "error"}
      >
        <SelectFormField<TransactionFormData>
          name="accountId"
          label="Account"
          options={accountOptions}
        />
        <SelectFormField<TransactionFormData>
          name="categoryId"
          label="Category"
          options={categoryOptions}
          selectClassName="ledger-category-form-select"
          displayEmpty
        />
        <Stack direction="row" spacing={1.5}>
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
            helperText="Use a negative amount for money out."
            slotProps={{ htmlInput: { inputMode: "decimal" } }}
          />
        </Stack>
        <TextFormField<TransactionFormData>
          name="description"
          label="Description"
          required
          fullWidth
          slotProps={{ htmlInput: { maxLength: 500 } }}
        />
      </FormDialog>

      <ConfirmDialog
        open={isDeleteModalOpen}
        title="Delete transaction"
        message={
          <>
            Are you sure you want to delete{" "}
            <strong>{transactionToDelete?.description}</strong>?
          </>
        }
        confirmLabel="Delete transaction"
        loading={deletingId !== null}
        onConfirm={handleDelete}
        onCancel={() => setIsDeleteModalOpen(false)}
        onExited={() => setTransactionToDelete(null)}
      />
    </Box>
  );
}

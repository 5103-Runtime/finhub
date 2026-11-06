import { useEffect, useRef, useState } from "react";
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
  FormControl,
  IconButton,
  InputAdornment,
  InputLabel,
  MenuItem,
  Select,
  TextField,
  Stack,
  Typography,
  TableSortLabel
} from "@mui/material";
import AddRounded from "@mui/icons-material/AddRounded";
import CloseRounded from "@mui/icons-material/CloseRounded";
import ExpandMoreRounded from "@mui/icons-material/ExpandMoreRounded";
import FilterListRounded from "@mui/icons-material/FilterListRounded";
import ReceiptLongRounded from "@mui/icons-material/ReceiptLongRounded";
import UploadFileRounded from "@mui/icons-material/UploadFileRounded";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm, useWatch, type SubmitHandler } from "react-hook-form";
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
  type TransactionFilters,
  type TransactionResponse,
} from "@app/services/transactionApi";
import {
  getCategories,
  type CategoryResponse,
} from "@app/services/categoriesApi";
import {
  getEmptyTransactionForm,
  transactionFilterSchema,
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
import { CategoryFilter } from "./CategoryFilter";
import { TransactionEditRow, TransactionRow } from "./TransactionRow";
import { DialogBase } from "@app/components/Dialog/DialogBase";
import StatementUpload from "@app/components/StatementUpload";

import "@app/components/Accounts/Accounts.css";
import "./Transactions.css";
import { ApiError } from "@app/services/apiClient";

function getTransactionFilters(params: URLSearchParams): TransactionFilters {
  return {
    accountId: params.get("accountId") ?? undefined,
    categoryId: params.get("categoryId") ?? undefined,
    uncategorized: params.get("uncategorized") === "true",
    fromDate: params.get("fromDate") ?? undefined,
    toDate: params.get("toDate") ?? undefined,
    minAmount: params.get("minAmount") ?? undefined,
    maxAmount: params.get("maxAmount") ?? undefined,
    search: params.get("search") ?? undefined,
  };
}

export default function Transactions() {
  const [searchParams, setSearchParams] = useSearchParams();
  const selectedAccountId = searchParams.get("accountId");
  const filterKey = searchParams.toString();
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
  const [isImportOpen, setIsImportOpen] = useState(false);
  const [importFile, setImportFile] = useState<File | null>(null);
  const [filtersOpen, setFiltersOpen] = useState(false);
  const [draftFilterValues, setDraftFilterValues] = useState({
    search: searchParams.get("search") ?? "",
    minAmount: searchParams.get("minAmount") ?? "",
    maxAmount: searchParams.get("maxAmount") ?? "",
  });
  const [searchFilterError, setSearchFilterError] = useState("");
  const [sortConfig, setSortConfig] = useState<SortConfig | null>(null);
  type SortConfig = {
    key: "transactionDate" | "description" | "amount" | "categoryId";
    ascending: boolean;
  };

  const filterDebounceRef = useRef<number | null>(null);

  // Load transactions and supporting data for the current URL filters.
  useEffect(() => {
    let isCurrentRequest = true;
    const filterParams = new URLSearchParams(filterKey);

    Promise.all([
      getTransactions(getTransactionFilters(filterParams)),
      getAccounts(),
      getCategories(),
    ])
      .then(([loadedTransactions, loadedAccounts, loadedCategories]) => {
        if (isCurrentRequest) {
          setTransactions(loadedTransactions);
          setAccounts(loadedAccounts);
          setCategories(loadedCategories);
          setLoadFailed(false);
          setServerError("");
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
  }, [filterKey]);

  // Scroll to an account selected from account navigation.
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
  const amountValue = useWatch({
    control: form.control,
    name: "amount",
  });
  const amountIndicator =
    amountValue.trim() === ""
      ? ""
      : Number(amountValue) > 0
        ? "ledger-amount-positive"
        : Number(amountValue) < 0
          ? "ledger-amount-negative"
          : "";

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

  const hasActiveFilters = [
    "accountId",
    "categoryId",
    "uncategorized",
    "fromDate",
    "toDate",
    "minAmount",
    "maxAmount",
    "search",
  ].some((key) => searchParams.has(key));

  const updateFilter = (key: string, value: string) => {
    setFiltersOpen(true);
    const next = new URLSearchParams(searchParams);
    if (value) {
      next.set(key, value);
    } else {
      next.delete(key);
    }

    if (key === "categoryId" || key === "uncategorized") {
      next.delete(key === "categoryId" ? "uncategorized" : "categoryId");
    }

    setSearchParams(next);
  };

  const updateTextFilter = (
    key: "search" | "minAmount" | "maxAmount",
    value: string,
  ) => {
    setFiltersOpen(true);
    setDraftFilterValues((current) => ({ ...current, [key]: value }));
    if (key === "search") {
      const validation = transactionFilterSchema.shape.search.safeParse(value);
      setSearchFilterError(
        validation.success ? "" : validation.error.issues[0].message,
      );
      if (!validation.success) {
        return;
      }
    }

    if (filterDebounceRef.current !== null) {
      window.clearTimeout(filterDebounceRef.current);
    }
    filterDebounceRef.current = window.setTimeout(() => {
      updateFilter(key, value);
    }, 350);
  };

  // Cancel pending filter updates when the page unmounts.
  useEffect(
    () => () => {
      if (filterDebounceRef.current !== null) {
        window.clearTimeout(filterDebounceRef.current);
      }
    },
    [],
  );

  // Auto-dismiss transaction update success feedback.
  useEffect(() => {
    if (!successTransactionId) {
      return;
    }

    const timeoutId = window.setTimeout(() => {
      setSuccessTransactionId(null);
    }, 4_000);

    return () => window.clearTimeout(timeoutId);
  }, [successTransactionId]);

  const clearFilters = () => {
    setFiltersOpen(true);
    if (filterDebounceRef.current !== null) {
      window.clearTimeout(filterDebounceRef.current);
      filterDebounceRef.current = null;
    }
    setDraftFilterValues({ search: "", minAmount: "", maxAmount: "" });
    setSearchFilterError("");
    setSearchParams(new URLSearchParams());
  };

  const toggleFilters = () => {
    setFiltersOpen((open) => !open);
  };

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
    (hasActiveFilters
      ? accounts.filter((account) => transactionsByAccount.has(account.id))
      : accounts
    ).map((account) => account.accountType),
  ).size;
  const visibleAccounts = hasActiveFilters
    ? accounts.filter((account) => transactionsByAccount.has(account.id))
    : accounts;
  const shouldGroupAccounts =
    visibleAccounts.length > 1 && accountTypeCount > 1;

  // Refetch after a save so the list keeps the API's ordering (newest first).
  const reloadTransactions = async (): Promise<boolean> => {
    try {
      setTransactions(
        await getTransactions(
          getTransactionFilters(new URLSearchParams(searchParams)),
        ),
      );
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
      categoryId:
        transactions.find((transaction) => transaction.id === id)?.categoryId ??
        null,
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

  // FIN-49 will send importFile to the parse endpoint and show the preview.
  const handleUpload = () => {
    if (!importFile) {
      return;
    }
  };

  // One collapsible card per account, holding that account's transactions.
  const renderAccountSection = (account: AccountResponse) => {
    const accountTransactions = transactionsByAccount.get(account.id) ?? [];
    const isCollapsed = collapsedAccountIds.has(account.id);
    const Icon = accountTypeIcons[account.accountType];
    const panelId = `ledger-account-${account.id}`;


    const handleSort = (key: SortConfig["key"]) => {
      setSortConfig((current) => ({
        key,
        ascending:
          current?.key === key ? !current.ascending : true,
      }));
    };
    const sortTransactions = (
      transactions: TransactionResponse[],
      sortConfig: SortConfig
    ) => {
      return [...transactions].sort((a, b) => {
        let comparison = 0;

        switch (sortConfig.key) {
          case "transactionDate":
            comparison = a.transactionDate.localeCompare(b.transactionDate);
            break;

          case "description":
            comparison = a.description.localeCompare(b.description);
            break;

          case "amount":
            comparison = a.amount - b.amount;
            break;

          case "categoryId":
            const categoryA =
              categoryOptions.find((option) => option.value === a.categoryId)?.label ?? "None";

            const categoryB =
              categoryOptions.find((option) => option.value === b.categoryId)?.label ?? "None";

            comparison = categoryA.localeCompare(categoryB);
            break;
        }

        return sortConfig.ascending ? comparison : -comparison;
      });
    };

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
                <span>
                  <TableSortLabel
                    className="transaction-sort-label"
                    active={sortConfig?.key === "transactionDate"}
                    direction={
                      sortConfig?.key === "transactionDate" && sortConfig.ascending
                        ? "asc"
                        : "desc"
                    }
                    onClick={() => handleSort("transactionDate")}
                  >
                    Date
                  </TableSortLabel>
                </span>
                <span>
                  <TableSortLabel
                    className="transaction-sort-label"
                    active={sortConfig?.key === "description"}
                    direction={
                      sortConfig?.key === "description" && sortConfig.ascending
                        ? "asc"
                        : "desc"
                    }
                    onClick={() => handleSort("description")}
                  >
                    Description
                  </TableSortLabel>
                </span>
                <span>
                  <TableSortLabel
                    className="transaction-sort-label"
                    active={sortConfig?.key === "categoryId"}
                    direction={
                      sortConfig?.key === "categoryId" && sortConfig.ascending
                        ? "asc"
                        : "desc"
                    }
                    onClick={() => handleSort("categoryId")}
                  >
                    Category
                  </TableSortLabel>
                </span>
                <span className="ledger-head-amount">
                  <TableSortLabel
                    className="transaction-sort-label"
                    active={sortConfig?.key === "amount"}
                    direction={
                      sortConfig?.key === "amount" && sortConfig.ascending
                        ? "asc"
                        : "desc"
                    }
                    onClick={() => handleSort("amount")}
                  >
                    Amount
                  </TableSortLabel>
                </span>
                <span aria-hidden="true" />
              </Box>
              {(sortConfig
                ? sortTransactions(accountTransactions, sortConfig)
                : accountTransactions
              ).map((transaction) =>
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
            <Typography className="empty-title">
              {hasActiveFilters
                ? "No matching transactions"
                : "No transactions yet"}
            </Typography>
            <Typography className="accounts-empty">
              {hasActiveFilters
                ? "Try changing or clearing your filters."
                : "Add your first transaction to get started."}
            </Typography>
            {hasActiveFilters ? (
              <Button
                className="accounts-primary-button empty-action"
                onClick={clearFilters}
              >
                Clear filters
              </Button>
            ) : (
              <Button
                className="accounts-primary-button empty-action"
                startIcon={<AddRounded />}
                onClick={openCreateModal}
              >
                Add transaction
              </Button>
            )}
          </CardContent>
        </Card>
      );
    }

    if (shouldGroupAccounts) {
      return (
        <Stack spacing={4}>
          {accountTypeOrder.map((accountType) => {
            const groupedAccounts = visibleAccounts
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
        {[...visibleAccounts]
          .sort(compareAccountsByName)
          .map(renderAccountSection)}
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
            {accounts.length > 0 && (
              <Stack direction="row" spacing={1.5}>
                <Button
                  className="accounts-add-button"
                  startIcon={<UploadFileRounded />}
                  onClick={() => setIsImportOpen(true)}
                >
                  Import statement
                </Button>
                <Button
                  className="accounts-add-button"
                  startIcon={<AddRounded />}
                  onClick={openCreateModal}
                >
                  Add transaction
                </Button>
              </Stack>
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
            <Box className="section-heading transaction-section-heading">
              <Typography component="h2" className="section-title">
                Transactions ({transactions.length})
              </Typography>
              <Button
                className={`transaction-filter-toggle${hasActiveFilters ? " has-active-filters" : ""}`}
                startIcon={<FilterListRounded />}
                onClick={toggleFilters}
                aria-expanded={filtersOpen}
                aria-controls="transaction-filter-panel"
              >
                Filters{hasActiveFilters ? " active" : ""}
              </Button>
            </Box>
            <Collapse in={filtersOpen}>
              <Box
                id="transaction-filter-panel"
                className="transaction-filters"
              >
                <Box className="transaction-filter-panel-heading">
                  <Box>
                    <Typography className="transaction-filter-title">
                      Filter transactions
                    </Typography>
                  </Box>
                  <Button
                    className="transaction-filter-clear"
                    onClick={clearFilters}
                    disabled={!hasActiveFilters}
                  >
                    Clear all
                  </Button>
                </Box>
                <Box className="transaction-filter-grid">
                  <Box className="transaction-filter-field search-field">
                    <TextField
                      label="Search description"
                      value={draftFilterValues.search}
                      onChange={(event) =>
                        updateTextFilter("search", event.target.value)
                      }
                      size="small"
                      type="text"
                      error={Boolean(searchFilterError)}
                      helperText={searchFilterError}
                      slotProps={{
                        input: {
                          endAdornment: draftFilterValues.search ? (
                            <InputAdornment position="end">
                              <IconButton
                                className="transaction-filter-search-clear"
                                aria-label="Clear description search"
                                onClick={() => updateTextFilter("search", "")}
                                edge="end"
                                size="small"
                              >
                                <CloseRounded fontSize="small" />
                              </IconButton>
                            </InputAdornment>
                          ) : undefined,
                        },
                      }}
                      fullWidth
                    />
                  </Box>
                  <Box className="transaction-filter-field">
                    <FormControl size="small" fullWidth>
                      <InputLabel>Account</InputLabel>
                      <Select
                        label="Account"
                        value={searchParams.get("accountId") ?? ""}
                        onChange={(event) =>
                          updateFilter("accountId", event.target.value)
                        }
                      >
                        <MenuItem value="">All accounts</MenuItem>
                        {accounts.map((account) => (
                          <MenuItem key={account.id} value={account.id}>
                            {account.name}
                          </MenuItem>
                        ))}
                      </Select>
                    </FormControl>
                  </Box>
                  <Box className="transaction-filter-field">
                    <CategoryFilter
                      categoryOptions={categoryOptions}
                      selectedCategoryId={searchParams.get("categoryId") ?? ""}
                      uncategorized={
                        searchParams.get("uncategorized") === "true"
                      }
                      onChange={updateFilter}
                    />
                  </Box>
                  <Box className="transaction-filter-group">
                    <span className="transaction-filter-group-label">
                      Date range
                    </span>
                    <Box className="transaction-filter-group-fields">
                      <TextField
                        label="From"
                        value={searchParams.get("fromDate") ?? ""}
                        onChange={(event) =>
                          updateFilter("fromDate", event.target.value)
                        }
                        size="small"
                        type="date"
                        slotProps={{ inputLabel: { shrink: true } }}
                      />
                      <TextField
                        label="To"
                        value={searchParams.get("toDate") ?? ""}
                        onChange={(event) =>
                          updateFilter("toDate", event.target.value)
                        }
                        size="small"
                        type="date"
                        slotProps={{ inputLabel: { shrink: true } }}
                      />
                    </Box>
                  </Box>
                  <Box className="transaction-filter-group">
                    <span className="transaction-filter-group-label">
                      Amount range
                    </span>
                    <Box className="transaction-filter-group-fields">
                      <TextField
                        label="Min"
                        value={draftFilterValues.minAmount}
                        onChange={(event) =>
                          updateTextFilter("minAmount", event.target.value)
                        }
                        size="small"
                        type="number"
                        slotProps={{ htmlInput: { step: "0.01" } }}
                      />
                      <TextField
                        label="Max"
                        value={draftFilterValues.maxAmount}
                        onChange={(event) =>
                          updateTextFilter("maxAmount", event.target.value)
                        }
                        size="small"
                        type="number"
                        slotProps={{ htmlInput: { step: "0.01" } }}
                      />
                    </Box>
                  </Box>
                </Box>
              </Box>
            </Collapse>
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
        onEntered={() => {
          form.setFocus("description");
          form.clearErrors("description");
        }}
        submitLabel="Add transaction"
        error={serverError}
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
              label="Category"
              options={categoryOptions}
              selectClassName="ledger-category-form-select"
              displayEmpty
            />
            <TextFormField<TransactionFormData>
              name="amount"
              label="Amount"
              required
              fullWidth
              helperText="Use a negative amount for money out"
              className={amountIndicator}
              slotProps={{ htmlInput: { inputMode: "decimal" } }}
            />
          </Box>
        </Box>
      </FormDialog>

      <DialogBase
        open={isImportOpen}
        title="Import statement"
        maxWidth="sm"
        busy={false}
        onClose={() => setIsImportOpen(false)}
        onExited={() => setImportFile(null)}
        actions={
          <>
            <Button
              className="dialog-cancel-button"
              onClick={() => setIsImportOpen(false)}
            >
              Close
            </Button>
            <Button
              className="dialog-primary-button"
              onClick={handleUpload}
              disabled={!importFile}
            >
              Upload
            </Button>
          </>
        }
      >
        <StatementUpload file={importFile} onFileChange={setImportFile} />
      </DialogBase>

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

import { useEffect, useRef, useState, type MouseEvent } from "react";
import { Link, useSearchParams } from "react-router-dom";
import {
  Alert,
  Box,
  Button,
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
} from "@mui/material";
import AddRounded from "@mui/icons-material/AddRounded";
import CloseRounded from "@mui/icons-material/CloseRounded";
import FilterListRounded from "@mui/icons-material/FilterListRounded";
import ReceiptLongRounded from "@mui/icons-material/ReceiptLongRounded";
import UploadFileRounded from "@mui/icons-material/UploadFileRounded";
import Sort from "@mui/icons-material/Sort";
import ArrowUpwardRounded from "@mui/icons-material/ArrowUpwardRounded";
import ArrowDownwardRounded from "@mui/icons-material/ArrowDownwardRounded";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm, useWatch, type SubmitHandler } from "react-hook-form";
import Footer from "@app/components/Footer";
import Navbar from "@app/components/Navbar";
import { FormAlert } from "@app/components/Form/FormAlert";
import { TransactionEntryDialog } from "./TransactionEntryDialog";
import { TransferEntryDialog } from "./TransferEntryDialog";
import { TransactionDeleteDialog } from "./TransactionDeleteDialog";
import { getAccounts, type AccountResponse } from "@app/services/accountsApi";
import {
  createTransaction,
  deleteTransaction,
  getTransactions,
  updateTransaction,
  type TransactionFilters,
  type TransactionResponse,
  type TransactionSortField,
  type SortOrder,
} from "@app/services/transactionApi";
import {
  createTransfer,
  deleteTransfer,
  unlinkTransfer,
} from "@app/services/transferApi";
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
  getEmptyTransferForm,
  transferSchema,
  type TransferFormData,
} from "@app/validators/transferSchemas";
import {
  accountTypeOrder,
  compareAccountsByName,
  getAccountTypePluralLabel,
} from "@app/utils/accounts";
import { applyFieldErrors, getErrorMessage } from "@app/utils/formErrors";
import { getCategoryColor } from "@app/utils/categories";
import { CategoryFilter } from "./CategoryFilter";
import { TransactionAccountSection } from "./TransactionAccountSection";
import StatementImportDialog from "@app/components/StatementImportDialog";

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

const transactionFilterKeys = [
  "accountId",
  "categoryId",
  "uncategorized",
  "fromDate",
  "toDate",
  "minAmount",
  "maxAmount",
  "search",
] as const;

type SortConfig = {
  key: "transactionDate" | "description" | "amount";
  ascending: boolean;
};

const sortByMap = {
  transactionDate: "date",
  description: "description",
  amount: "amount",
} as const satisfies Record<SortConfig["key"], TransactionSortField>;

const sortKeyMap = {
  date: "transactionDate",
  description: "description",
  amount: "amount",
} as const;

function getTransactionSort(params: URLSearchParams): {
  sortBy?: TransactionSortField;
  sortOrder?: SortOrder;
} {
  const sortBy = params.get("sortBy");
  const sortOrder = params.get("sortOrder");

  if (
    !sortBy ||
    !(sortBy in sortKeyMap) ||
    (sortOrder !== "asc" && sortOrder !== "desc")
  ) {
    return {};
  }

  return {
    sortBy: sortBy as TransactionSortField,
    sortOrder,
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
  const [isTransactionModalOpen, setIsTransactionModalOpen] = useState(false);
  const [isTransferModalOpen, setIsTransferModalOpen] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [collapsedAccountIds, setCollapsedAccountIds] = useState<Set<string>>(
    new Set(),
  );
  const [serverError, setServerError] = useState("");
  const [successTransactionId, setSuccessTransactionId] = useState<
    string | null
  >(null);
  const [successMessage, setSuccessMessage] = useState("");
  const [deletingId, setDeletingId] = useState<string | null>(null);
  const [transactionToDelete, setTransactionToDelete] =
    useState<TransactionResponse | null>(null);
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [isDuplicate, setIsDuplicate] = useState(false);
  const [isImportOpen, setIsImportOpen] = useState(false);
  const [filtersOpen, setFiltersOpen] = useState(false);
  const [draftFilterValues, setDraftFilterValues] = useState({
    search: searchParams.get("search") ?? "",
    minAmount: searchParams.get("minAmount") ?? "",
    maxAmount: searchParams.get("maxAmount") ?? "",
  });
  const [searchFilterError, setSearchFilterError] = useState("");
  const [sortOpen, setSortOpen] = useState(false);

  const filterDebounceRef = useRef<number | null>(null);
  const pageFeedbackRef = useRef<HTMLDivElement>(null);
  const sortAnchorRef = useRef<{
    accountId: string;
    viewportTop: number;
  } | null>(null);

  useEffect(() => {
    if (
      (!serverError && !successMessage) ||
      isTransactionModalOpen ||
      isTransferModalOpen
    ) {
      return;
    }

    const frame = requestAnimationFrame(() => {
      pageFeedbackRef.current?.scrollIntoView({
        behavior: "smooth",
        block: "start",
      });
    });

    return () => cancelAnimationFrame(frame);
  }, [
    isTransactionModalOpen,
    isTransferModalOpen,
    serverError,
    successMessage,
  ]);

  // Load transactions and supporting data for the current URL filters.
  useEffect(() => {
    let isCurrentRequest = true;
    const filterParams = new URLSearchParams(filterKey);
    const sorting = getTransactionSort(filterParams);

    Promise.all([
      getTransactions(
        getTransactionFilters(filterParams),
        sorting.sortBy,
        sorting.sortOrder,
      ),
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

          const sortAnchor = sortAnchorRef.current;
          if (sortAnchor) {
            sortAnchorRef.current = null;
            window.requestAnimationFrame(() => {
              const accountSection = document.getElementById(
                `ledger-account-${sortAnchor.accountId}`,
              );
              if (!accountSection) {
                return;
              }

              const targetScrollTop =
                window.scrollY +
                accountSection.getBoundingClientRect().top -
                sortAnchor.viewportTop;
              window.scrollTo(0, targetScrollTop);
            });
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

  const transferForm = useForm<TransferFormData>({
    resolver: zodResolver(transferSchema),
    defaultValues: getEmptyTransferForm(),
    mode: "onTouched",
  });
  const transactionForm = useForm<TransactionFormData>({
    resolver: zodResolver(transactionSchema),
    defaultValues: getEmptyTransactionForm(),
    mode: "onTouched",
  });
  const transferFromAccountId = useWatch({
    control: transferForm.control,
    name: "fromAccountId",
  });
  const transferToAccountId = useWatch({
    control: transferForm.control,
    name: "toAccountId",
  });
  const transferFromTransactionId = useWatch({
    control: transferForm.control,
    name: "fromTransactionId",
  });
  const transferToTransactionId = useWatch({
    control: transferForm.control,
    name: "toTransactionId",
  });
  const selectedExistingTransaction =
    transactions.find(
      (transaction) => transaction.id === transferFromTransactionId,
    ) ??
    transactions.find(
      (transaction) => transaction.id === transferToTransactionId,
    );

  useEffect(() => {
    if (!selectedExistingTransaction) {
      return;
    }

    transferForm.setValue(
      "transferDate",
      selectedExistingTransaction.transactionDate,
      { shouldValidate: false },
    );
    transferForm.setValue(
      "description",
      selectedExistingTransaction.description,
      { shouldValidate: false },
    );
    transferForm.setValue(
      "amount",
      Math.abs(selectedExistingTransaction.amount).toFixed(2),
      { shouldValidate: false },
    );
    transferForm.setValue(
      "categoryId",
      selectedExistingTransaction.categoryId ?? "",
      { shouldValidate: false },
    );
    transferForm.clearErrors(["transferDate", "description", "amount"]);
  }, [selectedExistingTransaction, transferForm]);

  const accountOptions = accounts.map((account) => ({
    value: account.id,
    label: account.name,
  }));
  const unlinkedTransactionOptions = transactions
    .filter((transaction) => transaction.transferId === null)
    .map((transaction) => {
      const account = accounts.find(
        (candidate) => candidate.id === transaction.accountId,
      );
      return {
        value: transaction.id,
        label: `${account?.name ?? "Account"} · ${transaction.transactionDate} · ${transaction.description} (${transaction.amount.toFixed(2)})`,
      };
    });
  const sourceTransactionOptions = unlinkedTransactionOptions.filter(
    (option) => {
      const transaction = transactions.find(
        (candidate) => candidate.id === option.value,
      );
      return (
        transaction?.accountId === transferFromAccountId &&
        transaction.amount < 0
      );
    },
  );
  const destinationTransactionOptions = unlinkedTransactionOptions.filter(
    (option) => {
      const transaction = transactions.find(
        (candidate) => candidate.id === option.value,
      );
      return (
        transaction?.accountId === transferToAccountId && transaction.amount > 0
      );
    },
  );
  const categoryOptions = [
    { value: "", label: "None", color: "var(--color-mist)", isEmpty: true },
    ...categories.map((category) => ({
      value: category.id,
      label: category.name,
      color: getCategoryColor(category.name),
      isEmpty: false,
    })),
  ];

  const hasActiveFilters = transactionFilterKeys.some((key) =>
    searchParams.has(key),
  );

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

  useEffect(() => {
    if (!successMessage) {
      return;
    }

    const timeoutId = window.setTimeout(() => {
      setSuccessMessage("");
    }, 4_000);

    return () => window.clearTimeout(timeoutId);
  }, [successMessage]);

  const clearFilters = () => {
    setFiltersOpen(true);
    if (filterDebounceRef.current !== null) {
      window.clearTimeout(filterDebounceRef.current);
      filterDebounceRef.current = null;
    }
    setDraftFilterValues({ search: "", minAmount: "", maxAmount: "" });
    setSearchFilterError("");
    const next = new URLSearchParams(searchParams);
    for (const key of transactionFilterKeys) {
      next.delete(key);
    }
    setSearchParams(next);
  };

  const toggleFilters = () => {
    setSortOpen(false);
    setFiltersOpen((open) => !open);
  };

  const toggleSort = () => {
    setFiltersOpen(false);
    setSortOpen((open) => !open);
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
      const params = new URLSearchParams(searchParams);
      const sorting = getTransactionSort(params);
      setTransactions(
        await getTransactions(
          getTransactionFilters(params),
          sorting.sortBy,
          sorting.sortOrder,
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

  const resetEntryState = () => {
    setServerError("");
    setSuccessMessage("");
    setSuccessTransactionId(null);
    setEditingId(null);
    setIsDuplicate(false);
  };

  const openCreateTransactionModal = () => {
    transactionForm.reset({
      ...getEmptyTransactionForm(),
      accountId: accounts[0]?.id ?? "",
    });
    resetEntryState();
    setIsTransactionModalOpen(true);
  };

  const openCreateTransferModal = () => {
    transferForm.reset(getEmptyTransferForm());
    resetEntryState();
    setIsTransferModalOpen(true);
  };

  // Clear the error too, so a failed add does not leave a stale page banner.
  const closeTransactionModal = () => {
    setIsTransactionModalOpen(false);
    setServerError("");
    setIsDuplicate(false);
  };

  const closeTransferModal = () => {
    setIsTransferModalOpen(false);
    setServerError("");
    setIsDuplicate(false);
  };

  const onSubmitTransaction: SubmitHandler<TransactionFormData> = async (
    data,
  ) => {
    setServerError("");
    setSuccessMessage("");
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
      setCollapsedAccountIds((current) => {
        const next = new Set(current);
        next.delete(data.accountId);
        return next;
      });
      closeTransactionModal();
      if (await reloadTransactions()) {
        setSuccessMessage("Transaction created successfully.");
      }
    } catch (requestError) {
      applyFieldErrors(
        requestError,
        transactionForm.setError,
        Object.keys(getEmptyTransactionForm()),
      );
      if (
        requestError instanceof ApiError &&
        requestError.errorCode === "potential_duplicate"
      ) {
        setServerError(
          "This transaction might be a duplicate. Submit again if you still wish to add it.",
        );
        setIsDuplicate(true);
        return;
      }
      setServerError(
        getErrorMessage(requestError, "Unable to save the transaction."),
      );
    }
  };

  const onSubmitTransfer: SubmitHandler<TransferFormData> = async (data) => {
    setServerError("");
    setSuccessMessage("");
    setSuccessTransactionId(null);
    try {
      const sourceTransaction = transactions.find(
        (transaction) => transaction.id === data.fromTransactionId,
      );
      const destinationTransaction = transactions.find(
        (transaction) => transaction.id === data.toTransactionId,
      );
      if (
        sourceTransaction &&
        destinationTransaction &&
        Math.abs(sourceTransaction.amount) !==
          Math.abs(destinationTransaction.amount)
      ) {
        setServerError("The selected transactions must have the same amount.");
        return;
      }

      await createTransfer({
        fromAccountId: data.fromAccountId,
        toAccountId: data.toAccountId,
        transferDate: data.transferDate,
        description: data.description,
        amount: Number(data.amount),
        categoryId: data.categoryId || null,
        fromTransactionId: data.fromTransactionId || undefined,
        toTransactionId: data.toTransactionId || undefined,
        confirmPotentialDuplicate: isDuplicate,
      });
      closeTransferModal();
      if (await reloadTransactions()) {
        setSuccessMessage("Transfer created successfully.");
      }
    } catch (requestError) {
      applyFieldErrors(
        requestError,
        transferForm.setError,
        Object.keys(getEmptyTransferForm()),
      );
      if (
        requestError instanceof ApiError &&
        requestError.errorCode === "potential_duplicate"
      ) {
        setServerError(
          "This transfer might be a duplicate. Submit again if you still wish to add it.",
        );
        setIsDuplicate(true);
        return;
      }
      setServerError(
        getErrorMessage(requestError, "Unable to save the transfer."),
      );
    }
  };

  // Errors are thrown on purpose: the edit row catches them and shows them inline.
  const handleSaveEdit = async (
    id: string,
    data: TransactionEditFormData,
    confirmPotentialDuplicate: boolean,
  ) => {
    const editedTransaction = transactions.find(
      (transaction) => transaction.id === id,
    );
    setServerError("");
    setSuccessMessage("");
    setSuccessTransactionId(null);
    await updateTransaction(id, {
      categoryId: editedTransaction?.categoryId ?? null,
      transactionDate: data.transactionDate,
      description: data.description,
      amount: Number(data.amount),
      confirmPotentialDuplicate,
    });
    if (await reloadTransactions()) {
      setEditingId(null);
      if (editedTransaction?.transferId) {
        setSuccessMessage("Transfer updated successfully.");
      } else {
        setSuccessTransactionId(id);
      }
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
    setSuccessMessage("");
    try {
      if (transaction.transferId) {
        await deleteTransfer(transaction.transferId);
        setTransactions((current) =>
          current.filter((item) => item.transferId !== transaction.transferId),
        );
        setSuccessMessage("Transfer deleted successfully.");
      } else {
        await deleteTransaction(transaction.id);
        setTransactions((current) =>
          current.filter((item) => item.id !== transaction.id),
        );
        setSuccessMessage("Transaction deleted successfully.");
      }
      setIsDeleteModalOpen(false);
    } catch (requestError) {
      setServerError(
        getErrorMessage(requestError, "Unable to delete the transaction."),
      );
    } finally {
      setDeletingId(null);
    }
  };

  const handleUnlink = async () => {
    if (!transactionToDelete?.transferId) {
      return;
    }

    setDeletingId(transactionToDelete.id);
    setServerError("");
    setSuccessMessage("");
    try {
      await unlinkTransfer(transactionToDelete.transferId);
      setIsDeleteModalOpen(false);
      if (await reloadTransactions()) {
        setSuccessMessage("Transfer unlinked successfully.");
      }
    } catch (requestError) {
      setServerError(
        getErrorMessage(requestError, "Unable to unlink the transfer."),
      );
    } finally {
      setDeletingId(null);
    }
  };
  const handleSort = (
    key: SortConfig["key"],
    event: MouseEvent<HTMLElement>,
  ) => {
    const updatedParams = new URLSearchParams(searchParams);
    const sortBy = sortByMap[key];

    const currentSortBy = updatedParams.get("sortBy");
    const currentSortOrder = updatedParams.get("sortOrder");

    const nextSortOrder =
      currentSortBy === sortBy && currentSortOrder === "asc" ? "desc" : "asc";

    updatedParams.set("sortBy", sortBy);
    updatedParams.set("sortOrder", nextSortOrder);

    const accountSection =
      event.currentTarget.closest<HTMLElement>(".ledger-account");
    if (accountSection) {
      sortAnchorRef.current = {
        accountId: accountSection.id.replace("ledger-account-", ""),
        viewportTop: accountSection.getBoundingClientRect().top,
      };
    }
    setSearchParams(updatedParams, { preventScrollReset: true });
  };

  const handleClearSort = () => {
    const updatedParams = new URLSearchParams(searchParams);

    updatedParams.delete("sortBy");
    updatedParams.delete("sortOrder");

    setSearchParams(updatedParams);
  };

  const sortConfig = (() => {
    const { sortBy, sortOrder } = getTransactionSort(searchParams);

    if (!sortBy || !sortOrder) {
      return null;
    }

    return {
      key: sortKeyMap[sortBy],
      ascending: sortOrder === "asc",
    };
  })();

  const renderAccountSection = (account: AccountResponse) => (
    <TransactionAccountSection
      key={account.id}
      account={account}
      transactions={transactionsByAccount.get(account.id) ?? []}
      isCollapsed={collapsedAccountIds.has(account.id)}
      sortConfig={sortConfig}
      categoryOptions={categoryOptions}
      editingId={editingId}
      deletingId={deletingId}
      successTransactionId={successTransactionId}
      onToggle={toggleAccount}
      onSort={handleSort}
      onClearSort={handleClearSort}
      onCategoryChange={handleCategoryChange}
      onCancelEdit={() => setEditingId(null)}
      onSaveEdit={handleSaveEdit}
      onDelete={(transaction) => {
        setServerError("");
        setSuccessMessage("");
        setSuccessTransactionId(null);
        setTransactionToDelete(transaction);
        setIsDeleteModalOpen(true);
      }}
      onEdit={(transaction) => {
        setServerError("");
        setSuccessMessage("");
        setSuccessTransactionId(null);
        setEditingId(transaction.id);
      }}
    />
  );

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
                onClick={openCreateTransactionModal}
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
              <Stack
                className="transaction-primary-actions"
                direction="row"
                spacing={1.5}
              >
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
                  onClick={openCreateTransactionModal}
                >
                  Add transaction
                </Button>
                <Button
                  className="accounts-add-button"
                  startIcon={<AddRounded />}
                  onClick={openCreateTransferModal}
                >
                  Add transfer
                </Button>
              </Stack>
            )}
          </Box>

          {!isTransactionModalOpen &&
            !isTransferModalOpen &&
            (serverError || successMessage) && (
              <Box ref={pageFeedbackRef}>
                {serverError ? (
                  <Alert
                    className="page-error"
                    severity="error"
                    variant="outlined"
                  >
                    {serverError}
                  </Alert>
                ) : (
                  <FormAlert severity="success">{successMessage}</FormAlert>
                )}
              </Box>
            )}

          <Box component="section" className="accounts-list-section">
            <Box className="section-heading transaction-section-heading">
              <Typography component="h2" className="section-title">
                Transactions ({transactions.length})
              </Typography>
              <Box className="transaction-button-row">
                <Button
                  className={`transaction-filter-toggle${hasActiveFilters ? " has-active-filters" : ""}`}
                  startIcon={<FilterListRounded />}
                  onClick={toggleFilters}
                  aria-expanded={filtersOpen}
                  aria-controls="transaction-filter-panel"
                >
                  Filters{hasActiveFilters ? " active" : ""}
                </Button>
                <Button
                  className="transaction-sort-btn"
                  startIcon={<Sort />}
                  onClick={toggleSort}
                  aria-expanded={sortOpen}
                  aria-controls="transaction-sort-panel"
                >
                  Sort
                </Button>
              </Box>
            </Box>
            <Collapse in={sortOpen}>
              <Box
                id="transaction-sort-panel"
                className="transaction-sort-panel"
              >
                <Button
                  className="transaction-sort-btn"
                  variant={
                    sortConfig?.key === "transactionDate"
                      ? "contained"
                      : "outlined"
                  }
                  onClick={(event) => handleSort("transactionDate", event)}
                  endIcon={
                    sortConfig?.key === "transactionDate" ? (
                      sortConfig.ascending ? (
                        <ArrowUpwardRounded />
                      ) : (
                        <ArrowDownwardRounded />
                      )
                    ) : undefined
                  }
                >
                  Date
                </Button>

                <Button
                  className="transaction-sort-btn"
                  variant={
                    sortConfig?.key === "description" ? "contained" : "outlined"
                  }
                  onClick={(event) => handleSort("description", event)}
                  endIcon={
                    sortConfig?.key === "description" ? (
                      sortConfig.ascending ? (
                        <ArrowUpwardRounded />
                      ) : (
                        <ArrowDownwardRounded />
                      )
                    ) : undefined
                  }
                >
                  Description
                </Button>

                <Button
                  className="transaction-sort-btn"
                  variant={
                    sortConfig?.key === "amount" ? "contained" : "outlined"
                  }
                  onClick={(event) => handleSort("amount", event)}
                  endIcon={
                    sortConfig?.key === "amount" ? (
                      sortConfig.ascending ? (
                        <ArrowUpwardRounded />
                      ) : (
                        <ArrowDownwardRounded />
                      )
                    ) : undefined
                  }
                >
                  Amount
                </Button>
                {sortConfig && (
                  <Box className="transaction-sort-clear">
                    <Button
                      className="transaction-sort-clear-btn"
                      startIcon={<CloseRounded />}
                      onClick={handleClearSort}
                    >
                      Clear sorting
                    </Button>
                  </Box>
                )}
              </Box>
            </Collapse>
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

      <TransactionEntryDialog
        open={isTransactionModalOpen}
        form={transactionForm}
        accountOptions={accountOptions}
        categoryOptions={categoryOptions}
        error={serverError}
        isDuplicate={isDuplicate}
        onSubmit={onSubmitTransaction}
        onClose={closeTransactionModal}
      />
      <TransferEntryDialog
        open={isTransferModalOpen}
        form={transferForm}
        accountOptions={accountOptions}
        categoryOptions={categoryOptions}
        sourceTransactionOptions={sourceTransactionOptions}
        destinationTransactionOptions={destinationTransactionOptions}
        selectedExistingTransaction={selectedExistingTransaction}
        error={serverError}
        isDuplicate={isDuplicate}
        onSubmit={onSubmitTransfer}
        onClose={closeTransferModal}
      />
      <StatementImportDialog
        open={isImportOpen}
        accounts={accounts}
        categoryOptions={categoryOptions}
        defaultAccountId={selectedAccountId ?? accounts[0]?.id ?? ""}
        onClose={() => setIsImportOpen(false)}
        onImported={async (accountId) => {
          // Make sure the account the lines were imported into is open.
          setCollapsedAccountIds((current) => {
            const next = new Set(current);
            next.delete(accountId);
            return next;
          });
          await reloadTransactions();
        }}
      />

      <TransactionDeleteDialog
        open={isDeleteModalOpen}
        transaction={transactionToDelete}
        linkedTransactions={
          transactionToDelete?.transferId
            ? transactions
                .filter(
                  (transaction) =>
                    transaction.transferId === transactionToDelete.transferId,
                )
                .sort((left, right) => left.amount - right.amount)
            : []
        }
        accounts={accounts}
        loading={deletingId !== null}
        onConfirm={handleDelete}
        onUnlink={handleUnlink}
        onCancel={() => setIsDeleteModalOpen(false)}
        onExited={() => setTransactionToDelete(null)}
      />
    </Box>
  );
}

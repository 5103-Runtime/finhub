import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Container,
  Stack,
  Typography,
} from "@mui/material";
import AddRounded from "@mui/icons-material/AddRounded";
import ReceiptLongRounded from "@mui/icons-material/ReceiptLongRounded";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm, type SubmitHandler } from "react-hook-form";
import Footer from "@app/components/Footer";
import Navbar from "@app/components/Navbar";
import { FormDialog } from "@app/components/Dialog";
import { SelectFormField, TextFormField } from "@app/components/Form";
import { getAccounts, type AccountResponse } from "@app/services/accountsApi";
import {
  createTransaction,
  getTransactions,
  updateTransaction,
  type TransactionResponse,
} from "@app/services/transactionApi";
import {
  getEmptyTransactionForm,
  transactionSchema,
  type TransactionEditFormData,
  type TransactionFormData,
} from "@app/validators/transactionSchemas";
import { applyFieldErrors, getErrorMessage } from "@app/utils/formErrors";
import { TransactionEditRow, TransactionRow } from "./TransactionRow";

import "@app/components/Accounts/Accounts.css";
import "./Transactions.css";

export default function Transactions() {
  const [transactions, setTransactions] = useState<TransactionResponse[]>([]);
  const [accounts, setAccounts] = useState<AccountResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadFailed, setLoadFailed] = useState(false);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [serverError, setServerError] = useState("");

  useEffect(() => {
    let isCurrentRequest = true;

    Promise.all([getTransactions(), getAccounts()])
      .then(([loadedTransactions, loadedAccounts]) => {
        if (isCurrentRequest) {
          setTransactions(loadedTransactions);
          setAccounts(loadedAccounts);
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
  }, []);

  const form = useForm<TransactionFormData>({
    resolver: zodResolver(transactionSchema),
    defaultValues: getEmptyTransactionForm(),
    mode: "onTouched",
  });

  const accountsById = new Map(accounts.map((account) => [account.id, account]));
  const accountOptions = accounts.map((account) => ({
    value: account.id,
    label: account.name,
  }));

  // Refetch after a save so the list keeps the API's ordering (newest first).
  const reloadTransactions = async () => {
    try {
      setTransactions(await getTransactions());
    } catch (requestError) {
      setServerError(
        getErrorMessage(
          requestError,
          "Saved, but unable to refresh the transactions.",
        ),
      );
    }
  };

  const openCreateModal = () => {
    form.reset({
      ...getEmptyTransactionForm(),
      accountId: accounts[0]?.id ?? "",
    });
    setServerError("");
    setEditingId(null);
    setIsModalOpen(true);
  };

  // Clear the error too, so a failed add does not leave a stale page banner.
  const closeModal = () => {
    setIsModalOpen(false);
    setServerError("");
  };

  const onSubmit: SubmitHandler<TransactionFormData> = async (data) => {
    setServerError("");
    try {
      await createTransaction({
        accountId: data.accountId,
        transactionDate: data.transactionDate,
        description: data.description,
        amount: Number(data.amount),
      });
      closeModal();
      await reloadTransactions();
    } catch (requestError) {
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
  ) => {
    await updateTransaction(id, {
      transactionDate: data.transactionDate,
      description: data.description,
      amount: Number(data.amount),
    });
    setEditingId(null);
    await reloadTransactions();
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

    return (
      <Box className="transactions-list">
        <Box className="transactions-head">
          <span>Date</span>
          <span>Description</span>
          <span>Account</span>
          <span className="transactions-head-amount">Amount</span>
          <span aria-hidden="true" />
        </Box>
        {transactions.map((transaction) => {
          const account = accountsById.get(transaction.accountId);

          return editingId === transaction.id ? (
            <TransactionEditRow
              key={transaction.id}
              transaction={transaction}
              account={account}
              onCancel={() => setEditingId(null)}
              onSave={(data) => handleSaveEdit(transaction.id, data)}
            />
          ) : (
            <TransactionRow
              key={transaction.id}
              transaction={transaction}
              account={account}
              onEdit={() => {
                setServerError("");
                setEditingId(transaction.id);
              }}
            />
          );
        })}
      </Box>
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
      >
        <SelectFormField<TransactionFormData>
          name="accountId"
          label="Account"
          options={accountOptions}
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
    </Box>
  );
}
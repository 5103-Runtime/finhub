import { useEffect, useState } from "react";
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Container,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  FormHelperText,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import AddRounded from "@mui/icons-material/AddRounded";
import AccountBalanceRounded from "@mui/icons-material/AccountBalanceRounded";
import DeleteOutlineRounded from "@mui/icons-material/DeleteOutlineRounded";
import EditRounded from "@mui/icons-material/EditRounded";
import Footer from "@app/components/Footer";
import Navbar from "@app/components/Navbar";
import {
  createAccount,
  deleteAccount,
  getAccounts,
  updateAccount,
  type AccountResponse,
} from "@app/services/accountsApi";
import { ApiError } from "@app/services/apiClient";
import {
  accountSchema,
  emptyAccountForm,
  type AccountFormData,
} from "@app/validators/accountSchemas";
import {
  accountTypeLabels,
  accountTypeOrder,
  accountTypeIcons,
  compareAccountsByName,
  formatAccountBalance,
  getAccountTypePluralLabel,
} from "@app/utils/accounts";
import { zodResolver } from "@hookform/resolvers/zod";
import { Controller, useForm, type SubmitHandler } from "react-hook-form";
import "./Accounts.css";

export default function Accounts() {
  const [accounts, setAccounts] = useState<AccountResponse[]>([]);
  const [selectedAccount, setSelectedAccount] =
    useState<AccountResponse | null>(null);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [loading, setLoading] = useState(true);
  const [deletingId, setDeletingId] = useState<string | null>(null);
  const [accountToDelete, setAccountToDelete] =
    useState<AccountResponse | null>(null);
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [serverError, setServerError] = useState("");
  const {
    register,
    control,
    handleSubmit,
    reset,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<AccountFormData>({
    resolver: zodResolver(accountSchema),
    defaultValues: emptyAccountForm,
    mode: "onTouched",
  });

  useEffect(() => {
    let isCurrentRequest = true;

    getAccounts()
      .then((loadedAccounts) => {
        if (isCurrentRequest) {
          setAccounts(loadedAccounts);
        }
      })
      .catch((requestError) => {
        if (isCurrentRequest) {
          setServerError(
            requestError instanceof Error
              ? requestError.message
              : "Unable to load accounts.",
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

  const openCreateModal = () => {
    setSelectedAccount(null);
    reset(emptyAccountForm);
    setServerError("");
    setIsModalOpen(true);
  };

  const openEditModal = (account: AccountResponse) => {
    setSelectedAccount(account);
    reset({
      name: account.name,
      institution: account.institution ?? "",
      accountType: account.accountType,
      currency: account.currency,
    });
    setServerError("");
    setIsModalOpen(true);
  };

  const closeModal = (force = false) => {
    if (force || !isSubmitting) {
      setIsModalOpen(false);
    }
  };

  const handleModalExited = () => {
    setSelectedAccount(null);
    reset(emptyAccountForm);
    setServerError("");
  };

  const onSubmit: SubmitHandler<AccountFormData> = async (data) => {
    setServerError("");
    try {
      if (selectedAccount) {
        const updatedAccount = await updateAccount(selectedAccount.id, {
          name: data.name,
          institution: data.institution || undefined,
        });
        setAccounts((currentAccounts) =>
          currentAccounts.map((account) =>
            account.id === updatedAccount.id ? updatedAccount : account,
          ),
        );
      } else {
        const createdAccount = await createAccount({
          name: data.name,
          institution: data.institution || undefined,
          accountType: data.accountType,
          currency: data.currency,
        });
        setAccounts((currentAccounts) => [...currentAccounts, createdAccount]);
      }
      closeModal(true);
    } catch (requestError) {
      if (requestError instanceof ApiError && requestError.fieldErrors) {
        Object.entries(requestError.fieldErrors).forEach(
          ([field, messages]) => {
            if (field in emptyAccountForm && messages[0]) {
              setError(field as keyof AccountFormData, {
                type: "server",
                message: messages[0],
              });
            }
          },
        );
      }
      setServerError(
        requestError instanceof Error
          ? requestError.message
          : "Unable to save the account.",
      );
    }
  };

  const handleDelete = async () => {
    if (!accountToDelete) {
      return;
    }

    const account = accountToDelete;
    setDeletingId(account.id);
    setServerError("");
    try {
      await deleteAccount(account.id);
      setAccounts((currentAccounts) =>
        currentAccounts.filter((item) => item.id !== account.id),
      );
      if (selectedAccount?.id === account.id) {
        closeModal();
      }
      setIsDeleteModalOpen(false);
    } catch (requestError) {
      setServerError(
        requestError instanceof Error
          ? requestError.message
          : "Unable to delete the account.",
      );
    } finally {
      setDeletingId(null);
    }
  };

  const renderAccountCard = (account: AccountResponse) => {
    const Icon = accountTypeIcons[account.accountType];
    const showAccountType = !shouldGroupAccounts;
    return (
      <Card key={account.id} className="account-management-card">
        <CardContent>
          <Box className={`account-management-icon ${account.accountType}`}>
            <Icon />
          </Box>
          <Box className="account-management-details">
            <Typography className="account-management-name">
              {account.name}
            </Typography>
            <Typography className="account-management-meta">
              {account.institution || "No institution"}
              {showAccountType && (
                <>
                  {" · "}
                  {accountTypeLabels[account.accountType]}
                </>
              )}
            </Typography>
          </Box>
          <Typography className="account-management-balance">
            {formatAccountBalance(account)}
          </Typography>
          <Stack direction="row" className="account-management-actions">
            <Button
              aria-label={`Edit ${account.name}`}
              className="account-icon-button"
              onClick={() => openEditModal(account)}
            >
              <EditRounded />
            </Button>
            <Button
              aria-label={`Delete ${account.name}`}
              className="account-icon-button delete"
              onClick={() => {
                setAccountToDelete(account);
                setIsDeleteModalOpen(true);
              }}
              disabled={deletingId === account.id}
            >
              <DeleteOutlineRounded />
            </Button>
          </Stack>
        </CardContent>
      </Card>
    );
  };

  const accountTypeCount = new Set(
    accounts.map((account) => account.accountType),
  ).size;
  // Grouping is useful only when multiple account types need visual separation
  const shouldGroupAccounts = accounts.length > 1 && accountTypeCount > 1;

  return (
    <Box className="accounts-page">
      <Container maxWidth="xl" className="accounts-shell">
        <Navbar />
        <Box component="main">
          <Box className="accounts-welcome">
            <Box>
              <Typography className="accounts-kicker">YOUR MONEY</Typography>
              <Typography component="h1" className="accounts-title">
                My Accounts
              </Typography>
            </Box>
            {accounts.length > 0 && (
              <Button
                className="accounts-add-button"
                startIcon={<AddRounded />}
                onClick={openCreateModal}
              >
                Add account
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
                Accounts ({accounts.length})
              </Typography>
            </Box>
            {loading ? (
              <Typography className="accounts-empty">
                Loading accounts...
              </Typography>
            ) : accounts.length === 0 ? (
              <Card className="accounts-empty-card">
                <CardContent>
                  <AccountBalanceRounded />
                  <Typography className="empty-title">
                    No accounts yet
                  </Typography>
                  <Typography className="accounts-empty">
                    Add your first account to get started.
                  </Typography>
                  <Button
                    className="accounts-primary-button empty-action"
                    startIcon={<AddRounded />}
                    onClick={openCreateModal}
                  >
                    Add account
                  </Button>
                </CardContent>
              </Card>
            ) : shouldGroupAccounts ? (
              <Stack spacing={4}>
                {accountTypeOrder.map((accountType) => {
                  const groupedAccounts = accounts.filter(
                    (account) => account.accountType === accountType,
                  );
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
                        {[...groupedAccounts]
                          .sort(compareAccountsByName)
                          .map(renderAccountCard)}
                      </Stack>
                    </Box>
                  );
                })}
              </Stack>
            ) : (
              <Stack spacing={1.5}>
                {[...accounts]
                  .sort(compareAccountsByName)
                  .map(renderAccountCard)}
              </Stack>
            )}
          </Box>
        </Box>
        <Footer />
      </Container>

      <Dialog
        open={isModalOpen}
        onClose={() => closeModal()}
        slotProps={{ transition: { onExited: handleModalExited } }}
        fullWidth
        maxWidth="sm"
        className="account-dialog"
      >
        <DialogTitle>
          {selectedAccount ? "Update account" : "Add an account"}
        </DialogTitle>
        <Box component="form" onSubmit={handleSubmit(onSubmit)} noValidate>
          <DialogContent>
            <Stack spacing={2.25} className="account-form">
              <TextField
                label="Account name"
                required
                fullWidth
                error={Boolean(errors.name)}
                helperText={errors.name?.message}
                {...register("name")}
                slotProps={{ htmlInput: { maxLength: 100 } }}
              />
              <TextField
                label="Institution"
                fullWidth
                error={Boolean(errors.institution)}
                helperText={errors.institution?.message}
                {...register("institution")}
                slotProps={{ htmlInput: { maxLength: 100 } }}
              />
              {!selectedAccount && (
                <Stack direction="row" spacing={1.5}>
                  <Controller
                    name="accountType"
                    control={control}
                    render={({ field }) => (
                      <FormControl
                        fullWidth
                        error={Boolean(errors.accountType)}
                      >
                        <InputLabel id="account-type-label">Type</InputLabel>
                        <Select
                          {...field}
                          labelId="account-type-label"
                          label="Type"
                        >
                          {Object.entries(accountTypeLabels).map(
                            ([value, label]) => (
                              <MenuItem key={value} value={value}>
                                {label}
                              </MenuItem>
                            ),
                          )}
                        </Select>
                        <FormHelperText>
                          {errors.accountType?.message}
                        </FormHelperText>
                      </FormControl>
                    )}
                  />
                  <Controller
                    name="currency"
                    control={control}
                    render={({ field }) => (
                      <FormControl fullWidth error={Boolean(errors.currency)}>
                        <InputLabel id="account-currency-label">
                          Currency
                        </InputLabel>
                        <Select
                          {...field}
                          labelId="account-currency-label"
                          label="Currency"
                        >
                          <MenuItem value="CAD">CAD</MenuItem>
                          <MenuItem value="USD">USD</MenuItem>
                        </Select>
                        <FormHelperText>
                          {errors.currency?.message}
                        </FormHelperText>
                      </FormControl>
                    )}
                  />
                </Stack>
              )}
              {serverError && (
                <Alert
                  className="auth-error"
                  severity="error"
                  variant="outlined"
                  role="alert"
                >
                  <Typography className="auth-error-message">
                    {serverError}
                  </Typography>
                </Alert>
              )}
            </Stack>
          </DialogContent>
          <DialogActions>
            <Button
              className="accounts-cancel-button"
              onClick={() => closeModal()}
            >
              Cancel
            </Button>
            <Button
              type="submit"
              className="accounts-primary-button"
              loading={isSubmitting}
            >
              {selectedAccount ? "Save changes" : "Add account"}
            </Button>
          </DialogActions>
        </Box>
      </Dialog>

      <Dialog
        open={isDeleteModalOpen}
        onClose={() => {
          if (deletingId === null) {
            setIsDeleteModalOpen(false);
          }
        }}
        slotProps={{
          transition: { onExited: () => setAccountToDelete(null) },
        }}
        maxWidth="xs"
        fullWidth
        className="account-dialog"
      >
        <DialogTitle>Delete account?</DialogTitle>
        <DialogContent>
          <Typography>
            Are you sure you want to delete{" "}
            <strong>{accountToDelete?.name}</strong>?
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button
            className="accounts-cancel-button"
            onClick={() => setIsDeleteModalOpen(false)}
            disabled={deletingId !== null}
          >
            Cancel
          </Button>
          <Button
            className="accounts-delete-button"
            onClick={() => void handleDelete()}
            loading={deletingId !== null}
          >
            Delete account
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}

import { useEffect, useMemo, useState } from "react";
import {
  Box,
  Button,
  Card,
  CardContent,
  Container,
  Divider,
  Alert,
  Stack,
  Typography,
} from "@mui/material";
import ArrowForwardRounded from "@mui/icons-material/ArrowForwardRounded";
import AutoGraphRounded from "@mui/icons-material/AutoGraphRounded";
import TrendingDownRounded from "@mui/icons-material/TrendingDownRounded";
import TrendingUpRounded from "@mui/icons-material/TrendingUpRounded";
import UploadFileRounded from "@mui/icons-material/UploadFileRounded";
import { Link } from "react-router-dom";
import Footer from "@app/components/Footer";
import Navbar from "@app/components/Navbar";
import { CategoryChip } from "@app/components/Category/CategoryChip";
import { getAccounts, type AccountResponse } from "@app/services/accountsApi";
import { getProfile, type ProfileResponse } from "@app/services/profileApi";
import {
  getTransactions,
  type TransactionResponse,
} from "@app/services/transactionApi";
import {
  getCategories,
  type CategoryResponse,
} from "@app/services/categoriesApi";
import { currencyFormatter, getPercentageChange } from "@app/utils/finance";
import {
  formatSignedCurrency,
  formatTransactionDate,
} from "@app/utils/transactions";
import {
  accountTypeIcons,
  accountTypeLabels,
  formatAccountBalance,
  getAccountBalanceInCad,
  withTransactionBalances,
} from "@app/utils/accounts";
import "./Overview.css";

// TODO: Replace monthly spending and comparison values with overview API data
const totalBalance = 15872.18;
const monthlySpending = 1284.6;
const previousTotalBalance = 14642.22;
const previousMonthlySpending = 1470.15;

const RECENT_TRANSACTION_COUNT = 4;

export default function Overview() {
  const [profile, setProfile] = useState<ProfileResponse | null>(null);
  const [accounts, setAccounts] = useState<AccountResponse[]>([]);
  const [accountsLoading, setAccountsLoading] = useState(true);
  const [accountsError, setAccountsError] = useState("");
  const [accountsById, setAccountsById] = useState<
    Map<string, AccountResponse>
  >(new Map());
  const [recentTransactions, setRecentTransactions] = useState<
    TransactionResponse[]
  >([]);
  const [transactionsLoading, setTransactionsLoading] = useState(true);
  const [transactionError, setTransactionsError] = useState("");
  const [categoriesById, setCategoriesById] = useState<
    Map<string, CategoryResponse>
  >(new Map());

  useEffect(() => {
    let isCurrentRequest = true;

    getProfile()
      .then((currentProfile) => {
        if (isCurrentRequest) {
          setProfile(currentProfile);
        }
      })
      .catch((error) => {
        if (isCurrentRequest) {
          console.error("Unable to load profile for overview:", error);
        }
      });

    return () => {
      isCurrentRequest = false;
    };
  }, []);

  useEffect(() => {
    let isCurrentRequest = true;

    Promise.all([getAccounts(), getTransactions(), getCategories()])
      .then(([loadedAccounts, loadedTransactions, loadedCategories]) => {
        if (isCurrentRequest) {
          setCategoriesById(
            new Map(
              loadedCategories.map((category) => [category.id, category]),
            ),
          );
          const accountsWithBalances = withTransactionBalances(
            loadedAccounts,
            loadedTransactions,
          );
          setAccountsById(
            new Map(
              accountsWithBalances.map((account) => [account.id, account]),
            ),
          );
          // API returns newest first.
          setRecentTransactions(
            loadedTransactions.slice(0, RECENT_TRANSACTION_COUNT),
          );
          setAccounts(
            // Keep the overview focused by showing the three highest balances.
            [...accountsWithBalances]
              .sort(
                (left, right) =>
                  getAccountBalanceInCad(right) - getAccountBalanceInCad(left),
              )
              .slice(0, 3),
          );
        }
      })
      .catch((error) => {
        if (isCurrentRequest) {
          console.error("Unable to load accounts for overview:", error);
          const message =
            error instanceof Error
              ? error.message
              : "Unable to load overview data.";
          setAccountsError(message);
          setTransactionsError(message);
        }
      })
      .finally(() => {
        if (isCurrentRequest) {
          setAccountsLoading(false);
          setTransactionsLoading(false);
        }
      });

    return () => {
      isCurrentRequest = false;
    };
  }, []);

  const formattedDate = useMemo(
    () =>
      new Intl.DateTimeFormat("en-CA", {
        weekday: "long",
        month: "long",
        day: "numeric",
        year: "numeric",
      }).format(new Date()),
    [],
  );
  const spendingPercentage = (monthlySpending / totalBalance) * 100;
  const totalBalanceChange = getPercentageChange(
    totalBalance,
    previousTotalBalance,
  );
  const monthlySpendingChange = getPercentageChange(
    monthlySpending,
    previousMonthlySpending,
  );
  const totalBalanceChangeIsPositive = totalBalanceChange >= 0;
  const monthlySpendingDecreased = monthlySpendingChange < 0;

  return (
    <Box className="overview-page">
      <Container maxWidth="xl" className="overview-shell">
        <Navbar />

        <Box component="main" id="overview">
          <Box className="overview-welcome">
            <Box>
              <Typography className="overview-kicker">
                {formattedDate}
              </Typography>
              <Typography component="h1" className="overview-title">
                Welcome back
                {profile?.firstName ? (
                  <>
                    , <em>{profile.firstName}</em>
                  </>
                ) : (
                  ""
                )}
              </Typography>
              <Typography className="overview-subtitle">
                Here's a quick look at your finances.
              </Typography>
            </Box>
            <Button
              component={Link}
              to="/transactions"
              className="import-statement-button"
              startIcon={<UploadFileRounded />}
              endIcon={<ArrowForwardRounded />}
            >
              Import statement
            </Button>
          </Box>

          <Box className="overview-metrics">
            <Card className="metric-card metric-card-featured">
              <CardContent>
                <Typography className="metric-label">TOTAL BALANCE</Typography>
                <Typography className="metric-value">
                  {currencyFormatter.format(totalBalance)}
                </Typography>
                <Stack
                  direction="row"
                  spacing={0.75}
                  className={`metric-change ${totalBalanceChangeIsPositive ? "metric-change-up" : "metric-change-down"}`}
                >
                  {totalBalanceChangeIsPositive ? (
                    <TrendingUpRounded />
                  ) : (
                    <TrendingDownRounded />
                  )}
                  <Typography component="span">
                    {Math.abs(totalBalanceChange).toFixed(1)}% from previous
                    period
                  </Typography>
                </Stack>
              </CardContent>
            </Card>
            <Card className="metric-card">
              <CardContent>
                <Typography className="metric-label">
                  SPENDING THIS MONTH
                </Typography>
                <Typography className="metric-value">
                  {currencyFormatter.format(monthlySpending)}
                </Typography>
                <Stack
                  direction="row"
                  spacing={0.75}
                  className={`metric-change ${monthlySpendingDecreased ? "metric-change-up" : "metric-change-down"}`}
                >
                  {monthlySpendingDecreased ? (
                    <TrendingDownRounded />
                  ) : (
                    <TrendingUpRounded />
                  )}
                  <Typography component="span">
                    {Math.abs(monthlySpendingChange).toFixed(1)}% from previous
                    period
                  </Typography>
                </Stack>
              </CardContent>
            </Card>
          </Box>

          <Box className="overview-grid">
            <Box
              component="section"
              id="accounts"
              className="overview-section accounts-section"
            >
              <Box className="section-heading">
                <Box>
                  <Typography className="section-kicker">YOUR MONEY</Typography>
                  <Typography component="h2" className="section-title">
                    Accounts
                  </Typography>
                </Box>
                {accounts.length > 0 && (
                  <Button
                    component={Link}
                    to="/accounts"
                    className="text-action"
                    endIcon={<ArrowForwardRounded />}
                  >
                    View all accounts
                  </Button>
                )}
              </Box>
              {accountsError ? (
                <Alert
                  className="overview-accounts-error"
                  severity="error"
                  variant="outlined"
                >
                  {accountsError}
                </Alert>
              ) : accountsLoading ? (
                <Typography className="overview-accounts-status">
                  Loading accounts...
                </Typography>
              ) : accounts.length === 0 ? (
                <Card className="overview-accounts-empty">
                  <CardContent>
                    <Typography className="overview-accounts-empty-title">
                      No accounts yet
                    </Typography>
                    <Typography className="overview-accounts-status">
                      Add your first account to get started.
                    </Typography>
                    <Button
                      component={Link}
                      to="/accounts"
                      className="text-action"
                      endIcon={<ArrowForwardRounded />}
                    >
                      Add account
                    </Button>
                  </CardContent>
                </Card>
              ) : (
                <Stack spacing={1.5}>
                  {accounts.map((account) => {
                    const Icon = accountTypeIcons[account.accountType];
                    return (
                      <Card
                        component={Link}
                        to={`/transactions?accountId=${account.id}`}
                        className="account-card"
                        key={account.id}
                      >
                        <CardContent>
                          <Box
                            className={`account-icon ${account.accountType.replace("_", "-")}`}
                          >
                            <Icon />
                          </Box>
                          <Box className="account-details">
                            <Typography className="account-name">
                              {account.name}
                            </Typography>
                            <Typography className="account-type">
                              {accountTypeLabels[account.accountType]}
                            </Typography>
                          </Box>
                          <Typography className="account-amount">
                            {formatAccountBalance(account)}
                          </Typography>
                        </CardContent>
                      </Card>
                    );
                  })}
                </Stack>
              )}
            </Box>

            <Box
              component="section"
              id="insights"
              className="overview-section insight-section"
            >
              <Box className="section-heading">
                <Box>
                  <Typography className="section-kicker">
                    A LITTLE INSIGHT
                  </Typography>
                  <Typography component="h2" className="section-title">
                    Spending pulse
                  </Typography>
                </Box>
              </Box>

              {/* TODO: Replace with dynamic insights once the insights API is available */}
              <Card className="insight-card">
                <CardContent>
                  <Box className="insight-visual">
                    <Box className="pulse-ring">
                      <AutoGraphRounded />
                    </Box>
                  </Box>
                  <Typography className="insight-title">
                    You&apos;re spending less on essentials
                  </Typography>
                  <Typography className="insight-copy">
                    Your essential spending is {spendingPercentage.toFixed(1)}%
                    of your total balance. Nice work keeping things steady.
                  </Typography>
                  <Button
                    component={Link}
                    to="/insights"
                    className="insight-link"
                    endIcon={<ArrowForwardRounded />}
                  >
                    Explore insights
                  </Button>
                </CardContent>
              </Card>
            </Box>
          </Box>

          <Box
            component="section"
            id="transactions"
            className="overview-section transactions-section"
          >
            <Box className="section-heading">
              <Box>
                <Typography className="section-kicker">KEEP TRACK</Typography>
                <Typography component="h2" className="section-title">
                  Recent transactions
                </Typography>
              </Box>
              <Button
                component={Link}
                to="/transactions"
                className="text-action"
                endIcon={<ArrowForwardRounded />}
              >
                View all transactions
              </Button>
            </Box>
            <Card className="transactions-card">
              {transactionError ? (
                <Alert
                  className="overview-accounts-error"
                  severity="error"
                  variant="outlined"
                >
                  {transactionError}
                </Alert>
              ) : transactionsLoading ? (
                <CardContent>
                  <Typography className="overview-accounts-status">
                    Loading transactions...
                  </Typography>
                </CardContent>
              ) : recentTransactions.length === 0 ? (
                <CardContent>
                  <Typography className="overview-accounts-empty-title">
                    No transactions yet
                  </Typography>
                  <Typography className="overview-accounts-status">
                    Add your first transaction to get started.
                  </Typography>
                  <Button
                    component={Link}
                    to="/transactions"
                    className="text-action"
                    endIcon={<ArrowForwardRounded />}
                  >
                    Add transaction
                  </Button>
                </CardContent>
              ) : (
                recentTransactions.map((transaction, index) => {
                  const account = accountsById.get(transaction.accountId);
                  const isIncome = transaction.amount > 0;
                  const category = transaction.categoryId
                    ? categoriesById.get(transaction.categoryId)
                    : undefined;
                  const AccountIcon = account
                    ? accountTypeIcons[account.accountType]
                    : null;

                  return (
                    <Box key={transaction.id} className="transaction-row">
                      <Box
                        className={`transaction-icon ${account?.accountType.replace("_", "-") ?? ""}`}
                      >
                        {AccountIcon ? (
                          <AccountIcon />
                        ) : (
                          <Typography>
                            {transaction.description.charAt(0).toUpperCase()}
                          </Typography>
                        )}
                      </Box>
                      <Box className="transaction-details">
                        <Typography className="transaction-merchant">
                          {transaction.description}
                        </Typography>
                        <Typography className="transaction-meta">
                          {account?.name ?? "Unknown account"} ·{" "}
                          {formatTransactionDate(transaction.transactionDate)}
                        </Typography>
                      </Box>
                      <Box className="transaction-category-desktop">
                        {category && <CategoryChip name={category.name} />}
                      </Box>
                      <Typography
                        className={`transaction-amount ${isIncome ? "positive" : ""}`}
                      >
                        {formatSignedCurrency(
                          transaction.amount,
                          account?.currency ?? "CAD",
                        )}
                      </Typography>
                      {index < recentTransactions.length - 1 && <Divider />}
                    </Box>
                  );
                })
              )}
            </Card>
          </Box>
        </Box>
        <Footer />
      </Container>
    </Box>
  );
}

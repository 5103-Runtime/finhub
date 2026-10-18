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
import { getAccounts, type AccountResponse } from "@app/services/accountsApi";
import { getProfile, type ProfileResponse } from "@app/services/profileApi";
import { currencyFormatter, getPercentageChange } from "@app/utils/finance";
import {
  accountTypeIcons,
  accountTypeLabels,
  formatAccountBalance,
  getAccountBalanceInCad,
} from "@app/utils/accounts";
import "./Overview.css";

// TODO: Replace account balances and monthly spending with overview API data
const totalBalance = 15872.18;
const monthlySpending = 1284.6;
const previousTotalBalance = 14642.22;
const previousMonthlySpending = 1470.15;

// TODO: Replace with dynamic transactions once the transactions API is available
const transactions = [
  {
    merchant: "Costco",
    category: "Groceries",
    date: "Today, 11:42 AM",
    amount: -254.47,
    color: "groceries",
  },
  {
    merchant: "Rogers",
    category: "Bills & Utilities",
    date: "Yesterday",
    amount: -85.49,
    color: "utilities",
  },
  {
    merchant: "Paycheque Deposit",
    category: "Income",
    date: "Oct 10, 2026",
    amount: 2450,
    color: "income",
  },
  {
    merchant: "London Transit",
    category: "Transport",
    date: "Oct 09, 2026",
    amount: -48,
    color: "transport",
  },
];

export default function Overview() {
  const [profile, setProfile] = useState<ProfileResponse | null>(null);
  const [accounts, setAccounts] = useState<AccountResponse[]>([]);
  const [accountsLoading, setAccountsLoading] = useState(true);
  const [accountsError, setAccountsError] = useState("");

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

    getAccounts()
      .then((loadedAccounts) => {
        if (isCurrentRequest) {
          setAccounts(
            // Keep the overview focused by showing the three highest balances.
            [...loadedAccounts]
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
          setAccountsError(
            error instanceof Error ? error.message : "Unable to load accounts.",
          );
        }
      })
      .finally(() => {
        if (isCurrentRequest) {
          setAccountsLoading(false);
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
                    // TODO: Replace with the account detail route when available
                    return (
                      <Card
                        component={Link}
                        to="/accounts"
                        className="account-card"
                        key={account.name}
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
              {transactions.map((transaction, index) => (
                <Box
                  key={`${transaction.merchant}-${transaction.date}`}
                  className="transaction-row"
                >
                  <Box className={`transaction-icon ${transaction.color}`}>
                    <Typography>{transaction.merchant.charAt(0)}</Typography>
                  </Box>
                  <Box className="transaction-details">
                    <Typography className="transaction-merchant">
                      {transaction.merchant}
                    </Typography>
                    <Typography className="transaction-meta">
                      {transaction.category} · {transaction.date}
                    </Typography>
                  </Box>
                  <Typography
                    className={`transaction-amount ${transaction.amount > 0 ? "positive" : ""}`}
                  >
                    {currencyFormatter.format(transaction.amount)}
                  </Typography>
                  {index < transactions.length - 1 && <Divider />}
                </Box>
              ))}
            </Card>
          </Box>
        </Box>
        <Footer />
      </Container>
    </Box>
  );
}

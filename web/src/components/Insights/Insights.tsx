import { useEffect, useMemo, useState } from "react";
import {
  Alert,
  Box,
  Container,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Typography,
} from "@mui/material";
import Navbar from "@app/components/Navbar";
import Footer from "@app/components/Footer";
import {
  getOverview,
  type OverviewDashboardResponse,
} from "@app/services/overviewApi";
import { formatDashboardCurrency } from "@app/utils/finance";
import {
  CashFlowChart,
  CategorySpendingChart,
  NetCashFlowChart,
} from "../FinancialCharts";
import "./Insights.css";

const INSIGHTS_MONTHS = 6;

export default function Insights() {
  const [dashboard, setDashboard] = useState<OverviewDashboardResponse | null>(
    null,
  );
  const [selectedCurrency, setSelectedCurrency] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    let isCurrentRequest = true;

    getOverview(INSIGHTS_MONTHS)
      .then((loadedDashboard) => {
        if (!isCurrentRequest) {
          return;
        }

        setDashboard(loadedDashboard);
        setSelectedCurrency(loadedDashboard.currencies[0] ?? "CAD");
      })
      .catch((requestError) => {
        if (isCurrentRequest) {
          console.error("Unable to load insights data:", requestError);
          setError(
            requestError instanceof Error
              ? requestError.message
              : "Unable to load insights data.",
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

  const currency = selectedCurrency || dashboard?.currencies[0] || "CAD";
  const trend = useMemo(
    () =>
      dashboard?.monthlyTrend.filter((point) => point.currency === currency) ??
      [],
    [currency, dashboard],
  );
  const categories = useMemo(
    () =>
      dashboard?.categorySpending.filter(
        (category) => category.currency === currency,
      ) ?? [],
    [currency, dashboard],
  );
  const formatValue = (value: number) =>
    formatDashboardCurrency(value, currency);

  return (
    <Box className="insights-page">
      <Container maxWidth="xl" className="insights-shell">
        <Navbar />
        <Box component="main" id="insights">
          <Box className="insights-header">
            <Box>
              <Typography className="overview-kicker">
                FINANCIAL HEALTH
              </Typography>
              <Typography component="h1" className="overview-title">
                Insights
              </Typography>
            </Box>
            {dashboard?.currencies.length ? (
              <FormControl className="insights-currency-control" size="small">
                <InputLabel id="insights-currency-label">Currency</InputLabel>
                <Select
                  labelId="insights-currency-label"
                  value={currency}
                  label="Currency"
                  onChange={(event) => setSelectedCurrency(event.target.value)}
                >
                  {dashboard.currencies.map((option) => (
                    <MenuItem key={option} value={option}>
                      {option}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            ) : null}
          </Box>

          {error ? (
            <Alert className="insights-status" severity="error">
              {error}
            </Alert>
          ) : loading ? (
            <Typography className="insights-status insights-loading">
              Loading financial insights...
            </Typography>
          ) : (
            <>
              <Box className="insights-period">
                <Typography>
                  Showing {currency} data from {dashboard?.periodStart} to{" "}
                  {dashboard?.periodEnd}.
                </Typography>
              </Box>
              <Box className="insights-chart-grid">
                <CashFlowChart
                  points={trend}
                  currency={currency}
                  formatValue={formatValue}
                />
                <CategorySpendingChart
                  categories={categories}
                  currency={currency}
                  formatValue={formatValue}
                />
                <NetCashFlowChart
                  points={trend}
                  currency={currency}
                  formatValue={formatValue}
                />
              </Box>
              <Box component="section" className="system-insights-section">
                <Typography className="section-kicker">
                  SYSTEM INSIGHTS
                </Typography>
                <Typography component="h2" className="section-title">
                  Personalized observations are coming soon
                </Typography>
                <Typography className="system-insights-copy">
                  Automated spending insights and alerts will appear here when
                  the insights rules engine is implemented.
                </Typography>
              </Box>
            </>
          )}
        </Box>
        <Footer />
      </Container>
    </Box>
  );
}

import { useEffect, useMemo, useState } from "react";
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Container,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Switch,
  Tab,
  Tabs,
  TextField,
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
import {
  getInsightPreferences,
  getAlertHistory,
  getUnreadAlerts,
  markAlertRead,
  notifyInsightsChanged,
  updateInsightPreference,
  type InsightPreferenceResponse,
} from "@app/services/insightsApi";
import InsightAlertList from "@app/components/InsightAlerts";
import "./Insights.css";

const INSIGHTS_MONTHS = 6;
const INSIGHT_PREFERENCE_TYPES = [
  "category_spike",
  "unusual_purchase",
] as const;
const DEFAULT_CATEGORY_SPIKE_THRESHOLD = 50;
const DEFAULT_UNUSUAL_PURCHASE_THRESHOLD = 3;

export default function Insights() {
  const [dashboard, setDashboard] = useState<OverviewDashboardResponse | null>(
    null,
  );
  const [selectedCurrency, setSelectedCurrency] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [alertView, setAlertView] = useState<"unread" | "history">("unread");
  const [pendingAlertView, setPendingAlertView] = useState<
    "unread" | "history" | null
  >(null);
  const [insightRefreshVersion, setInsightRefreshVersion] = useState(0);
  const [unreadCount, setUnreadCount] = useState(0);
  const [preferences, setPreferences] = useState<InsightPreferenceResponse[]>(
    [],
  );
  const [preferencesLoading, setPreferencesLoading] = useState(true);

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

  useEffect(() => {
    let isCurrentRequest = true;

    getUnreadAlerts(INSIGHTS_MONTHS)
      .then((alerts) => {
        if (isCurrentRequest) {
          setUnreadCount(alerts.length);
        }
      })
      .catch(() => {
        if (isCurrentRequest) {
          setUnreadCount(0);
        }
      });

    return () => {
      isCurrentRequest = false;
    };
  }, [insightRefreshVersion, alertView]);

  useEffect(() => {
    let isCurrentRequest = true;

    getInsightPreferences()
      .then((loadedPreferences) => {
        if (isCurrentRequest) {
          setPreferences(loadedPreferences);
        }
      })
      .catch(() => undefined)
      .finally(() => {
        if (isCurrentRequest) {
          setPreferencesLoading(false);
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

  const mergePreference = (updated: InsightPreferenceResponse) => {
    setPreferences((currentPreferences) => {
      const exists = currentPreferences.some(
        (preference) => preference.insightType === updated.insightType,
      );
      return exists
        ? currentPreferences.map((preference) =>
            preference.insightType === updated.insightType
              ? updated
              : preference,
          )
        : [...currentPreferences, updated];
    });
  };

  const switchAlertView = async (nextView: "unread" | "history") => {
    if (nextView === alertView || pendingAlertView !== null) {
      return;
    }

    setPendingAlertView(nextView);
    try {
      if (nextView === "unread") {
        await getUnreadAlerts(INSIGHTS_MONTHS);
      } else {
        await getAlertHistory(INSIGHTS_MONTHS);
      }
      setAlertView(nextView);
      setInsightRefreshVersion((version) => version + 1);
    } catch (requestError) {
      setError(
        requestError instanceof Error
          ? requestError.message
          : "Unable to load financial insights.",
      );
    } finally {
      setPendingAlertView(null);
    }
  };

  const readAllUnread = async () => {
    if (alertView !== "unread") {
      return;
    }

    try {
      const alerts = await getUnreadAlerts(INSIGHTS_MONTHS);
      await Promise.all(
        alerts.map((alert) =>
          markAlertRead({
            insightType: alert.insightType,
            categoryId: alert.categoryId,
            transactionId: alert.transactionId,
            currency:
              alert.insightType === "category_spike"
                ? alert.currency
                : undefined,
            periodStart: alert.periodStart,
            amount: alert.amount,
          }),
        ),
      );
      notifyInsightsChanged();
      setInsightRefreshVersion((version) => version + 1);
    } catch (requestError) {
      setError(
        requestError instanceof Error
          ? requestError.message
          : "Unable to mark all insights as read.",
      );
    }
  };

  const setPreferenceEnabled = async (
    preference: InsightPreferenceResponse,
    isEnabled: boolean,
  ) => {
    try {
      const updated = await updateInsightPreference(preference.insightType, {
        isEnabled,
        threshold: preference.threshold,
      });
      mergePreference(updated);
    } catch {
      return;
    }
  };

  const setPreferenceThreshold = async (
    insightType: "category_spike" | "unusual_purchase",
    preference: InsightPreferenceResponse | undefined,
    thresholdText: string,
  ) => {
    const threshold = Number(thresholdText);
    if (!Number.isFinite(threshold) || threshold <= 0) {
      return;
    }

    try {
      const updated = await updateInsightPreference(insightType, {
        isEnabled: preference?.isEnabled ?? true,
        threshold,
      });
      mergePreference(updated);
    } catch {
      return;
    }
  };

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
                <InputLabel id="insights-currency-label">
                  Chart currency
                </InputLabel>
                <Select
                  labelId="insights-currency-label"
                  value={currency}
                  label="Chart currency"
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
              <Box component="section" className="system-insights-section">
                <Typography className="section-kicker">
                  SYSTEM INSIGHTS
                </Typography>
                <Typography className="system-insights-description">
                  Alerts use the currency of their own account and transaction.
                </Typography>
                <Box className="insights-alert-toolbar">
                  <Tabs
                    className="insights-alert-tabs"
                    value={alertView}
                    onChange={(_, value: "unread" | "history") =>
                      void switchAlertView(value)
                    }
                    aria-busy={pendingAlertView !== null}
                  >
                    <Tab
                      value="unread"
                      label="Unread"
                      disabled={pendingAlertView !== null}
                    />
                    <Tab
                      value="history"
                      label="History"
                      disabled={pendingAlertView !== null}
                    />
                  </Tabs>
                  {pendingAlertView !== null ? (
                    <CircularProgress
                      className="insights-alert-loading"
                      size={18}
                      thickness={5}
                    />
                  ) : alertView === "unread" && unreadCount > 0 ? (
                    <Button
                      size="small"
                      className="insights-read-all"
                      onClick={() => void readAllUnread()}
                    >
                      Read all
                    </Button>
                  ) : null}
                </Box>
                <Box className="insight-alert-scroll">
                  <InsightAlertList
                    view={alertView}
                    refreshVersion={insightRefreshVersion}
                  />
                </Box>
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
              <Box component="section" className="insight-preferences-section">
                <Typography className="section-kicker">
                  ALERT SETTINGS
                </Typography>
                <Typography component="h2" className="section-title">
                  Insight preferences
                </Typography>
                {preferencesLoading ? (
                  <Typography className="insights-loading">
                    Loading alert preferences...
                  </Typography>
                ) : (
                  <Box className="insight-preferences-grid">
                    {INSIGHT_PREFERENCE_TYPES.map((insightType) => {
                      const preference = preferences.find(
                        (item) => item.insightType === insightType,
                      );
                      const isEnabled = preference?.isEnabled ?? true;
                      return (
                        <Box
                          key={insightType}
                          className="insight-preference-row"
                        >
                          <Box>
                            <Typography className="insight-preference-title">
                              {insightType === "category_spike"
                                ? "Category spending spikes"
                                : "Unusual purchases"}
                            </Typography>
                            <Typography className="insight-preference-copy">
                              {insightType === "category_spike"
                                ? "Alerts when category spending exceeds its recent average."
                                : "Alerts when a purchase is unusually large for its category."}
                            </Typography>
                          </Box>
                          <Box className="insight-preference-controls">
                            <Box
                              className={`insight-preference-stepper ${
                                isEnabled ? "" : "disabled"
                              }`}
                            >
                              <Box className="insight-preference-stepper-body">
                                <TextField
                                  className="insight-preference-threshold"
                                  type="number"
                                  size="small"
                                  disabled={!isEnabled}
                                  label={
                                    insightType === "category_spike"
                                      ? "Above average"
                                      : "Purchase median"
                                  }
                                  value={
                                    preference?.threshold ??
                                    (insightType === "category_spike"
                                      ? DEFAULT_CATEGORY_SPIKE_THRESHOLD
                                      : DEFAULT_UNUSUAL_PURCHASE_THRESHOLD)
                                  }
                                  slotProps={{
                                    htmlInput: {
                                      min: 0.01,
                                      max: 9999.99,
                                      step: 1,
                                      inputMode: "decimal",
                                    },
                                  }}
                                  onChange={(event) =>
                                    void setPreferenceThreshold(
                                      insightType,
                                      preference,
                                      event.target.value.replace(
                                        /[^0-9.]/g,
                                        "",
                                      ),
                                    )
                                  }
                                  onKeyDown={(event) => {
                                    if (
                                      ["-", "+", "e", "E"].includes(event.key)
                                    ) {
                                      event.preventDefault();
                                    }
                                  }}
                                  onPaste={(event) => {
                                    const pastedText =
                                      event.clipboardData.getData("text");
                                    if (/[^0-9.]/.test(pastedText)) {
                                      event.preventDefault();
                                    }
                                  }}
                                />
                                <Typography
                                  className="insight-preference-threshold-unit"
                                  component="span"
                                >
                                  {insightType === "category_spike" ? "%" : "×"}
                                </Typography>
                                <Box className="insight-preference-step-buttons">
                                  <Button
                                    size="small"
                                    disabled={!isEnabled}
                                    aria-label="Increase threshold"
                                    onClick={() => {
                                      const current =
                                        preference?.threshold ??
                                        (insightType === "category_spike"
                                          ? DEFAULT_CATEGORY_SPIKE_THRESHOLD
                                          : DEFAULT_UNUSUAL_PURCHASE_THRESHOLD);
                                      void setPreferenceThreshold(
                                        insightType,
                                        preference,
                                        String(current + 1),
                                      );
                                    }}
                                  >
                                    +
                                  </Button>
                                  <Button
                                    size="small"
                                    disabled={!isEnabled}
                                    aria-label="Decrease threshold"
                                    onClick={() => {
                                      const current =
                                        preference?.threshold ??
                                        (insightType === "category_spike"
                                          ? DEFAULT_CATEGORY_SPIKE_THRESHOLD
                                          : DEFAULT_UNUSUAL_PURCHASE_THRESHOLD);
                                      void setPreferenceThreshold(
                                        insightType,
                                        preference,
                                        String(Math.max(0.01, current - 1)),
                                      );
                                    }}
                                  >
                                    −
                                  </Button>
                                </Box>
                              </Box>
                            </Box>
                            <Switch
                              checked={isEnabled}
                              disabled={preferencesLoading}
                              onChange={(_, checked) =>
                                void (preference
                                  ? setPreferenceEnabled(preference, checked)
                                  : updateInsightPreference(insightType, {
                                      isEnabled: checked,
                                      threshold: null,
                                    })
                                      .then((updated) =>
                                        mergePreference(updated),
                                      )
                                      .catch(() => undefined))
                              }
                            />
                          </Box>
                        </Box>
                      );
                    })}
                  </Box>
                )}
              </Box>
            </>
          )}
        </Box>
        <Footer />
      </Container>
    </Box>
  );
}

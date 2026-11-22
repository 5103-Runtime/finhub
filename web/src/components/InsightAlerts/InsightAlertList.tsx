import { useEffect, useState } from "react";
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  Typography,
} from "@mui/material";
import ArrowForwardRounded from "@mui/icons-material/ArrowForwardRounded";
import AutoGraphRounded from "@mui/icons-material/AutoGraphRounded";
import ErrorOutlineRounded from "@mui/icons-material/ErrorOutlineRounded";
import WarningAmberRounded from "@mui/icons-material/WarningAmberRounded";
import { Link } from "react-router-dom";
import {
  getCategories,
  type CategoryResponse,
} from "@app/services/categoriesApi";
import {
  dismissAlert,
  getAlertHistory,
  getUnreadAlerts,
  markAlertRead,
  notifyInsightsChanged,
  INSIGHTS_CHANGED_EVENT,
  type InsightAlertResponse,
  type UpdateInsightStateRequest,
} from "@app/services/insightsApi";
import { formatDashboardCurrency } from "@app/utils/finance";
import "./InsightAlertList.css";

interface InsightAlertListProps {
  view: "unread" | "history";
  maxItems?: number;
  compact?: boolean;
  onCountChange?: (count: number) => void;
  refreshVersion?: number;
}

export default function InsightAlertList({
  view,
  maxItems,
  compact = false,
  onCountChange,
  refreshVersion = 0,
}: InsightAlertListProps) {
  const [alerts, setAlerts] = useState<InsightAlertResponse[]>([]);
  const [categories, setCategories] = useState<Map<string, CategoryResponse>>(
    new Map(),
  );
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [changeVersion, setChangeVersion] = useState(0);

  useEffect(() => {
    const handleInsightChange = () =>
      setChangeVersion((current) => current + 1);
    const handleWindowFocus = () => setChangeVersion((current) => current + 1);

    window.addEventListener(INSIGHTS_CHANGED_EVENT, handleInsightChange);
    window.addEventListener("focus", handleWindowFocus);

    return () => {
      window.removeEventListener(INSIGHTS_CHANGED_EVENT, handleInsightChange);
      window.removeEventListener("focus", handleWindowFocus);
    };
  }, []);

  useEffect(() => {
    let isCurrentRequest = true;

    const alertsRequest =
      view === "unread" ? getUnreadAlerts(6) : getAlertHistory(6);

    Promise.all([alertsRequest, getCategories()])
      .then(([loadedAlerts, loadedCategories]) => {
        if (!isCurrentRequest) {
          return;
        }

        setAlerts(loadedAlerts);
        setCategories(
          new Map(loadedCategories.map((category) => [category.id, category])),
        );
        onCountChange?.(loadedAlerts.length);
      })
      .catch((requestError) => {
        if (isCurrentRequest) {
          const message =
            requestError instanceof Error
              ? requestError.message
              : "Unable to load financial alerts.";
          setError(message);
          onCountChange?.(0);
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
  }, [changeVersion, onCountChange, refreshVersion, view]);

  const updateState = async (
    alert: InsightAlertResponse,
    action: "read" | "dismiss",
  ) => {
    const request: UpdateInsightStateRequest = {
      insightType: alert.insightType,
      categoryId: alert.categoryId,
      transactionId: alert.transactionId,
      currency:
        alert.insightType === "category_spike" ? alert.currency : undefined,
      periodStart: alert.periodStart,
    };

    try {
      if (action === "read") {
        await markAlertRead(request);
      } else {
        await dismissAlert(request);
      }
      notifyInsightsChanged();

      setAlerts((currentAlerts) => {
        const nextAlerts =
          view === "unread"
            ? currentAlerts.filter(
                (currentAlert) => currentAlert.id !== alert.id,
              )
            : currentAlerts.map((currentAlert) =>
                currentAlert.id === alert.id
                  ? {
                      ...currentAlert,
                      isRead: action === "read",
                    }
                  : currentAlert,
              );
        onCountChange?.(nextAlerts.length);
        return nextAlerts;
      });
    } catch (requestError) {
      setError(
        requestError instanceof Error
          ? requestError.message
          : "Unable to update the financial alert.",
      );
    }
  };

  const visibleAlerts = maxItems ? alerts.slice(0, maxItems) : alerts;

  if (loading) {
    return (
      <Typography className="insight-alert-status">
        Loading financial alerts...
      </Typography>
    );
  }

  if (error) {
    return (
      <Alert
        className="insight-alert-error"
        severity="error"
        variant="outlined"
      >
        {error}
      </Alert>
    );
  }

  if (visibleAlerts.length === 0) {
    return (
      <Box className={`insight-alert-empty ${compact ? "compact" : ""}`}>
        {compact ? (
          <Box className="insight-alert-empty-icon">
            <AutoGraphRounded />
          </Box>
        ) : null}
        <Typography className="insight-alert-empty-title">
          {view === "unread" ? "You are all caught up" : "No insight history"}
        </Typography>
        <Typography className="insight-alert-empty-copy">
          {view === "unread"
            ? "No unread financial alerts."
            : "Dismissed and read alerts will appear here."}
        </Typography>
        {compact ? (
          <Button
            component={Link}
            to="/insights"
            className="insight-alert-link"
            endIcon={<ArrowForwardRounded />}
          >
            Explore insights
          </Button>
        ) : null}
      </Box>
    );
  }

  return (
    <Box
      className={`insight-alert-list ${compact ? "compact" : ""}`}
      aria-label={view === "unread" ? "Unread insights" : "Insight history"}
    >
      {visibleAlerts.map((alert) => {
        const categoryName =
          alert.categoryId && categories.get(alert.categoryId)?.name;
        const title =
          alert.insightType === "category_spike"
            ? `${categoryName ?? "Category"} spending spike`
            : "Unusual purchase";
        const alertCurrency = alert.currency ?? "CAD";
        const SeverityIcon =
          alert.severity === "critical"
            ? ErrorOutlineRounded
            : WarningAmberRounded;

        return (
          <Card key={alert.id} className="insight-alert-card">
            <CardContent>
              <Box className="insight-alert-heading">
                <Box className={`insight-alert-icon ${alert.severity}`}>
                  <SeverityIcon />
                </Box>
                <Chip
                  className={`insight-alert-severity ${alert.severity}`}
                  label={alert.severity}
                  size="small"
                />
              </Box>
              <Typography component="h3" className="insight-alert-title">
                {title}
              </Typography>
              <Typography className="insight-alert-copy">
                {formatDashboardCurrency(alert.amount, alertCurrency)}
                {" compared with "}
                {formatDashboardCurrency(alert.baseline, alertCurrency)}
                {" baseline"}
              </Typography>
              {view === "unread" ? (
                <Box className="insight-alert-actions">
                  <Button
                    size="small"
                    onClick={() => void updateState(alert, "read")}
                  >
                    Read
                  </Button>
                  <Button
                    size="small"
                    color="inherit"
                    onClick={() => void updateState(alert, "dismiss")}
                  >
                    Dismiss
                  </Button>
                </Box>
              ) : (
                <Typography className="insight-alert-read-state">
                  {alert.isRead ? "Read" : "Unread"}
                </Typography>
              )}
            </CardContent>
          </Card>
        );
      })}
    </Box>
  );
}

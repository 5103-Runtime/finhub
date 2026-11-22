import { useEffect, useRef, useState } from "react";
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  IconButton,
  Typography,
} from "@mui/material";
import ArrowForwardRounded from "@mui/icons-material/ArrowForwardRounded";
import ChevronLeftRounded from "@mui/icons-material/ChevronLeftRounded";
import ChevronRightRounded from "@mui/icons-material/ChevronRightRounded";
import AutoGraphRounded from "@mui/icons-material/AutoGraphRounded";
import ErrorOutlineRounded from "@mui/icons-material/ErrorOutlineRounded";
import WarningAmberRounded from "@mui/icons-material/WarningAmberRounded";
import { Link, useNavigate } from "react-router-dom";
import {
  getCategories,
  type CategoryResponse,
} from "@app/services/categoriesApi";
import {
  getTransactions,
  type TransactionResponse,
} from "@app/services/transactionApi";
import { getAccounts, type AccountResponse } from "@app/services/accountsApi";
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
  const navigate = useNavigate();
  const [alerts, setAlerts] = useState<InsightAlertResponse[]>([]);
  const [categories, setCategories] = useState<Map<string, CategoryResponse>>(
    new Map(),
  );
  const [transactions, setTransactions] = useState<
    Map<string, TransactionResponse>
  >(new Map());
  const [accounts, setAccounts] = useState<Map<string, AccountResponse>>(
    new Map(),
  );
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [changeVersion, setChangeVersion] = useState(0);
  const carouselRef = useRef<HTMLDivElement>(null);
  const [carouselIndex, setCarouselIndex] = useState(0);

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

    Promise.all([
      alertsRequest,
      getCategories(),
      getTransactions(),
      getAccounts(),
    ])
      .then(
        ([
          loadedAlerts,
          loadedCategories,
          loadedTransactions,
          loadedAccounts,
        ]) => {
          if (!isCurrentRequest) {
            return;
          }

          setAlerts(loadedAlerts);
          setCategories(
            new Map(
              loadedCategories.map((category) => [category.id, category]),
            ),
          );
          setAccounts(
            new Map(loadedAccounts.map((account) => [account.id, account])),
          );
          setTransactions(
            new Map(
              loadedTransactions.map((transaction) => [
                transaction.id,
                transaction,
              ]),
            ),
          );
          onCountChange?.(loadedAlerts.length);
        },
      )
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

  useEffect(() => {
    carouselRef.current?.scrollTo({ left: 0, behavior: "auto" });
  }, [view, refreshVersion, visibleAlerts.length]);

  const getVisibleCardCount = () =>
    window.matchMedia("(max-width: 850px)").matches ? 1 : 2;

  const moveCarousel = (direction: -1 | 1) => {
    const carousel = carouselRef.current;
    if (!carousel) {
      return;
    }
    carousel.scrollBy({
      left: direction * carousel.clientWidth,
      behavior: "smooth",
    });
  };

  const updateCarouselIndex = () => {
    const carousel = carouselRef.current;
    if (!carousel) {
      return;
    }
    const card = carousel.querySelector<HTMLElement>(".insight-alert-card");
    if (!card) {
      setCarouselIndex(0);
      return;
    }
    const gap = 14;
    const cardPageWidth = card.offsetWidth + gap;
    setCarouselIndex(
      Math.max(0, Math.round(carousel.scrollLeft / cardPageWidth)),
    );
  };

  const maxCarouselIndex = Math.max(
    0,
    visibleAlerts.length - getVisibleCardCount(),
  );

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
    <Box className={`insight-alert-carousel ${compact ? "compact" : ""}`}>
      {visibleAlerts.length > getVisibleCardCount() ? (
        <Box className="insight-alert-carousel-controls">
          <IconButton
            className="insight-alert-carousel-button"
            aria-label="Previous insights"
            disabled={carouselIndex === 0}
            onClick={() => moveCarousel(-1)}
          >
            <ChevronLeftRounded />
          </IconButton>
          <IconButton
            className="insight-alert-carousel-button"
            aria-label="Next insights"
            disabled={carouselIndex >= maxCarouselIndex}
            onClick={() => moveCarousel(1)}
          >
            <ChevronRightRounded />
          </IconButton>
        </Box>
      ) : (
        <Box className="insight-alert-carousel-spacer" />
      )}
      <Box
        ref={carouselRef}
        className={`insight-alert-list ${compact ? "compact" : ""}`}
        aria-label={view === "unread" ? "Unread insights" : "Insight history"}
        onScroll={updateCarouselIndex}
      >
        {visibleAlerts.map((alert) => {
          const transaction = alert.transactionId
            ? transactions.get(alert.transactionId)
            : undefined;
          const categoryId =
            alert.categoryId ?? transaction?.categoryId ?? undefined;
          const categoryName = categoryId
            ? categories.get(categoryId)?.name
            : undefined;
          const title =
            alert.insightType === "category_spike"
              ? `${categoryName ?? "Category"} spending spike`
              : `${categoryName ?? "Category"} unusual purchase`;
          const alertCurrency = alert.currency ?? "CAD";
          const relatedTransactions =
            alert.insightType === "unusual_purchase"
              ? transaction
                ? [transaction]
                : []
              : [];
          const periodLabel = alert.periodStart
            ? new Intl.DateTimeFormat("en-CA", {
                year: "numeric",
                month: "long",
              }).format(new Date(`${alert.periodStart}T00:00:00`))
            : undefined;
          const formatTransactionDate = (date: string) =>
            new Intl.DateTimeFormat("en-CA", {
              year: "numeric",
              month: "short",
              day: "numeric",
            }).format(new Date(`${date}T00:00:00`));
          const SeverityIcon =
            alert.severity === "critical"
              ? ErrorOutlineRounded
              : WarningAmberRounded;
          const spendingSpikeUrl = (() => {
            if (alert.insightType !== "category_spike" || !alert.periodStart) {
              return "";
            }
            const periodStart = new Date(`${alert.periodStart}T00:00:00`);
            const periodEnd = new Date(
              periodStart.getFullYear(),
              periodStart.getMonth() + 1,
              0,
            );
            const params = new URLSearchParams({
              fromDate: alert.periodStart,
              toDate: `${periodEnd.getFullYear()}-${String(
                periodEnd.getMonth() + 1,
              ).padStart(2, "0")}-${String(periodEnd.getDate()).padStart(
                2,
                "0",
              )}`,
              maxAmount: "0",
            });
            if (categoryId) {
              params.set("categoryId", categoryId);
            }
            return `/transactions?${params.toString()}`;
          })();

          return (
            <Card
              key={alert.id}
              className={`insight-alert-card ${
                spendingSpikeUrl ? "insight-alert-card-link" : ""
              }`}
              role={spendingSpikeUrl ? "link" : undefined}
              tabIndex={spendingSpikeUrl ? 0 : undefined}
              onClick={(event) => {
                if (
                  !spendingSpikeUrl ||
                  (event.target as HTMLElement).closest("button, a")
                ) {
                  return;
                }
                navigate(spendingSpikeUrl);
              }}
              onKeyDown={(event) => {
                if (
                  spendingSpikeUrl &&
                  (event.key === "Enter" || event.key === " ")
                ) {
                  event.preventDefault();
                  navigate(spendingSpikeUrl);
                }
              }}
            >
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
                <Box className="insight-alert-details">
                  {relatedTransactions.map((relatedTransaction) => {
                    const account = accounts.get(relatedTransaction.accountId);
                    const currency = account?.currency ?? alertCurrency;
                    const transactionParams = new URLSearchParams({
                      accountId: relatedTransaction.accountId,
                      fromDate: relatedTransaction.transactionDate,
                      toDate: relatedTransaction.transactionDate,
                      minAmount: String(relatedTransaction.amount),
                      maxAmount: String(relatedTransaction.amount),
                      search: relatedTransaction.description,
                    });
                    if (relatedTransaction.categoryId) {
                      transactionParams.set(
                        "categoryId",
                        relatedTransaction.categoryId,
                      );
                    } else {
                      transactionParams.set("uncategorized", "true");
                    }
                    return (
                      <Link
                        key={relatedTransaction.id}
                        className="insight-alert-transaction-link"
                        to={`/transactions?${transactionParams.toString()}`}
                      >
                        <span className="insight-alert-transaction-date">
                          {formatTransactionDate(
                            relatedTransaction.transactionDate,
                          )}
                        </span>
                        <span className="insight-alert-transaction-divider">
                          |
                        </span>
                        <span className="insight-alert-transaction-name">
                          {relatedTransaction.description}
                        </span>
                        <span className="insight-alert-transaction-divider">
                          |
                        </span>
                        <span className="insight-alert-transaction-amount">
                          {formatDashboardCurrency(
                            relatedTransaction.amount,
                            currency,
                          )}
                        </span>
                        <ArrowForwardRounded />
                      </Link>
                    );
                  })}
                  {alert.insightType === "category_spike" && periodLabel ? (
                    <Typography className="insight-alert-detail-empty">
                      {periodLabel}
                    </Typography>
                  ) : alert.insightType === "unusual_purchase" &&
                    relatedTransactions.length === 0 ? (
                    <Typography className="insight-alert-detail-empty">
                      Transaction details unavailable.
                    </Typography>
                  ) : null}
                </Box>
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
                      onClick={(event) => {
                        event.stopPropagation();
                        void updateState(alert, "read");
                      }}
                    >
                      Read
                    </Button>
                    <Button
                      size="small"
                      color="inherit"
                      onClick={(event) => {
                        event.stopPropagation();
                        void updateState(alert, "dismiss");
                      }}
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
    </Box>
  );
}

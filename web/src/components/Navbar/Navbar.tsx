import { useEffect, useRef, useState } from "react";
import {
  Avatar,
  Badge,
  Box,
  Button,
  Divider,
  IconButton,
  Menu,
  MenuItem,
  Stack,
  Snackbar,
  Typography,
} from "@mui/material";
import MenuRounded from "@mui/icons-material/MenuRounded";
import CloseRounded from "@mui/icons-material/CloseRounded";
import KeyboardArrowDownRounded from "@mui/icons-material/KeyboardArrowDownRounded";
import NotificationsNoneRounded from "@mui/icons-material/NotificationsNoneRounded";
import ErrorOutlineRounded from "@mui/icons-material/ErrorOutlineRounded";
import WarningAmberRounded from "@mui/icons-material/WarningAmberRounded";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { useLogout } from "@app/hooks/useLogout";
import { getProfile, type ProfileResponse } from "@app/services/profileApi";
import {
  dismissAlert,
  getUnreadAlerts,
  INSIGHTS_CHANGED_EVENT,
  markAlertRead,
  notifyInsightsChanged,
  type InsightAlertResponse,
  type UpdateInsightStateRequest,
} from "@app/services/insightsApi";
import {
  getCategories,
  type CategoryResponse,
} from "@app/services/categoriesApi";
import {
  getTransactions,
  type TransactionResponse,
} from "@app/services/transactionApi";
import { formatDashboardCurrency } from "@app/utils/finance";
import "./Navbar.css";

const navigationItems = [
  { label: "Overview", href: "/overview" },
  { label: "Accounts", href: "/accounts" },
  { label: "Transactions", href: "/transactions" },
  { label: "Insights", href: "/insights" },
  { label: "Reports", href: "/reports" },
];

export default function Navbar() {
  const [mobileOpen, setMobileOpen] = useState(false);
  const [profileAnchor, setProfileAnchor] = useState<null | HTMLElement>(null);
  const [profile, setProfile] = useState<ProfileResponse | null>(null);
  const [unreadInsightCount, setUnreadInsightCount] = useState(0);
  const [unreadInsights, setUnreadInsights] = useState<InsightAlertResponse[]>(
    [],
  );
  const [categoriesById, setCategoriesById] = useState<
    Map<string, CategoryResponse>
  >(new Map());
  const [transactions, setTransactions] = useState<TransactionResponse[]>([]);
  const [notificationAnchor, setNotificationAnchor] =
    useState<null | HTMLElement>(null);
  const profileTriggerRef = useRef<HTMLButtonElement>(null);
  const [newInsight, setNewInsight] = useState<InsightAlertResponse | null>(
    null,
  );
  const [toastTop, setToastTop] = useState(window.scrollY <= 20 ? 78 : 16);
  const knownInsightIds = useRef<Set<string>>(new Set());
  const insightLoadInitialized = useRef(false);
  const location = useLocation();
  const navigate = useNavigate();
  const handleLogout = useLogout();

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
          console.error("Unable to load profile for navbar:", error);
        }
      });

    return () => {
      isCurrentRequest = false;
    };
  }, []);

  useEffect(() => {
    let animationFrame = 0;

    const updateScrollPosition = () => {
      if (animationFrame) {
        return;
      }

      animationFrame = window.requestAnimationFrame(() => {
        const progress = Math.min(1, Math.max(0, window.scrollY / 62));
        setToastTop(78 - progress * 62);
        animationFrame = 0;
      });
    };

    updateScrollPosition();
    window.addEventListener("scroll", updateScrollPosition, { passive: true });

    return () => {
      window.removeEventListener("scroll", updateScrollPosition);
      if (animationFrame) {
        window.cancelAnimationFrame(animationFrame);
      }
    };
  }, []);

  useEffect(() => {
    let isCurrentRequest = true;

    const refreshUnreadInsights = (showPopup = false) => {
      Promise.all([getUnreadAlerts(6), getCategories(), getTransactions()])
        .then(([alerts, categories, loadedTransactions]) => {
          if (isCurrentRequest) {
            setUnreadInsightCount(alerts.length);
            setUnreadInsights(alerts);
            setTransactions(loadedTransactions);
            setCategoriesById(
              new Map(categories.map((category) => [category.id, category])),
            );

            if (!insightLoadInitialized.current) {
              knownInsightIds.current = new Set(
                alerts.map((alert) => alert.id),
              );
              insightLoadInitialized.current = true;
              return;
            }

            if (showPopup) {
              const addedInsight = alerts.find(
                (alert) => !knownInsightIds.current.has(alert.id),
              );
              if (addedInsight) {
                setNewInsight(addedInsight);
              }
            }

            knownInsightIds.current = new Set(alerts.map((alert) => alert.id));
          }
        })
        .catch((error) => {
          if (isCurrentRequest) {
            console.error("Unable to load navbar insights:", error);
          }
        });
    };

    refreshUnreadInsights();
    const handleInsightChange = () => refreshUnreadInsights(true);
    const handleWindowFocus = () => refreshUnreadInsights(true);
    window.addEventListener(INSIGHTS_CHANGED_EVENT, handleInsightChange);
    window.addEventListener("focus", handleWindowFocus);

    return () => {
      isCurrentRequest = false;
      window.removeEventListener(INSIGHTS_CHANGED_EVENT, handleInsightChange);
      window.removeEventListener("focus", handleWindowFocus);
    };
  }, []);

  const closeMobileMenu = () => setMobileOpen(false);
  const firstName = profile?.firstName.trim() ?? "";
  const lastName = profile?.lastName.trim() ?? "";
  const fullName = [firstName, lastName].filter(Boolean).join(" ");
  const initials = [firstName, lastName]
    .map((name) => name.charAt(0).toUpperCase())
    .join("");
  const getAlertTitle = (alert: InsightAlertResponse) =>
    alert.insightType === "category_spike"
      ? `${
          alert.categoryId
            ? (categoriesById.get(alert.categoryId)?.name ?? "Category")
            : "Category"
        } spending spike`
      : `${
          alert.categoryId
            ? (categoriesById.get(alert.categoryId)?.name ?? "Category")
            : "Category"
        } unusual purchase`;
  const getAlertRequest = (
    alert: InsightAlertResponse,
  ): UpdateInsightStateRequest => ({
    insightType: alert.insightType,
    categoryId: alert.categoryId,
    transactionId: alert.transactionId,
    currency:
      alert.insightType === "category_spike" ? alert.currency : undefined,
    periodStart: alert.periodStart,
  });

  const getAlertTransactionUrl = (alert: InsightAlertResponse): string => {
    const params = new URLSearchParams();

    if (alert.categoryId) {
      params.set("categoryId", alert.categoryId);
    }

    if (alert.periodStart) {
      const periodStart = new Date(`${alert.periodStart}T00:00:00`);
      const periodEnd = new Date(
        periodStart.getFullYear(),
        periodStart.getMonth() + 1,
        0,
      );
      params.set("fromDate", alert.periodStart);
      params.set(
        "toDate",
        `${periodEnd.getFullYear()}-${String(periodEnd.getMonth() + 1).padStart(2, "0")}-${String(periodEnd.getDate()).padStart(2, "0")}`,
      );
    }

    if (alert.insightType === "category_spike") {
      params.set("maxAmount", "0");
    }

    if (alert.transactionId) {
      const transaction = transactions.find(
        (candidate) => candidate.id === alert.transactionId,
      );
      if (!transaction) {
        return `/transactions?${params.toString()}`;
      }
      params.set("accountId", transaction.accountId);
      params.set("fromDate", transaction.transactionDate);
      params.set("toDate", transaction.transactionDate);
      params.set("minAmount", String(transaction.amount));
      params.set("maxAmount", String(transaction.amount));
      params.set("search", transaction.description);
      if (transaction.categoryId) {
        params.set("categoryId", transaction.categoryId);
      } else {
        params.set("uncategorized", "true");
      }
    }

    return `/transactions?${params.toString()}`;
  };

  const updateNotification = async (
    alert: InsightAlertResponse,
    action: "read" | "dismiss",
  ) => {
    try {
      const request = getAlertRequest(alert);
      if (action === "read") {
        await markAlertRead(request);
      } else {
        await dismissAlert(request);
      }

      setUnreadInsights((currentAlerts) =>
        currentAlerts.filter((currentAlert) => currentAlert.id !== alert.id),
      );
      setUnreadInsightCount((count) => Math.max(0, count - 1));
      knownInsightIds.current.delete(alert.id);
      notifyInsightsChanged();
    } catch (error) {
      console.error("Unable to update navbar insight:", error);
    }
  };

  const readAllNotifications = async () => {
    if (unreadInsights.length === 0) {
      return;
    }

    try {
      await Promise.all(
        unreadInsights.map((alert) => markAlertRead(getAlertRequest(alert))),
      );
      setUnreadInsights([]);
      setUnreadInsightCount(0);
      knownInsightIds.current.clear();
      notifyInsightsChanged();
    } catch (error) {
      console.error("Unable to mark navbar insights as read:", error);
    }
  };

  return (
    <Box component="header" className="dashboard-navbar">
      <Link
        to="/overview"
        className="dashboard-brand"
        onClick={closeMobileMenu}
      >
        Fin<span>Hub</span>
      </Link>

      <Stack component="nav" className="dashboard-nav-links" direction="row">
        {navigationItems.map((item) => (
          <Link
            key={item.href}
            to={item.href}
            className={
              location.pathname === item.href
                ? "dashboard-nav-link active"
                : "dashboard-nav-link"
            }
          >
            {item.label}
          </Link>
        ))}
      </Stack>

      <Stack className="dashboard-actions" direction="row">
        <IconButton
          onClick={(event) => {
            if (window.matchMedia("(max-width: 800px)").matches) {
              setNotificationAnchor(event.currentTarget);
            } else {
              setNotificationAnchor(profileTriggerRef.current);
            }
          }}
          className="dashboard-insight-button"
          aria-label={`${unreadInsightCount} unread insights`}
          title="Insights"
        >
          <Badge
            badgeContent={unreadInsightCount}
            color="error"
            max={99}
            invisible={unreadInsightCount === 0}
          >
            <NotificationsNoneRounded />
          </Badge>
        </IconButton>
        <Button
          ref={profileTriggerRef}
          className="dashboard-profile-trigger"
          onClick={(event) => setProfileAnchor(event.currentTarget)}
          endIcon={<KeyboardArrowDownRounded />}
          startIcon={
            <Avatar className="dashboard-avatar">
              {initials || undefined}
            </Avatar>
          }
        >
          <span className="dashboard-profile-name">{fullName}</span>
        </Button>
        <Menu
          anchorEl={profileAnchor}
          open={Boolean(profileAnchor)}
          onClose={() => setProfileAnchor(null)}
          className="dashboard-profile-menu"
        >
          <MenuItem
            component={Link}
            to="/profile"
            onClick={() => setProfileAnchor(null)}
          >
            Profile settings
          </MenuItem>
          <Divider />
          <MenuItem
            className="dashboard-signout-menu-item"
            onClick={() => {
              setProfileAnchor(null);
              void handleLogout();
            }}
          >
            Sign out
          </MenuItem>
        </Menu>
        <Menu
          anchorEl={notificationAnchor}
          open={Boolean(notificationAnchor)}
          onClose={() => setNotificationAnchor(null)}
          className="dashboard-notification-menu"
          anchorOrigin={{ vertical: "bottom", horizontal: "right" }}
          transformOrigin={{ vertical: "top", horizontal: "right" }}
        >
          <Box className="dashboard-notification-header">
            <Typography component="h2">Insights</Typography>
            <Box className="dashboard-notification-header-actions">
              {unreadInsightCount > 0 ? (
                <Button
                  size="small"
                  className="dashboard-notification-read-all"
                  onClick={() => void readAllNotifications()}
                >
                  Read all
                </Button>
              ) : null}
              <Typography>{unreadInsightCount} unread</Typography>
            </Box>
          </Box>
          <Box className="dashboard-notification-content">
            {unreadInsights.length === 0 ? (
              <Box className="dashboard-notification-empty">
                <Typography>You&apos;re all caught up.</Typography>
              </Box>
            ) : (
              unreadInsights.map((alert) => (
                <Box key={alert.id} className="dashboard-notification-item">
                  <Link
                    className="dashboard-notification-link"
                    to={getAlertTransactionUrl(alert)}
                    onClick={() => setNotificationAnchor(null)}
                  >
                    <Box
                      className={`dashboard-notification-icon ${alert.severity}`}
                    >
                      {alert.severity === "critical" ? (
                        <ErrorOutlineRounded />
                      ) : (
                        <WarningAmberRounded />
                      )}
                    </Box>
                    <Box className="dashboard-notification-copy">
                      <Typography className="dashboard-notification-title">
                        {getAlertTitle(alert)}
                      </Typography>
                      <Typography className="dashboard-notification-detail">
                        {formatDashboardCurrency(
                          alert.amount,
                          alert.currency ?? "CAD",
                        )}
                        {" compared to "}
                        {formatDashboardCurrency(
                          alert.baseline,
                          alert.currency ?? "CAD",
                        )}
                        {" baseline"}
                      </Typography>
                    </Box>
                  </Link>
                  <Stack
                    direction="row"
                    className="dashboard-notification-actions"
                  >
                    <Button
                      size="small"
                      onClick={(event) => {
                        event.stopPropagation();
                        void updateNotification(alert, "read");
                      }}
                    >
                      Read
                    </Button>
                    <Button
                      size="small"
                      color="inherit"
                      onClick={(event) => {
                        event.stopPropagation();
                        void updateNotification(alert, "dismiss");
                      }}
                    >
                      Dismiss
                    </Button>
                  </Stack>
                </Box>
              ))
            )}
          </Box>
          <Box className="dashboard-notification-footer">
            <Divider />
            <MenuItem
              component={Link}
              to="/insights"
              className="dashboard-notification-view-all"
              onClick={() => setNotificationAnchor(null)}
            >
              View all insights
            </MenuItem>
          </Box>
        </Menu>
        <IconButton
          className="dashboard-menu-button"
          aria-label={mobileOpen ? "Close navigation" : "Open navigation"}
          onClick={() => {
            setProfileAnchor(null);
            setMobileOpen((open) => !open);
          }}
        >
          {mobileOpen ? <CloseRounded /> : <MenuRounded />}
        </IconButton>
      </Stack>

      <Snackbar
        open={Boolean(newInsight)}
        autoHideDuration={6000}
        onClose={() => setNewInsight(null)}
        anchorOrigin={{ vertical: "top", horizontal: "right" }}
        className="dashboard-insight-toast"
        style={{ top: `${toastTop}px` }}
      >
        {newInsight ? (
          <Box
            className="dashboard-insight-toast-content"
            role="alert"
            onClick={() => {
              setNewInsight(null);
              navigate("/insights");
            }}
          >
            <NotificationsNoneRounded />
            <Box>
              <Typography component="h2">
                {getAlertTitle(newInsight)}
              </Typography>
              <Typography>
                {formatDashboardCurrency(
                  newInsight.amount,
                  newInsight.currency ?? "CAD",
                )}
                {" · New insight detected"}
              </Typography>
            </Box>
          </Box>
        ) : (
          <span />
        )}
      </Snackbar>

      {mobileOpen && (
        <Box className="dashboard-mobile-menu">
          {navigationItems.map((item) => (
            <Link
              key={item.href}
              to={item.href}
              className={
                location.pathname === item.href
                  ? "dashboard-mobile-link active"
                  : "dashboard-mobile-link"
              }
              onClick={closeMobileMenu}
            >
              {item.label}
            </Link>
          ))}
          <Divider />
          <Button
            component={Link}
            to="/profile"
            className="dashboard-mobile-link"
            onClick={closeMobileMenu}
          >
            Profile settings
          </Button>
          <Button
            className="dashboard-mobile-link dashboard-mobile-signout"
            onClick={() => {
              closeMobileMenu();
              void handleLogout();
            }}
          >
            Sign out
          </Button>
        </Box>
      )}
    </Box>
  );
}

import { useEffect, useMemo, useState } from "react";
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Container,
  Stack,
  Switch,
  Typography,
} from "@mui/material";
import AddRounded from "@mui/icons-material/AddRounded";
import ArrowDownwardRounded from "@mui/icons-material/ArrowDownwardRounded";
import ArrowUpwardRounded from "@mui/icons-material/ArrowUpwardRounded";
import DeleteOutlineRounded from "@mui/icons-material/DeleteOutlineRounded";
import EditRounded from "@mui/icons-material/EditRounded";
import Footer from "@app/components/Footer";
import Navbar from "@app/components/Navbar";
import { ConfirmDialog } from "@app/components/Dialog";
import { CategoryChip } from "@app/components/Category/CategoryChip";
import type { SelectOption } from "@app/components/Form/SelectFormField";
import { getAccounts, type AccountResponse } from "@app/services/accountsApi";
import { getCategories, type CategoryResponse } from "@app/services/categoriesApi";
import {
  deleteCategorizationRule,
  disableCategorizationRule,
  enableCategorizationRule,
  getCategorizationRules,
  reorderCategorizationRules,
  type CategorizationRuleResponse,
} from "@app/services/categorizationRulesApi";
import { getCategoryColor } from "@app/utils/categories";
import { getErrorMessage } from "@app/utils/formErrors";
import RuleFormDialog from "./RuleFormDialog";
import "@app/components/Accounts/Accounts.css";
import "./Rules.css";

function describeConditions(
  rule: CategorizationRuleResponse,
  accountNames: Map<string, string>,
): string[] {
  const conditions: string[] = [];
  if (rule.accountId) {
    conditions.push(
      `Account is ${accountNames.get(rule.accountId) ?? "a deleted account"}`,
    );
  }
  if (rule.descriptionContains) {
    conditions.push(`Description contains “${rule.descriptionContains}”`);
  }
  if (rule.amountDirection) {
    conditions.push(
      rule.amountDirection === "in" ? "Money in" : "Money out",
    );
  }
  return conditions;
}

export default function Rules() {
  const [rules, setRules] = useState<CategorizationRuleResponse[]>([]);
  const [categories, setCategories] = useState<CategoryResponse[]>([]);
  const [accounts, setAccounts] = useState<AccountResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState("");
  const [actionError, setActionError] = useState("");
  const [busyRuleId, setBusyRuleId] = useState<string | null>(null);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [selectedRule, setSelectedRule] =
    useState<CategorizationRuleResponse | null>(null);
  const [ruleToDelete, setRuleToDelete] =
    useState<CategorizationRuleResponse | null>(null);
  const [isDeleteOpen, setIsDeleteOpen] = useState(false);
  const [deleteError, setDeleteError] = useState("");
  const [deleting, setDeleting] = useState(false);

  useEffect(() => {
    Promise.all([
      getCategorizationRules(),
      getCategories(),
      getAccounts(),
    ])
      .then(([loadedRules, loadedCategories, loadedAccounts]) => {
        setRules(loadedRules);
        setCategories(loadedCategories);
        setAccounts(loadedAccounts);
      })
      .catch((error) =>
        setLoadError(getErrorMessage(error, "Unable to load rules.")),
      )
      .finally(() => setLoading(false));
  }, []);

  const categoryById = useMemo(
    () => new Map(categories.map((category) => [category.id, category])),
    [categories],
  );
  const accountNames = useMemo(
    () => new Map(accounts.map((account) => [account.id, account.name])),
    [accounts],
  );
  const categoryOptions: SelectOption[] = useMemo(
    () =>
      categories.map((category) => ({
        value: category.id,
        label: category.name,
        color: getCategoryColor(category.name),
      })),
    [categories],
  );
  const accountOptions: SelectOption[] = useMemo(
    () => [
      { value: "", label: "Any account" },
      ...accounts.map((account) => ({ value: account.id, label: account.name })),
    ],
    [accounts],
  );
  const nextPriority =
    rules.reduce((highest, rule) => Math.max(highest, rule.priority), 0) + 1;

  const openCreate = () => {
    setSelectedRule(null);
    setIsFormOpen(true);
  };

  const openEdit = (rule: CategorizationRuleResponse) => {
    setSelectedRule(rule);
    setIsFormOpen(true);
  };

  const handleSaved = (saved: CategorizationRuleResponse) =>
    setRules((current) =>
      current.some((rule) => rule.id === saved.id)
        ? current.map((rule) => (rule.id === saved.id ? saved : rule))
        : [...current, saved],
    );

  const handleToggle = async (rule: CategorizationRuleResponse) => {
    setBusyRuleId(rule.id);
    setActionError("");
    try {
      const saved = rule.isEnabled
        ? await disableCategorizationRule(rule.id)
        : await enableCategorizationRule(rule.id);
      handleSaved(saved);
    } catch (error) {
      setActionError(getErrorMessage(error, "Unable to update the rule."));
    } finally {
      setBusyRuleId(null);
    }
  };

  const handleMove = async (index: number, offset: -1 | 1) => {
    const target = index + offset;
    if (target < 0 || target >= rules.length) {
      return;
    }

    const reordered = [...rules];
    [reordered[index], reordered[target]] = [reordered[target], reordered[index]];

    setBusyRuleId(rules[index].id);
    setActionError("");
    try {
      setRules(
        await reorderCategorizationRules(reordered.map((rule) => rule.id)),
      );
    } catch (error) {
      setActionError(getErrorMessage(error, "Unable to reorder the rules."));
    } finally {
      setBusyRuleId(null);
    }
  };

  const handleDelete = async () => {
    if (!ruleToDelete) {
      return;
    }

    setDeleting(true);
    setDeleteError("");
    try {
      await deleteCategorizationRule(ruleToDelete.id);
      setRules((current) =>
        current
          .filter((rule) => rule.id !== ruleToDelete.id)
          .map((rule, index) => ({ ...rule, priority: index + 1 })),
      );
      setIsDeleteOpen(false);
    } catch (error) {
      setDeleteError(getErrorMessage(error, "Unable to delete the rule."));
    } finally {
      setDeleting(false);
    }
  };

  return (
    <Box className="rules-page">
      <Container maxWidth="xl" className="accounts-shell">
        <Navbar />
        <Box component="main">
          <Box className="accounts-welcome">
            <Box>
              <Typography className="accounts-kicker">CATEGORY</Typography>
              <Typography component="h1" className="accounts-title">
                Rules
              </Typography>
            </Box>
            <Button
              className="accounts-add-button"
              startIcon={<AddRounded />}
              onClick={openCreate}
              disabled={loading || categories.length === 0}
            >
              Add rule
            </Button>
          </Box>

          {loadError && (
            <Alert className="page-error" severity="error" variant="outlined">
              {loadError}
            </Alert>
          )}
          {actionError && (
            <Alert className="page-error" severity="error" variant="outlined">
              {actionError}
            </Alert>
          )}

          {loading ? (
            <Typography className="accounts-empty">Loading rules...</Typography>
          ) : rules.length === 0 ? (
            <Typography className="accounts-empty">
              No rules yet. Add one to categorize matching transactions
              automatically.
            </Typography>
          ) : (
            <Stack spacing={1.5} component="ol" className="rules-list">
              {rules.map((rule, index) => {
                const category = categoryById.get(rule.categoryId);
                const conditions = describeConditions(rule, accountNames);
                const busy = busyRuleId === rule.id;
                return (
                  <Card
                    component="li"
                    key={rule.id}
                    className={`rule-card${rule.isEnabled ? "" : " disabled"}`}
                  >
                    <CardContent>
                      <Typography className="rule-priority" aria-hidden>
                        {index + 1}
                      </Typography>
                      <Box className="rule-body">
                        <Box className="rule-conditions">
                          {conditions.map((condition) => (
                            <span className="rule-condition" key={condition}>
                              {condition}
                            </span>
                          ))}
                        </Box>
                        <Box className="rule-result">
                          <span className="rule-arrow">Category:</span>
                          {category ? (
                            <CategoryChip name={category.name} />
                          ) : (
                            <span className="rule-missing">
                              Deleted category
                            </span>
                          )}
                          {!rule.isEnabled && (
                            <span className="rule-status">Disabled</span>
                          )}
                        </Box>
                      </Box>
                      <Box className="rule-actions">
                        <Switch
                          size="small"
                          checked={rule.isEnabled}
                          disabled={busy}
                          onChange={() => handleToggle(rule)}
                          slotProps={{
                            input: {
                              "aria-label": `${rule.isEnabled ? "Disable" : "Enable"} rule ${index + 1}`,
                            },
                          }}
                        />
                        <Button
                          className="account-icon-button"
                          aria-label={`Move rule ${index + 1} up`}
                          disabled={busy || index === 0}
                          onClick={() => handleMove(index, -1)}
                        >
                          <ArrowUpwardRounded />
                        </Button>
                        <Button
                          className="account-icon-button"
                          aria-label={`Move rule ${index + 1} down`}
                          disabled={busy || index === rules.length - 1}
                          onClick={() => handleMove(index, 1)}
                        >
                          <ArrowDownwardRounded />
                        </Button>
                        <Button
                          className="account-icon-button"
                          aria-label={`Edit rule ${index + 1}`}
                          disabled={busy}
                          onClick={() => openEdit(rule)}
                        >
                          <EditRounded />
                        </Button>
                        <Button
                          className="account-icon-button delete"
                          aria-label={`Delete rule ${index + 1}`}
                          disabled={busy}
                          onClick={() => {
                            setRuleToDelete(rule);
                            setDeleteError("");
                            setIsDeleteOpen(true);
                          }}
                        >
                          <DeleteOutlineRounded />
                        </Button>
                      </Box>
                    </CardContent>
                  </Card>
                );
              })}
            </Stack>
          )}
        </Box>
        <Footer />
      </Container>

      <RuleFormDialog
        open={isFormOpen}
        rule={selectedRule}
        categoryOptions={categoryOptions}
        accountOptions={accountOptions}
        nextPriority={nextPriority}
        onClose={() => setIsFormOpen(false)}
        onSaved={handleSaved}
      />
      <ConfirmDialog
        open={isDeleteOpen}
        title="Delete rule"
        message={
          <>
            Delete this rule? Transactions already categorized by it keep their
            category. You can disable the rule instead to keep it for later.
          </>
        }
        confirmLabel="Delete rule"
        error={deleteError}
        loading={deleting}
        onConfirm={handleDelete}
        onCancel={() => setIsDeleteOpen(false)}
        onExited={() => setRuleToDelete(null)}
      />
    </Box>
  );
}
import { useEffect, useState } from "react";
import { Box, Divider, Typography } from "@mui/material";
import { useForm, type SubmitHandler } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { FormDialog } from "@app/components/Dialog";
import { TextFormField } from "@app/components/Form";
import {
  SelectFormField,
  type SelectOption,
} from "@app/components/Form/SelectFormField";
import {
  createCategorizationRule,
  updateCategorizationRule,
  type CategorizationRuleRequest,
  type CategorizationRuleResponse,
} from "@app/services/categorizationRulesApi";
import { applyFieldErrors, getErrorMessage } from "@app/utils/formErrors";
import {
  emptyRuleForm,
  ruleSchema,
  type RuleFormData,
} from "@app/validators/ruleSchemas";

type RuleFormDialogProps = {
  open: boolean;
  rule: CategorizationRuleResponse | null;
  categoryOptions: SelectOption[];
  accountOptions: SelectOption[];
  nextPriority: number;
  onClose: () => void;
  onSaved: (rule: CategorizationRuleResponse) => void;
};

const directionOptions: SelectOption[] = [
  { value: "", label: "Any amount" },
  { value: "in", label: "Money in" },
  { value: "out", label: "Money out" },
];

function toFormValues(rule: CategorizationRuleResponse): RuleFormData {
  return {
    categoryId: rule.categoryId,
    accountId: rule.accountId ?? "",
    descriptionContains: rule.descriptionContains ?? "",
    amountDirection: rule.amountDirection ?? "",
  };
}

export default function RuleFormDialog({
  open,
  rule,
  categoryOptions,
  accountOptions,
  nextPriority,
  onClose,
  onSaved,
}: RuleFormDialogProps) {
  const [serverError, setServerError] = useState("");
  const form = useForm<RuleFormData>({
    resolver: zodResolver(ruleSchema),
    defaultValues: emptyRuleForm,
    mode: "onTouched",
  });

  useEffect(() => {
    if (open) {
      form.reset(rule ? toFormValues(rule) : emptyRuleForm);
    }
  }, [open, rule, form]);

  const onSubmit: SubmitHandler<RuleFormData> = async (data) => {
    setServerError("");
    const request: CategorizationRuleRequest = {
      categoryId: data.categoryId,
      accountId: data.accountId || null,
      descriptionContains: data.descriptionContains.trim() || null,
      amountDirection: data.amountDirection || null,
      priority: rule ? rule.priority : nextPriority,
    };

    try {
      const saved = rule
        ? await updateCategorizationRule(rule.id, request)
        : await createCategorizationRule(request);
      onSaved(saved);
      onClose();
    } catch (error) {
      applyFieldErrors(error, form.setError, Object.keys(emptyRuleForm));
      setServerError(getErrorMessage(error, "Unable to save the rule."));
    }
  };
  const handleClose = () => {
    setServerError("");
    onClose();
  };

  return (
    <FormDialog
      open={open}
      title={rule ? "Edit rule" : "Add a rule"}
      form={form}
      onSubmit={onSubmit}
      onClose={handleClose}
      submitLabel={rule ? "Save rule" : "Add rule"}
      error={serverError}
    >
      <Box className="rule-dialog-form">
        <Typography className="rule-dialog-section-label">ASSIGN TO</Typography>
        <Box className="rule-dialog-section rule-dialog-category-section">
          <SelectFormField<RuleFormData>
            name="categoryId"
            label="Category"
            options={categoryOptions}
            selectClassName="ledger-category-form-select"
            required
            displayEmpty
          />
        </Box>

        <Divider className="rule-dialog-divider" />

        <Typography className="rule-dialog-section-label">
          MATCH WHEN
        </Typography>
        <Box className="rule-dialog-section rule-dialog-condition-section">
          <SelectFormField<RuleFormData>
            name="accountId"
            label="Account"
            options={accountOptions}
            displayEmpty
          />
          <SelectFormField<RuleFormData>
            name="amountDirection"
            label="Amount"
            options={directionOptions}
            displayEmpty
          />
          <TextFormField<RuleFormData>
            name="descriptionContains"
            label="Description contains"
            slotProps={{ htmlInput: { maxLength: 500 } }}
            className="rule-dialog-description-field"
          />
        </Box>
      </Box>
    </FormDialog>
  );
}

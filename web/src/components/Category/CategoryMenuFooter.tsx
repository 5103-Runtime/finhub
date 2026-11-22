import { MenuItem } from "@mui/material";
import SettingsRounded from "@mui/icons-material/SettingsRounded";
import RuleRounded from "@mui/icons-material/RuleRounded";
import { useNavigate } from "react-router-dom";

export const MANAGE_CATEGORIES_VALUE = "__manage_categories__";
export const MANAGE_RULES_VALUE = "__manage_rules__";

export function CategoryMenuFooter() {
  const navigate = useNavigate();

  return (
    <>
      <MenuItem
        value={MANAGE_CATEGORIES_VALUE}
        className="ledger-category-manage-option"
        onMouseDown={(event) => event.preventDefault()}
        onClick={() => navigate("/categories")}
      >
        <SettingsRounded fontSize="small" />
        Edit categories
      </MenuItem>
      <MenuItem
        value={MANAGE_RULES_VALUE}
        className="ledger-category-manage-option ledger-category-rules-option"
        onMouseDown={(event) => event.preventDefault()}
        onClick={() => navigate("/rules")}
      >
        <RuleRounded fontSize="small" />
        Manage rules
      </MenuItem>
    </>
  );
}

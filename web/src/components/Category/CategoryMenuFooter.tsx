import { MenuItem } from "@mui/material";
import SettingsRounded from "@mui/icons-material/SettingsRounded";
import { useNavigate } from "react-router-dom";

export const MANAGE_CATEGORIES_VALUE = "__manage_categories__";

export function CategoryMenuFooter() {
  const navigate = useNavigate();

  return (
    <MenuItem
      value={MANAGE_CATEGORIES_VALUE}
      className="ledger-category-manage-option"
      onClick={() => navigate("/categories")}
    >
      <SettingsRounded fontSize="small" />
      Edit categories
    </MenuItem>
  );
}

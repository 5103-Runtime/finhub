import type { CSSProperties } from "react";
import { FormControl, InputLabel, MenuItem, Select } from "@mui/material";
import type { CategoryOption } from "./categoryTypes";

type CategoryFilterProps = {
  categoryOptions: CategoryOption[];
  selectedCategoryId: string;
  uncategorized: boolean;
  onChange: (key: "categoryId" | "uncategorized", value: string) => void;
};

export function CategoryFilter({
  categoryOptions,
  selectedCategoryId,
  uncategorized,
  onChange,
}: CategoryFilterProps) {
  const selectedCategory = uncategorized
    ? categoryOptions[0]
    : (categoryOptions.find((option) => option.value === selectedCategoryId) ??
      categoryOptions[0]);
  const hasSelectedCategoryFilter =
    uncategorized || Boolean(selectedCategoryId);
  const value = uncategorized ? "__uncategorized" : selectedCategoryId;
  const renderOption = (option: CategoryOption, value = option.value) => (
    <MenuItem
      key={value || option.label}
      value={value}
      className={`ledger-category-option${value === "" ? " ledger-category-option-all" : ""}`}
      style={{ "--category-color": option.color } as CSSProperties}
    >
      {option.label}
    </MenuItem>
  );

  return (
    <FormControl
      size="small"
      fullWidth
      className={
        hasSelectedCategoryFilter
          ? "transaction-filter-category-selected"
          : undefined
      }
    >
      {hasSelectedCategoryFilter && <InputLabel shrink>Category</InputLabel>}
      <Select
        className="ledger-category-form-select"
        displayEmpty
        label={hasSelectedCategoryFilter ? "Category" : undefined}
        value={value}
        style={{ "--category-color": selectedCategory.color } as CSSProperties}
        renderValue={(selectedValue) =>
          selectedValue === ""
            ? "Category"
            : selectedValue === "__uncategorized"
              ? "None"
              : selectedCategory.label
        }
        MenuProps={{
          slotProps: {
            paper: { className: "ledger-category-menu" },
          },
        }}
        onChange={(event) =>
          event.target.value === "__uncategorized"
            ? onChange("uncategorized", "true")
            : onChange("categoryId", event.target.value)
        }
      >
        {renderOption(
          {
            ...categoryOptions[0],
            label: "All categories",
          },
          "",
        )}
        {renderOption(
          {
            ...categoryOptions[0],
            label: "None",
          },
          "__uncategorized",
        )}
        {categoryOptions.slice(1).map((option) => renderOption(option))}
      </Select>
    </FormControl>
  );
}

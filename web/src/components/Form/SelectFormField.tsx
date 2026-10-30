import { useId, type CSSProperties, type ReactNode } from "react";
import { useNavigate } from "react-router-dom";
import {
  Controller,
  useFormContext,
  type FieldValues,
  type Path,
} from "react-hook-form";
import {
  FormControl,
  FormHelperText,
  InputLabel,
  Divider,
  MenuItem,
  Select,
  type FormControlProps,
} from "@mui/material";
import SettingsRounded from "@mui/icons-material/SettingsRounded";
import { CategoryMenuList } from "@app/components/Category/CategoryMenuList";

export type SelectOption = {
  value: string;
  label: string;
  color?: string;
};

export type SelectFormFieldProps<T extends FieldValues> = {
  name: Path<T>;
  label: string;
  options: SelectOption[];
  helperText?: ReactNode;
  selectClassName?: string;
  displayEmpty?: boolean;
} & Omit<FormControlProps, "error" | "children">;

export function SelectFormField<T extends FieldValues>({
  name,
  label,
  options,
  helperText,
  selectClassName,
  displayEmpty = false,
  ...props
}: SelectFormFieldProps<T>) {
  const { control } = useFormContext<T>();
  const labelId = useId();
  const navigate = useNavigate();
  const isCategoryField = options.some((option) => option.color);
  const manageCategoriesValue = "__manage_categories__";

  return (
    <Controller
      name={name}
      control={control}
      render={({ field: { ref, ...field }, fieldState }) => (
        <FormControl fullWidth error={Boolean(fieldState.error)} {...props}>
          <InputLabel
            id={labelId}
            shrink={displayEmpty || Boolean(field.value)}
          >
            {label}
          </InputLabel>
          <Select
            {...field}
            inputRef={ref}
            labelId={labelId}
            label={label}
            className={selectClassName}
            displayEmpty={displayEmpty}
            onChange={(event) => {
              if (
                isCategoryField &&
                event.target.value === manageCategoriesValue
              ) {
                navigate("/categories");
                return;
              }
              field.onChange(event);
            }}
            renderValue={(value) =>
              options.find((option) => option.value === value)?.label ?? ""
            }
            MenuProps={{
              slots: isCategoryField ? { list: CategoryMenuList } : undefined,
              slotProps: {
                paper: {
                  className: [
                    "app-form-menu",
                    options.some((option) => option.color)
                      ? "ledger-category-menu"
                      : undefined,
                  ]
                    .filter(Boolean)
                    .join(" "),
                },
              },
            }}
            style={
              {
                "--category-color": options.find(
                  (option) => option.value === field.value,
                )?.color,
              } as CSSProperties
            }
          >
            {options.map((option) => (
              <MenuItem
                key={option.value}
                value={option.value}
                className={option.color ? "ledger-category-option" : undefined}
                style={
                  option.color
                    ? ({
                        "--category-color": option.color,
                      } as CSSProperties)
                    : undefined
                }
              >
                {option.label}
              </MenuItem>
            ))}
            {isCategoryField && (
              <>
                <Divider />
                <MenuItem
                  value={manageCategoriesValue}
                  className="ledger-category-manage-option"
                  onClick={() => navigate("/categories")}
                >
                  <SettingsRounded fontSize="small" />
                  Edit categories
                </MenuItem>
              </>
            )}
          </Select>
          <FormHelperText>
            {fieldState.error?.message ?? helperText}
          </FormHelperText>
        </FormControl>
      )}
    />
  );
}

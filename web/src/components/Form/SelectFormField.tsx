import {
  useId,
  useRef,
  type CSSProperties,
  type MouseEvent,
  type ReactNode,
} from "react";
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
  MenuItem,
  Select,
  type FormControlProps,
} from "@mui/material";
import { CategoryMenuList } from "@app/components/Category/CategoryMenuList";
import {
  CategoryMenuFooter,
  MANAGE_CATEGORIES_VALUE,
  MANAGE_RULES_VALUE,
} from "@app/components/Category/CategoryMenuFooter";

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
  placeholder?: string;
  onValueChange?: (value: string) => void;
} & Omit<FormControlProps, "error" | "children">;

export function SelectFormField<T extends FieldValues>({
  name,
  label,
  options,
  helperText,
  selectClassName,
  displayEmpty = false,
  placeholder,
  onValueChange,
  required = false,
  ...props
}: SelectFormFieldProps<T>) {
  const { control } = useFormContext<T>();
  const labelId = useId();
  const navigate = useNavigate();
  const selectRootRef = useRef<HTMLDivElement>(null);
  const isCategoryField = options.some((option) => option.color);

  const focusSelectTrigger = () => {
    const trigger =
      selectRootRef.current?.querySelector<HTMLElement>('[role="combobox"]') ??
      null;
    trigger?.focus();
  };

  return (
    <Controller
      name={name}
      control={control}
      render={({ field: { ref, ...field }, fieldState }) => (
        <FormControl
          fullWidth
          error={Boolean(fieldState.error)}
          required={required}
          {...props}
        >
          <InputLabel
            id={labelId}
            shrink={displayEmpty || Boolean(field.value)}
            sx={
              placeholder && !field.value ? { visibility: "hidden" } : undefined
            }
          >
            {label}
          </InputLabel>
          <Select
            {...field}
            ref={selectRootRef}
            value={
              field.value &&
              options.some((option) => option.value === field.value)
                ? field.value
                : ""
            }
            inputRef={ref}
            labelId={labelId}
            label={placeholder && !field.value ? undefined : label}
            className={[
              selectClassName,
              fieldState.error ? "app-select-error" : undefined,
            ]
              .filter(Boolean)
              .join(" ")}
            displayEmpty={displayEmpty || Boolean(placeholder)}
            onClose={() => {
              requestAnimationFrame(() => {
                focusSelectTrigger();
              });
            }}
            autoFocus={false}
            onChange={(event) => {
              const selectedValue = String(event.target.value);
              focusSelectTrigger();
              if (
                isCategoryField &&
                selectedValue === MANAGE_CATEGORIES_VALUE
              ) {
                navigate("/categories");
                return;
              }
              if (isCategoryField && selectedValue === MANAGE_RULES_VALUE) {
                navigate("/rules");
                return;
              }
              field.onChange(selectedValue);
              onValueChange?.(selectedValue);
            }}
            renderValue={(value) =>
              options.find((option) => option.value === value)?.label ??
              (value
                ? ""
                : placeholder
                  ? `${placeholder}${required ? " *" : ""}`
                  : "")
            }
            MenuProps={{
              slots: isCategoryField ? { list: CategoryMenuList } : undefined,
              slotProps: {
                paper: {
                  onMouseDownCapture: (event: MouseEvent) => {
                    event.preventDefault();
                  },
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
                onMouseDown={(event) => event.preventDefault()}
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
            {isCategoryField && <CategoryMenuFooter />}
          </Select>
          <FormHelperText>
            {fieldState.error?.message ?? helperText}
          </FormHelperText>
        </FormControl>
      )}
    />
  );
}

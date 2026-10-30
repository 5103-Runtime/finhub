import { useId, type CSSProperties, type ReactNode } from "react";
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

  return (
    <Controller
      name={name}
      control={control}
      render={({ field: { ref, ...field }, fieldState }) => (
        <FormControl fullWidth error={Boolean(fieldState.error)} {...props}>
          <InputLabel id={labelId}>{label}</InputLabel>
          <Select
            {...field}
            inputRef={ref}
            labelId={labelId}
            label={label}
            className={selectClassName}
            displayEmpty={displayEmpty}
            renderValue={(value) =>
              options.find((option) => option.value === value)?.label ?? ""
            }
            MenuProps={{
              slotProps: {
                paper: { className: "ledger-category-menu" },
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
          </Select>
          <FormHelperText>
            {fieldState.error?.message ?? helperText}
          </FormHelperText>
        </FormControl>
      )}
    />
  );
}

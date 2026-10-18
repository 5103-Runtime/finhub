import { useId, type ReactNode } from "react";
import { Controller, useFormContext, type FieldValues, type Path } from "react-hook-form";
import { FormControl, FormHelperText, InputLabel, MenuItem, Select, type FormControlProps } from "@mui/material";

export type SelectOption = { value: string; label: string };

export type SelectFormFieldProps<T extends FieldValues> = {
  name: Path<T>;
  label: string;
  options: SelectOption[];
  helperText?: ReactNode;
} & Omit<FormControlProps, "error" | "children">;

export function SelectFormField<T extends FieldValues>({
  name,
  label,
  options,
  helperText,
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
          <Select {...field} inputRef={ref} labelId={labelId} label={label}>
            {options.map((option) => (
              <MenuItem key={option.value} value={option.value}>
                {option.label}
              </MenuItem>
            ))}
          </Select>
          <FormHelperText>{fieldState.error?.message ?? helperText}</FormHelperText>
        </FormControl>
      )}
    />
  );
}
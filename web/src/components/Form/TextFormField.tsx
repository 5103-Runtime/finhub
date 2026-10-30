import {
  type FieldValues,
  type Path,
  useFormContext,
  Controller,
} from "react-hook-form";
import { type TextFieldProps, TextField } from "@mui/material";

export type TextFieldFormProps<T extends FieldValues> = {
  name: Path<T>;
} & Omit<TextFieldProps, "name">;

export function TextFormField<T extends FieldValues>({
  name,
  helperText,
  ...props
}: TextFieldFormProps<T>) {
  const { control } = useFormContext<T>();
  return (
    <Controller
      name={name}
      control={control}
      render={({ field: { ref, ...field }, fieldState }) => (
        <TextField
          {...props}
          {...field}
          inputRef={ref}
          error={Boolean(fieldState.error)}
          helperText={fieldState.error?.message ?? helperText}
        />
      )}
    />
  );
}

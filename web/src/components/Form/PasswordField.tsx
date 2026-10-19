import { type FieldValues } from "react-hook-form";
import { useState } from "react";
import { InputAdornment, IconButton } from "@mui/material";
import { VisibilityOff, Visibility } from "@mui/icons-material";
import { TextFormField, type TextFieldFormProps } from "./TextFormField";

export function PasswordField<T extends FieldValues>(
  props: TextFieldFormProps<T>,
) {
  const [show, setShow] = useState(false);
  return (
    <TextFormField<T>
      {...props}
      type={show ? "text" : "password"}
      slotProps={{
        input: {
          endAdornment: (
            <InputAdornment position="end">
              <IconButton
                edge="end"
                onClick={() => setShow((s) => !s)}
                aria-label={show ? "Hide password" : "Show password"}
              >
                {show ? <VisibilityOff /> : <Visibility />}
              </IconButton>
            </InputAdornment>
          ),
        },
      }}
    />
  );
}

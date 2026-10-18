import { useFormContext } from "react-hook-form";
import { type ButtonProps, Button } from "@mui/material";

export function SubmitButton({ children, ...props }: ButtonProps) {
  const { formState: { isSubmitting } } = useFormContext();
  return (
    <Button type="submit" variant="contained" className="primary-action"
            loading={isSubmitting} {...props}>
      {children}
    </Button>
  );
}
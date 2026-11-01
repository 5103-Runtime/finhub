import { useFormContext, type UseFormReturn } from "react-hook-form";
import { type ButtonProps, Button } from "@mui/material";

export function SubmitButton({ children, loading, ...props }: ButtonProps) {
  // useFormContext returns null when there is no FormProvider.
  // This causes an issue with submitting in a dialog so the lines below were added
  const context = useFormContext() as UseFormReturn | null;
  const isLoading = loading ?? context?.formState.isSubmitting ?? false;

  return (
    <Button
      type="submit"
      variant="contained"
      className="primary-action"
      loading={isLoading}
      {...props}
    >
      {children}
    </Button>
  );
}

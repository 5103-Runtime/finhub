import { useId, type ReactNode } from "react";
import { Button, Stack } from "@mui/material";
import type { FieldValues, SubmitHandler, UseFormReturn, FormProvider} from "react-hook-form";
import { Form } from "@app/components/Form/Form";
import { FormAlert } from "@app/components/Form/FormAlert";
import { SubmitButton } from "@app/components/Form/SubmitButton";
import { DialogBase, type DialogBaseProps } from "./DialogBase";

type FormDialogProps<T extends FieldValues> = Omit<
  DialogBaseProps,
  "actions" | "busy" | "children"
> & {
  form: UseFormReturn<T>;
  onSubmit: SubmitHandler<T>;
  submitLabel: string;
  error?: string;
  children: ReactNode;
};

export function FormDialog<T extends FieldValues>({
  form, onSubmit, submitLabel, error, onClose, children, ...dialogProps
}: FormDialogProps<T>) {
  const formId = useId();
  const { isSubmitting } = form.formState;

  return (
    
    <DialogBase
      {...dialogProps}
      onClose={onClose}
      busy={isSubmitting}
      actions={
        <>
          <Button className="dialog-cancel-button" onClick={onClose} disabled={isSubmitting}>
            Cancel
          </Button>
          <SubmitButton
            form={formId}
            className="dialog-primary-button"
            variant="text"
          >
            {submitLabel}
          </SubmitButton>
        </>
      }
    >
      <Form id={formId} form={form} onSubmit={onSubmit}>
        <Stack spacing={2.25}>
          {children}
          <FormAlert severity="error">{error}</FormAlert>
        </Stack>
      </Form>
    </DialogBase>
  );
}
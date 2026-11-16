import type { ReactNode } from "react";
import { Button, Typography } from "@mui/material";
import { DialogBase } from "./DialogBase";
import { FormAlert } from "@app/components/Form/FormAlert";

type ConfirmDialogProps = {
  open: boolean;
  title: string;
  message: ReactNode;
  confirmLabel: string;
  secondaryLabel?: string;
  error?: string;
  loading?: boolean;
  onConfirm: () => void;
  onSecondary?: () => void;
  onCancel: () => void;
  onExited?: () => void;
};

export function ConfirmDialog({
  open,
  title,
  message,
  confirmLabel,
  secondaryLabel,
  error,
  loading = false,
  onConfirm,
  onSecondary,
  onCancel,
  onExited,
}: ConfirmDialogProps) {
  return (
    <DialogBase
      open={open}
      title={title}
      maxWidth="xs"
      busy={loading}
      onClose={onCancel}
      onExited={onExited}
      actions={
        <>
          <Button
            className="dialog-cancel-button"
            onClick={onCancel}
            disabled={loading}
          >
            Cancel
          </Button>
          {secondaryLabel && onSecondary && (
            <Button
              className="dialog-cancel-button"
              onClick={onSecondary}
              disabled={loading}
            >
              {secondaryLabel}
            </Button>
          )}
          <Button
            className="dialog-danger-button"
            onClick={onConfirm}
            loading={loading}
          >
            {confirmLabel}
          </Button>
        </>
      }
    >
      <Typography component="div">{message}</Typography>
      <FormAlert severity="error">{error}</FormAlert>
    </DialogBase>
  );
}

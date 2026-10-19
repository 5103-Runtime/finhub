import type { ReactNode } from "react";
import { Button, Typography } from "@mui/material";
import { DialogBase } from "./DialogBase";

type ConfirmDialogProps = {
  open: boolean;
  title: string;
  message: ReactNode;
  confirmLabel: string;
  loading?: boolean;
  onConfirm: () => void;
  onCancel: () => void;
  onExited?: () => void;
};

export function ConfirmDialog({
  open, title, message, confirmLabel, loading = false,
  onConfirm, onCancel, onExited,
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
          <Button className="dialog-cancel-button" onClick={onCancel} disabled={loading}>
            Cancel
          </Button>
          <Button className="dialog-danger-button" onClick={onConfirm} loading={loading}>
            {confirmLabel}
          </Button>
        </>
      }
    >
      <Typography>{message}</Typography>
    </DialogBase>
  );
}
import type { ReactNode } from "react";
import {
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  type DialogProps,
} from "@mui/material";
import "./Dialog.css";

export type DialogBaseProps = {
  open: boolean;
  title: string;
  onClose: () => void;
  onExited?: () => void;
  busy?: boolean;
  maxWidth?: DialogProps["maxWidth"];
  actions?: ReactNode;
  children: ReactNode;
};

export function DialogBase({
  open,
  title,
  onClose,
  onExited,
  busy = false,
  maxWidth = "sm",
  actions,
  children,
}: DialogBaseProps) {
  return (
    <Dialog
      open={open}
      fullWidth
      maxWidth={maxWidth}
      className="app-dialog"
      onClose={() => {
        if (!busy) onClose();
      }}
      slotProps={{ transition: { onExited } }}
    >
      <DialogTitle>{title}</DialogTitle>
      <DialogContent>{children}</DialogContent>
      {actions && <DialogActions>{actions}</DialogActions>}
    </Dialog>
  );
}

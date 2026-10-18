import { Alert, Typography } from "@mui/material";
import type { ReactNode } from "react";

export function FormAlert({ severity, children }: { severity: "error" | "success"; children?: ReactNode }) {
  if (!children) return null;
  const isError = severity === "error";
  return (
    <Alert className={isError ? "auth-error" : "auth-success"} severity={severity}
           variant="outlined" role={isError ? "alert" : "status"}>
      <Typography className={isError ? "auth-error-message" : "auth-success-message"}>
        {children}
      </Typography>
    </Alert>
  );
}
import { Alert, Typography } from "@mui/material";
import type { ReactNode } from "react";

export function FormAlert({
  severity,
  children,
}: {
  severity: "error" | "warning" | "success";
  children?: ReactNode;
}) {
  if (!children) return null;
  const isError = severity === "error";
  const className = `auth-${severity}`;
  return (
    <Alert
      className={className}
      severity={severity}
      variant="outlined"
      role={isError ? "alert" : "status"}
    >
      <Typography className={`${className}-message`}>{children}</Typography>
    </Alert>
  );
}

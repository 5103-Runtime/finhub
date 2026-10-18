import { type FieldValues, type UseFormSetError, type Path } from "react-hook-form";
import { ApiError } from "@app/services/apiClient";

export function getErrorMessage(err: unknown, fallback: string) {
  return err instanceof ApiError ? err.message : fallback;
}

export function applyFieldErrors<T extends FieldValues>(
  err: unknown, setError: UseFormSetError<T>, knownFields: string[],
) {
  if (!(err instanceof ApiError) || !err.fieldErrors) return;
  for (const [field, messages] of Object.entries(err.fieldErrors)) {
    if (knownFields.includes(field) && messages[0]) {
      setError(field as Path<T>, { type: "server", message: messages[0] });
    }
  }
}
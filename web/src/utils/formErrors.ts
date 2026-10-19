import type {FieldValues, UseFormSetError, Path } from "react-hook-form";
import { ApiError } from "@app/services/apiClient";

/**
 * Returns a user-facing message for an error caught while submitting a form.
 * @param err The value caught in a `catch` block.
 * @param fallback The message to use when `err` is not an `ApiError`.
 * @returns The `ApiError` message, or `fallback`.
 *
 * @example
 * try {
 *   await createAccount(data);
 * } catch (err) {
 *   setServerError(getErrorMessage(err, "Unable to save the account."));
 * }
 */
export function getErrorMessage(err: unknown, fallback: string) {
  return err instanceof ApiError ? err.message : fallback;
}

/**
 * Copies field validation errors from an `ApiError` onto a
 * react-hook-form form, so they appear under the matching inputs.
 *
 * @param err The value caught in a `catch` block.
 * @param setError The `setError` function from `useForm`.
 * @param knownFields Names of the form fields that may receive server errors,
 * for example `Object.keys(emptyAccountForm)`.
 *
 * @example
 * try {
 *   await createAccount(data);
 * } catch (err) {
 *   applyFieldErrors(err, form.setError, Object.keys(emptyAccountForm));
 *   setServerError(getErrorMessage(err, "Unable to save the account."));
 * }
 */
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
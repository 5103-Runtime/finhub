/**
 * @brief The largest statement file the upload accepts, in bytes. The API refuses a
 * request body over 1 MiB, so this leaves a little room for the form fields sent with the file.
 */
export const MAX_STATEMENT_BYTES = 1024 * 1024 - 1024;

/**
 * @brief Checks that a file can be uploaded as a bank statement.
 * The type is judged by the file extension, because browsers report CSV files with
 * different MIME types depending on the computer (for example, when Excel is installed).
 * @param file The file the user chose.
 * @returns A message describing the problem, or null when the file is valid.
 */
export function validateStatementFile(file: File): string | null {
  if (!file.name.toLowerCase().endsWith(".csv")) {
    return "Only CSV files are supported. Choose a .csv export from your bank.";
  }

  if (file.size === 0) {
    return "This file is empty. Choose a different export.";
  }

  if (file.size > MAX_STATEMENT_BYTES) {
    return "This file is too large. Statement exports must be under 1 MB.";
  }

  return null;
}

/**
 * @brief Formats a file size for display.
 * @param bytes The size in bytes.
 * @returns The size in B or KB, such as "812 B" or "4.2 KB".
 */
export function formatFileSize(bytes: number): string {
  if (bytes < 1024) {
    return `${bytes} B`;
  }

  return `${(bytes / 1024).toFixed(1)} KB`;
}
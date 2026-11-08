/**
 * @brief Display names for the banks the statement parser has mappings for, keyed by the
 * `bank` value the API returns. Add an entry when a new mapping file is added to the API.
 */
const bankLabels: Record<string, string> = {
    rbc: "RBC",
    scotiabank: "Scotiabank",
};

/**
 * Gets the display name for a bank key returned by the statement API.
 * @param bank The API's bank key, such as "rbc".
 * @returns The display name, or the key with a capital letter when the bank is not known here.
 */
export function getBankLabel(bank: string): string {
    return bankLabels[bank] ?? bank.charAt(0).toUpperCase() + bank.slice(1);
}

/**
 * Checks whether an account's institution is exactly the bank a statement was detected as.
 * Surrounding spaces and letter case are ignored, nothing else, so "Royal Bank" is not "RBC".
 * This can be changed if needed
 * @param institution The institution saved on the account, if any.
 * @param bank The API's bank key for the parsed statement.
 * @returns "unknown" when the account has no institution or the bank is not listed here,
 * otherwise "match" or "mismatch".
 */
export function checkInstitution(
    institution: string | null | undefined,
    bank: string,
): "match" | "mismatch" | "unknown" {
    const saved = institution?.trim().toLowerCase() ?? "";
    const label = bankLabels[bank]?.toLowerCase();
    if (saved === "" || !label) {
        return "unknown";
    }

    return saved === label ? "match" : "mismatch";
}
namespace FinHub.Api.DTOs.Statement;

/// <summary>
/// Represents the response body for a parsed statement. Nothing in it has been saved.
/// </summary>
/// <param name="Bank">The bank the statement's mapping belongs to, such as rbc.</param>
/// <param name="AccountType">The account type the mapping is for: chequing, savings, or credit_card.</param>
/// <param name="SkippedRows">How many rows were left out because their status is not one the mapping imports.</param>
/// <param name="Transactions">The candidate transactions, in file order.</param>
public sealed record ParsedStatementResponse(
    string Bank,
    string AccountType,
    int SkippedRows,
    IReadOnlyList<ParsedTransactionResponse> Transactions);

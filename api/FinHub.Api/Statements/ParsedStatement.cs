namespace FinHub.Api.Statements;

/// <summary>
/// The result of parsing one uploaded statement. Nothing in it has been saved.
/// </summary>
/// <param name="Bank">The bank the mapping belongs to, such as rbc.</param>
/// <param name="AccountType">The account type the mapping is for: chequing, savings, or credit_card.</param>
/// <param name="Transactions">The candidate transactions, in file order.</param>
/// <param name="SkippedRows">How many rows were left out because their status is not one the mapping imports.</param>
public sealed record ParsedStatement(
    string Bank,
    string AccountType,
    IReadOnlyList<ParsedTransaction> Transactions,
    int SkippedRows);

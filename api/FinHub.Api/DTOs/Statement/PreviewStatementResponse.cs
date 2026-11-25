namespace FinHub.Api.DTOs.Statement;

/// <summary>
/// Represents a statement preview returned to a guest. Nothing in it has been saved.
/// </summary>
/// <param name="Bank">The bank the statement's mapping belongs to, such as rbc.</param>
/// <param name="AccountType">The account type the mapping is for.</param>
/// <param name="SkippedRows">How many rows were left out because their status is not imported.</param>
/// <param name="TotalTransactions">The total number of parsed candidate transactions.</param>
/// <param name="Transactions">The candidate transactions, in file order.</param>
public sealed record PreviewStatementResponse(
    string Bank,
    string AccountType,
    int SkippedRows,
    int TotalTransactions,
    IReadOnlyList<PreviewStatementTransactionResponse> Transactions);

/// <summary>
/// Represents one candidate transaction in a guest statement preview.
/// </summary>
/// <param name="StatementLine">The line number in the uploaded statement.</param>
/// <param name="TransactionDate">The date of the transaction.</param>
/// <param name="Description">The transaction description.</param>
/// <param name="Amount">The signed transaction amount.</param>
/// <param name="BalanceAfter">The statement's balance after the transaction, if present.</param>
public sealed record PreviewStatementTransactionResponse(
    int StatementLine,
    DateOnly TransactionDate,
    string Description,
    decimal Amount,
    decimal? BalanceAfter);

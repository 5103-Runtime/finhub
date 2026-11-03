namespace FinHub.Api.DTOs.Statement;

/// <summary>
/// Represents one candidate transaction from a parsed statement.
/// </summary>
/// <param name="StatementLine">The line the row starts on in the file, counting the first line as 1.</param>
/// <param name="TransactionDate">The date the bank reports for the line.</param>
/// <param name="Description">The description of the transaction.</param>
/// <param name="Amount">The signed amount: positive for money in, negative for money out.</param>
/// <param name="BalanceAfter">The balance the statement reports after this line, if any.</param>
/// <param name="PotentialDuplicate">Whether the account already holds a transaction with the same date, description, and amount.</param>
public sealed record ParsedTransactionResponse(
    int StatementLine,
    DateOnly TransactionDate,
    string Description,
    decimal Amount,
    decimal? BalanceAfter,
    bool PotentialDuplicate);

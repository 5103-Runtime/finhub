namespace FinHub.Api.Statements;

/// <summary>
/// One statement line turned into a candidate transaction, in the ledger's sign convention.
/// </summary>
/// <param name="StatementLine">The line the row starts on in the file, counting the first line as 1.</param>
/// <param name="TransactionDate">The date the bank reports for the line.</param>
/// <param name="Description">The mapping's description columns joined, trimmed, flattened to one line, and capped at 500 characters.</param>
/// <param name="Amount">The amount, money in positive.</param>
/// <param name="BalanceAfter">The balance the export reports after this line, or null when it has none.</param>
public sealed record ParsedTransaction(
    int StatementLine,
    DateOnly TransactionDate,
    string Description,
    decimal Amount,
    decimal? BalanceAfter);

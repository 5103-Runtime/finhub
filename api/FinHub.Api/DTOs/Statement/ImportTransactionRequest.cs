using System.ComponentModel.DataAnnotations;

namespace FinHub.Api.DTOs.Statement;

/// <summary>
/// Represents one confirmed line of a parsed statement to save as a transaction.
/// </summary>
/// <param name="StatementLine">The line the row starts on in the file, counting the first line as 1.</param>
/// <param name="TransactionDate">The date the transaction occurred.</param>
/// <param name="Description">The description of the transaction.</param>
/// <param name="Amount">The signed amount: positive for money in, negative for money out.</param>
/// <param name="BalanceAfter">The balance the statement reports after this line, if any.</param>
/// <param name="CategoryId">The optional category ID.</param>
/// <param name="ConfirmPotentialDuplicate">Whether to save the line although the account already holds a matching transaction.</param>
public sealed record ImportTransactionRequest(
    [Required, Range(1, int.MaxValue)] int? StatementLine,
    [Required] DateOnly? TransactionDate,
    [Required, StringLength(500)] string Description,
    [Required, Range(typeof(decimal), "-999999999999.99", "999999999999.99")] decimal? Amount,
    [Range(typeof(decimal), "-999999999999.99", "999999999999.99")] decimal? BalanceAfter,
    Guid? CategoryId,
    bool ConfirmPotentialDuplicate = false);

using System.Text.Json.Serialization;

namespace FinHub.Api.DTOs.Transaction;

/// <summary>
/// Represents the response body for a transaction.
/// </summary>
/// <param name="Id">The ID of the transaction.</param>
/// <param name="AccountId">The ID of the account the transaction belongs to.</param>
/// <param name="TransferId">The optional transfer that created the transaction.</param>
/// <param name="CategoryId">The optional category ID.</param>
/// <param name="TransactionDate">The date the transaction occurred.</param>
/// <param name="Description">The description of the transaction.</param>
/// <param name="Amount">The signed amount: positive for money in, negative for money out.</param>
/// <param name="BalanceAfter">The balance the statement reports after this line, if any.</param>
/// <param name="StatementLine">The line's position on the source statement; null for manual entries.</param>
/// <param name="CreatedAt">The date and time when the transaction was created.</param>
/// <param name="UpdatedAt">The date and time when the transaction was last updated.</param>
/// <param name="PotentialDuplicate">Whether the transaction may duplicate an existing transaction.</param>

public sealed record TransactionResponse(
    Guid Id,
    Guid AccountId,
    Guid? TransferId,
    Guid? CategoryId,
    DateOnly TransactionDate,
    string Description,
    decimal Amount,
    decimal? BalanceAfter,
    int? StatementLine,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    bool? PotentialDuplicate = null
);

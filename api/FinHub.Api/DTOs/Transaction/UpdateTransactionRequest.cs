using System.ComponentModel.DataAnnotations;

namespace FinHub.Api.DTOs.Transaction;

/// <summary>
/// represents the request body for updating a transaction. The account cannot be changed.
/// </summary>
/// <param name="CategoryId">The optional category ID.</param>
/// <param name="TransactionDate"> The date the transaction occurred.</param>
/// <param name="Description"> The description of the transaction.</param>
/// <param name="Amount"> The signed amount: positive for money in, negative for money out.</param>
/// <param name="ConfirmPotentialDuplicate">Whether to save after the API reports a potential duplicate.</param>
public sealed record UpdateTransactionRequest(
    Guid? CategoryId,
    [Required] DateOnly? TransactionDate,
    [Required, StringLength(500)] string Description,
    [Required, Range(typeof(decimal), "-999999999999.99", "999999999999.99")] decimal? Amount,
    bool ConfirmPotentialDuplicate = false
);

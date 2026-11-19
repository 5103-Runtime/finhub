using System.ComponentModel.DataAnnotations;

namespace FinHub.Api.DTOs.Transaction;

/// <summary>
/// Represents the request body for manually creating a transaction.
/// </summary>
/// <param name="AccountId">The ID of the account the transaction belongs to.</param>
/// <param name="CategoryId">The optional category ID.</param>
/// <param name="TransactionDate">The date the transaction occurred.</param>
/// <param name="Description">The description of the transaction.</param>
/// <param name="Amount">The signed amount: positive for money in, negative for money out.</param>
/// <param name="ConfirmPotentialDuplicate">Whether to save after the API reports a potential duplicate.</param>
/// <param name="CategoryWasExplicitlySet">Whether the category value, including null, was chosen explicitly by the user.</param>
public sealed record CreateTransactionRequest(
    [Required] Guid? AccountId,
    Guid? CategoryId,
    [Required] DateOnly? TransactionDate,
    [Required, StringLength(500)] string Description,
    [Required, Range(typeof(decimal), "-999999999999.99", "999999999999.99")] decimal? Amount,
    bool ConfirmPotentialDuplicate = false,
    bool CategoryWasExplicitlySet = false);

using System.ComponentModel.DataAnnotations;

namespace FinHub.Api.DTOs.Transfer;

/// <summary>
/// Represents a request to move money between two accounts owned by the user.
/// </summary>
/// <param name="FromAccountId">The account sending the money.</param>
/// <param name="ToAccountId">The account receiving the money.</param>
/// <param name="TransferDate">The date the transfer occurred.</param>
/// <param name="Description">The description stored on both ledger lines, up to 500 characters.</param>
/// <param name="Amount">The positive amount to move.</param>
/// <param name="CategoryId">The optional category applied to newly created ledger lines.</param>
/// <param name="FromTransactionId">An optional existing unlinked outgoing transaction to reuse.</param>
/// <param name="ToTransactionId">An optional existing unlinked incoming transaction to reuse.</param>
public sealed record CreateTransferRequest(
    [Required] Guid? FromAccountId,
    [Required] Guid? ToAccountId,
    [Required] DateOnly? TransferDate,
    [Required, StringLength(500)] string Description,
    [Required, Range(typeof(decimal), "0.01", "999999999999.99")] decimal? Amount,
    Guid? CategoryId = null,
    Guid? FromTransactionId = null,
    Guid? ToTransactionId = null);

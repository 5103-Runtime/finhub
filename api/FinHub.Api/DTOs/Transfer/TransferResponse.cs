namespace FinHub.Api.DTOs.Transfer;

/// <summary>
/// Represents a transfer and its two linked ledger lines.
/// </summary>
/// <param name="Id">The transfer identifier.</param>
/// <param name="FromAccountId">The account that sent the money.</param>
/// <param name="ToAccountId">The account that received the money.</param>
/// <param name="Amount">The positive amount moved between the accounts.</param>
/// <param name="TransferDate">The date of the transfer.</param>
/// <param name="Description">The transfer description.</param>
/// <param name="FromTransactionId">The linked outgoing transaction identifier.</param>
/// <param name="ToTransactionId">The linked incoming transaction identifier.</param>
public sealed record TransferResponse(
    Guid Id,
    Guid FromAccountId,
    Guid ToAccountId,
    decimal Amount,
    DateOnly TransferDate,
    string Description,
    Guid FromTransactionId,
    Guid ToTransactionId);

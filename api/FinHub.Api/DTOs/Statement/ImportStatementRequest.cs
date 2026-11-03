using System.ComponentModel.DataAnnotations;

namespace FinHub.Api.DTOs.Statement;

/// <summary>
/// Represents the request body for saving the confirmed lines of a parsed statement.
/// </summary>
/// <param name="AccountId">The ID of the account the statement belongs to.</param>
/// <param name="Transactions">The lines to save, in statement order.</param>
public sealed record ImportStatementRequest(
    [Required] Guid? AccountId,
    [Required, MinLength(1)] IReadOnlyList<ImportTransactionRequest> Transactions);

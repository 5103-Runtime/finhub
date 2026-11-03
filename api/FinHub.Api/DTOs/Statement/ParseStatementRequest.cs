using System.ComponentModel.DataAnnotations;

namespace FinHub.Api.DTOs.Statement;

/// <summary>
/// Represents the multipart form body for parsing an uploaded statement.
/// </summary>
/// <param name="File">The bank's export.</param>
/// <param name="AccountId">The ID of the account the statement belongs to.</param>
public sealed record ParseStatementRequest(
    [Required] IFormFile? File,
    [Required] Guid? AccountId);

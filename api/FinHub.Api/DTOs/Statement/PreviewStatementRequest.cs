using System.ComponentModel.DataAnnotations;

namespace FinHub.Api.DTOs.Statement;

/// <summary>
/// Represents the multipart form body for previewing a statement without an account.
/// </summary>
/// <param name="File">The bank's export.</param>
public sealed record PreviewStatementRequest([Required] IFormFile? File);

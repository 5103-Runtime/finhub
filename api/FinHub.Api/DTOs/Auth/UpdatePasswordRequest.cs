using System.ComponentModel.DataAnnotations;

namespace FinHub.Api.DTOs.Auth;

/// <summary>
/// Represents a request to update the authenticated user's password.
/// </summary>
public sealed class UpdatePasswordRequest
{
    /// <summary>
    /// The new password for the user. Must be at least 8 characters long.
    /// </summary>
    [Required]
    [MinLength(8)]
    public string NewPassword { get; init; } = "";
}
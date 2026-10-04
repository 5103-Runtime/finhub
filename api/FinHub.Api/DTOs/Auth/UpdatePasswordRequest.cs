using System.ComponentModel.DataAnnotations;

namespace FinHub.Api.DTOs.Auth;

/// <summary>
/// Represents a request to update the authenticated user's password.
/// </summary>
public sealed class UpdatePasswordRequest
{
    /// <summary>
    /// The user's current password, used to reauthenticate before changing the password.
    /// </summary>
    [Required]
    public string CurrentPassword { get; init; } = "";

    /// <summary>
    /// The new password for the user. Must be at least 8 characters long.
    /// </summary>
    [Required]
    [MinLength(8)]
    public string Password { get; init; } = "";
}
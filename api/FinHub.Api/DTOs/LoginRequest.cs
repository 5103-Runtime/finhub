using System.ComponentModel.DataAnnotations;

namespace FinHub.Api.DTOs;

/// <summary>
/// Represents a request to log into an existing user account.
/// </summary>
public sealed class LoginRequest
{
    /// <summary>
    /// The email address of the user.
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; init; } = "";

    /// <summary>
    /// The user's password.
    /// </summary>
    [Required]
    public string Password { get; init; } = "";
}
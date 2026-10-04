using System.ComponentModel.DataAnnotations;

namespace FinHub.Api.DTOs.Auth;

/// <summary>
/// Represents a request to initiate the password reset process for a user account.
/// </summary>
public sealed class ForgotPasswordRequest
{
    /// <summary>
    /// The email address of the user requesting a password reset. Must be a valid email format.
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; init; } = "";
}
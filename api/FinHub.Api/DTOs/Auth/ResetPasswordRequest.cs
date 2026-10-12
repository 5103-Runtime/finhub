using System.ComponentModel.DataAnnotations;

namespace FinHub.Api.DTOs.Auth;

/// <summary>
/// Represents a request to set a new password using a recovery token from a password reset email.
/// </summary>
public sealed class ResetPasswordRequest
{
    /// <summary>
    /// The recovery access token Supabase put in the reset link.
    /// </summary>
    [Required]
    public string AccessToken {get; init;} = "";

    /// <summary>
    /// The new password for the user. Must be at least 8 characters long.
    /// </summary>
    [Required]
    [MinLength(8)]
    public string NewPassword {get; init;} = "";

}
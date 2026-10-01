using System.ComponentModel.DataAnnotations;

namespace FinHub.Api.DTOs;

/// <summary>
/// Represents a request to register a new user.
/// </summary>
public sealed class RegisterRequest
{
    /// <summary>
    /// The email address of the new user. Must be a valid email format.
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; init; } = "";

    /// <summary>
    /// The password for the new user. Must be at least 8 characters long.
    /// </summary>
    [Required]
    [MinLength(8)]
    public string Password { get; init; } = "";

    /// <summary>
    /// The display name of the new user. Optional, but if provided, must not exceed 100 characters.
    /// </summary>
    [MaxLength(100)]
    public string? FirstName { get; init; }

    /// <summary>
    /// The last name of the new user. Optional, but if provided, must not exceed 100 characters.
    /// </summary>
    [MaxLength(100)]
    public string? LastName { get; init; }

    /// <summary>
    /// The preferred name of the new user. Optional, but if provided, must not exceed 100 characters.
    /// </summary>
    [MaxLength(100)]
    public string? PreferredName { get; init; }
}

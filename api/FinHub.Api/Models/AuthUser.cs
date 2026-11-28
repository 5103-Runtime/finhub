namespace FinHub.Api.Models;

/// <summary>
/// Represents the read-only identity fields used from a Supabase Auth user.
/// </summary>
public sealed class AuthUser
{
    /// <summary>
    /// Gets or sets the unique identifier for the authentication user.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the user's email address.
    /// </summary>
    public string? Email { get; set; }
}

namespace FinHub.Api.Models;

/// <summary>
/// Represents a user's profile in the FinHub application.
/// </summary>
public sealed class Profile
{
    /// <summary>
    /// Gets or sets the unique identifier for the profile.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the first name of the user.
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Gets or sets the last name of the user.
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Gets or sets the preferred name of the user, if any.
    /// </summary>
    public string? PreferredName { get; set; }

    /// <summary>
    /// Gets or sets the date of birth of the user.
    /// </summary>
    public DateOnly? DateOfBirth { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the profile was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the profile was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

namespace FinHub.Api.Models;

/// <summary>
/// Represents a category or account a user has excluded from insights.
/// </summary>
public sealed class InsightMute
{
    /// <summary>
    /// Gets or sets the unique identifier for the mute.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who owns the mute.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the muted category; null when the mute targets an account.
    /// </summary>
    public Guid? CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the muted account; null when the mute targets a category.
    /// </summary>
    public Guid? AccountId { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the mute was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

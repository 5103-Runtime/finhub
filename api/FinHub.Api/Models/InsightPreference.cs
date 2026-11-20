namespace FinHub.Api.Models;

/// <summary>
/// Represents a user's settings for one insight type.
/// </summary>
public sealed class InsightPreference
{
    /// <summary>
    /// Gets or sets the unique identifier for the preference.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who owns the preference.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the insight type the preference applies to.
    /// </summary>
    public string InsightType { get; set; } = "";

    /// <summary>
    /// Gets or sets whether the insight type is evaluated for the user.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the user's threshold; null means the API default applies.
    /// </summary>
    public decimal? Threshold { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the preference was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the preference was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

namespace FinHub.Api.Models;

/// <summary>
/// Represents the read and dismissed markers for one insight.
/// </summary>
public sealed class InsightState
{
    /// <summary>
    /// Gets or sets the unique identifier for the state.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who owns the state.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the type of the insight the state belongs to.
    /// </summary>
    public string InsightType { get; set; } = "";

    /// <summary>
    /// Gets or sets the category of a category spike; null for other types.
    /// </summary>
    public Guid? CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the transaction of an unusual purchase; null for other types.
    /// </summary>
    public Guid? TransactionId { get; set; }

    /// <summary>
    /// Gets or sets the currency of a category spike; null for other types.
    /// </summary>
    public string? Currency { get; set; }

    /// <summary>
    /// Gets or sets the first day of the month a category spike covers; null for other types.
    /// </summary>
    public DateOnly? PeriodStart { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the insight was read, if it was.
    /// </summary>
    public DateTime? ReadAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the insight was dismissed, if it was.
    /// </summary>
    public DateTime? DismissedAt { get; set; }

    /// <summary>
    /// Gets or sets the positive amount the insight showed when it was dismissed.
    /// </summary>
    public decimal? DismissedValue { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the state was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the state was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

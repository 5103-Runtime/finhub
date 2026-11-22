using System.ComponentModel.DataAnnotations;

namespace FinHub.Api.DTOs.Insight;

/// <summary>
/// Identifies the computed insight whose read or dismissed state is being changed.
/// </summary>
public sealed class UpdateInsightStateRequest
{
    /// <summary>
    /// Gets or sets the insight type.
    /// </summary>
    [Required]
    [RegularExpression("^(category_spike|unusual_purchase)$")]
    public string InsightType { get; set; } = "";

    /// <summary>
    /// Gets or sets the category ID for a category-spike insight.
    /// </summary>
    public Guid? CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the transaction ID for an unusual-purchase insight.
    /// </summary>
    public Guid? TransactionId { get; set; }

    /// <summary>
    /// Gets or sets the returned three-letter currency code.
    /// It is required for category spikes and ignored for unusual purchases.
    /// </summary>
    [RegularExpression("^[A-Z]{3}$")]
    public string? Currency { get; set; }

    /// <summary>
    /// Gets or sets the first day of the month for a category-spike insight.
    /// </summary>
    public DateOnly? PeriodStart { get; set; }

}

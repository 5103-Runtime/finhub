using System.ComponentModel.DataAnnotations;

namespace FinHub.Api.DTOs.Insight;

/// <summary>
/// Updates the authenticated user's preference for one insight type.
/// </summary>
public sealed class UpdateInsightPreferenceRequest
{
    /// <summary>
    /// Gets or sets whether the insight type is enabled.
    /// </summary>
    [Required]
    public bool? IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the threshold, or null to use the API default.
    /// </summary>
    [Range(typeof(decimal), "0.01", "9999.99")]
    public decimal? Threshold { get; set; }
}

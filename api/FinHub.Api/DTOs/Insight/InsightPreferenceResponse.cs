namespace FinHub.Api.DTOs.Insight;

/// <summary>
/// Represents the authenticated user's preference for one insight type.
/// </summary>
/// <param name="Id">The preference identifier.</param>
/// <param name="InsightType">The insight type the preference applies to.</param>
/// <param name="IsEnabled">Whether the insight type is enabled.</param>
/// <param name="Threshold">The custom threshold, or null for the API default.</param>
/// <param name="CreatedAt">The date and time when the preference was created.</param>
/// <param name="UpdatedAt">The date and time when the preference was last updated.</param>
public sealed record InsightPreferenceResponse(
    Guid Id,
    string InsightType,
    bool IsEnabled,
    decimal? Threshold,
    DateTime CreatedAt,
    DateTime UpdatedAt);

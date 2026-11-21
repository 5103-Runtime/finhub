namespace FinHub.Api.DTOs.Insight;

/// <summary>
/// Represents one computed insight that can be treated as an alert.
/// </summary>
/// <param name="Id">A stable identifier for the insight subject.</param>
/// <param name="InsightType">The type of insight.</param>
/// <param name="Severity">The computed severity.</param>
/// <param name="CategoryId">The category for a category-spike insight.</param>
/// <param name="TransactionId">The transaction for an unusual-purchase insight.</param>
/// <param name="Currency">The currency for a category-spike insight.</param>
/// <param name="PeriodStart">The month start for a category-spike insight.</param>
/// <param name="Amount">The amount the insight is about.</param>
/// <param name="Baseline">The amount the insight was compared against.</param>
/// <param name="IsRead">Whether the insight is currently read.</param>
public sealed record InsightAlertResponse(
    string Id,
    string InsightType,
    string Severity,
    Guid? CategoryId,
    Guid? TransactionId,
    string? Currency,
    DateOnly? PeriodStart,
    decimal Amount,
    decimal Baseline,
    bool IsRead);


namespace FinHub.Api.DTOs.CategorizationRule;

/// <summary>
/// Represents a categorization rule returned by the API.
/// </summary>
/// <param name="Id">The unique identifier for the rule.</param>
/// <param name="CategoryId">The category assigned by the rule.</param>
/// <param name="AccountId">The optional account to which the rule is limited.</param>
/// <param name="DescriptionContains">The optional text that must occur in a transaction description.</param>
/// <param name="AmountDirection">Whether the rule matches incoming or outgoing amounts.</param>
/// <param name="Priority">The order in which the rule is evaluated for the user.</param>
/// <param name="IsEnabled">Whether the rule is currently evaluated.</param>
/// <param name="CreatedAt">The date and time when the rule was created.</param>
/// <param name="UpdatedAt">The date and time when the rule was last updated.</param>
public sealed record CategorizationRuleResponse(
    Guid Id,
    Guid CategoryId,
    Guid? AccountId,
    string? DescriptionContains,
    string? AmountDirection,
    int Priority,
    bool IsEnabled,
    DateTime CreatedAt,
    DateTime UpdatedAt);

using System.ComponentModel.DataAnnotations;

namespace FinHub.Api.DTOs.CategorizationRule;

/// <summary>
/// Represents the complete desired order of the authenticated user's categorization rules.
/// </summary>
/// <param name="RuleIds">Rule IDs from highest to lowest priority.</param>
public sealed record ReorderCategorizationRulesRequest(
    [Required, MinLength(1)] IReadOnlyList<Guid>? RuleIds) : IValidatableObject
{
    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (RuleIds is null || RuleIds.Count == 0)
        {
            yield return new ValidationResult(
                "At least one categorization rule is required.",
                [nameof(RuleIds)]);
            yield break;
        }

        if (RuleIds.Count != RuleIds.Distinct().Count())
        {
            yield return new ValidationResult(
                "Each categorization rule can only appear once.",
                [nameof(RuleIds)]);
        }
    }
}

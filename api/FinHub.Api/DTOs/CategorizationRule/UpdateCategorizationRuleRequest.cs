using System.ComponentModel.DataAnnotations;

namespace FinHub.Api.DTOs.CategorizationRule;

/// <summary>
/// Represents the request body for updating a categorization rule.
/// </summary>
public sealed record UpdateCategorizationRuleRequest : IValidatableObject
{
    /// <summary>
    /// Gets the category assigned by the rule.
    /// </summary>
    [Required]
    public Guid? CategoryId { get; init; }

    /// <summary>
    /// Gets the optional account to which the rule is limited.
    /// </summary>
    public Guid? AccountId { get; init; }

    /// <summary>
    /// Gets the optional text that must occur in a transaction description.
    /// </summary>
    [StringLength(500)]
    public string? DescriptionContains { get; init; }

    /// <summary>
    /// Gets whether the rule matches incoming or outgoing amounts.
    /// </summary>
    [AllowedValues(null, "in", "out")]
    public string? AmountDirection { get; init; }

    /// <summary>
    /// Gets the order in which the rule is evaluated for the user.
    /// </summary>
    [Required, Range(0, int.MaxValue)]
    public int? Priority { get; init; }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (AccountId is null
            && string.IsNullOrWhiteSpace(DescriptionContains)
            && string.IsNullOrWhiteSpace(AmountDirection))
        {
            yield return new ValidationResult(
                "At least one rule criterion is required.",
                [nameof(AccountId), nameof(DescriptionContains), nameof(AmountDirection)]);
        }
    }
}

namespace FinHub.Api.Models;

/// <summary>
/// Represents a user-owned rule for assigning categories to matching transactions.
/// </summary>
public sealed class CategorizationRule
{
    /// <summary>
    /// Gets or sets the unique identifier for the rule.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who owns the rule.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the category assigned by the rule.
    /// </summary>
    public Guid CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the optional account to which the rule is limited.
    /// </summary>
    public Guid? AccountId { get; set; }

    /// <summary>
    /// Gets or sets the optional text that must occur in a transaction description.
    /// </summary>
    public string? DescriptionContains { get; set; }

    /// <summary>
    /// Gets or sets whether the rule matches incoming or outgoing amounts.
    /// </summary>
    public string? AmountDirection { get; set; }

    /// <summary>
    /// Gets or sets the order in which the rule is evaluated for the user.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Gets or sets whether the rule is currently evaluated.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the rule was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the rule was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

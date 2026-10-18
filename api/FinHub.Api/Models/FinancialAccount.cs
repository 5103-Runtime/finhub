namespace FinHub.Api.Models;

/// <summary>
/// Represents a financial account a user imports statements for.
/// </summary>
public sealed class FinancialAccount
{
    /// <summary>
    /// Gets or sets the unique identifier for the account.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who owns the account.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the display name of the account.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Gets or sets the institution that holds the account, if any.
    /// </summary>
    public string? Institution { get; set; }

    /// <summary>
    /// Gets or sets the account type: chequing, savings, or credit_card.
    /// </summary>
    public string AccountType { get; set; } = "";

    /// <summary>
    /// Gets or sets the ISO 4217 currency code of the account.
    /// </summary>
    public string Currency { get; set; } = "CAD";

    /// <summary>
    /// Gets or sets the date and time when the account was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the account was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

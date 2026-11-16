namespace FinHub.Api.Models;

/// <summary>
/// Represents money moved between two accounts owned by the same user.
/// </summary>
public sealed class Transfer
{
    /// <summary>
    /// Gets or sets the unique identifier for the transfer.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who owns both transfer accounts.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the account sending the money.
    /// </summary>
    public Guid FromAccountId { get; set; }

    /// <summary>
    /// Gets or sets the account receiving the money.
    /// </summary>
    public Guid ToAccountId { get; set; }

    /// <summary>
    /// Gets or sets the positive amount moved between the accounts.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the date of the transfer.
    /// </summary>
    public DateOnly TransferDate { get; set; }

    /// <summary>
    /// Gets or sets the description of the transfer.
    /// </summary>
    public string Description { get; set; } = "";

    /// <summary>
    /// Gets or sets the date and time when the transfer was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the transfer was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

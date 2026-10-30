namespace FinHub.Api.Models;

/// <summary>
/// Represents a single ledger line belonging to a financial account.
/// </summary>
public sealed class Transaction
{
    /// <summary>
    /// Gets or sets the unique identifier for the transaction.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the ID of the account the transaction belongs to.
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// Gets or sets the optional category ID.
    /// </summary>
    public Guid? CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the date the transaction was made on.
    /// </summary>
    public DateOnly TransactionDate { get; set; }

    /// <summary>
    /// Gets or sets the description of the transaction.
    /// </summary>
    public string Description { get; set; } = "";

    /// <summary>
    /// Gets or sets the amount: positive for money in, negative for money out.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the balance reported by the statement, if any.
    /// </summary>
    public decimal? BalanceAfter { get; set; }

    /// <summary>
    /// Gets or sets the line's position on the source statement; null for manual entries.
    /// </summary>
    public int? StatementLine { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the transaction was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the transaction was updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

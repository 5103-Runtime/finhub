namespace FinHub.Api.Models;

/// <summary>
/// Represents a shared system category or a user-owned custom category.
/// </summary>
public sealed class Category
{
    /// <summary>
    /// Gets or sets the unique identifier for the category.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the ID of the owning user, or null for a system category.
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Gets or sets the category's display name.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Gets or sets whether the category is shared and managed by the system.
    /// </summary>
    public bool IsSystem { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the category was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the category was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

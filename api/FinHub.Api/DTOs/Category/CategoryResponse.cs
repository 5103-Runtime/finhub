namespace FinHub.Api.DTOs.Category;

/// <summary>
/// Represents the response body for a category.
/// </summary>
/// <param name="Id">The ID of the category.</param>
/// <param name="Name">The display name of the category.</param>
/// <param name="IsSystem">Whether the category is shared and managed by the system.</param>
/// <param name="CreatedAt">The date and time when the category was created.</param>
/// <param name="UpdatedAt">The date and time when the category was last updated.</param>
public sealed record CategoryResponse(
    Guid Id,
    string Name,
    bool IsSystem,
    DateTime CreatedAt,
    DateTime UpdatedAt);

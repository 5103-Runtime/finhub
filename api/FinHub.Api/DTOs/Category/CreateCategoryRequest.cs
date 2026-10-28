using System.ComponentModel.DataAnnotations;

namespace FinHub.Api.DTOs.Category;

/// <summary>
/// Represents the request body for creating a custom category.
/// </summary>
/// <param name="Name">The display name of the category.</param>
public sealed record CreateCategoryRequest(
    [Required, StringLength(50, MinimumLength = 1), RegularExpression(@".*\S.*")]
    string Name);

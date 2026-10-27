using System.ComponentModel.DataAnnotations;

namespace FinHub.Api.DTOs.Category;

/// <summary>
/// Represents the request body for updating a custom category.
/// </summary>
/// <param name="Name">The display name of the category.</param>
/// <param name="Slug">The lowercase machine-readable identifier of the category.</param>
public sealed record UpdateCategoryRequest(
    [Required, StringLength(50, MinimumLength = 1), RegularExpression(@".*\S.*")]
    string Name,
    [Required, RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$")] string Slug);

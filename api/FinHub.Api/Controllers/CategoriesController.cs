using FinHub.Api.Data;
using FinHub.Api.DTOs.Category;
using FinHub.Api.Models;
using FinHub.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FinHub.Api.Controllers;

/// <summary>
/// Controller responsible for managing system and authenticated-user categories.
/// </summary>
/// <param name="dbContext">The database context.</param>
/// <param name="authClient">The authentication client.</param>
[ApiController]
[Route("api/categories")]
public sealed class CategoriesController(
    FinHubDbContext dbContext,
    SupabaseAuthClient authClient) : AuthenticatedControllerBase(authClient)
{
    /// <summary>
    /// Lists system categories and the authenticated user's custom categories.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The available categories, with system categories first.</returns>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryResponse>>> List(
        CancellationToken cancellationToken)
    {
        (Guid? userId, ActionResult? failure) = await ResolveCurrentUserAsync(cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        List<CategoryResponse> categories = await dbContext.Categories
            .AsNoTracking()
            .Where(category => category.IsSystem || category.UserId == userId!.Value)
            .OrderByDescending(category => category.IsSystem)
            .ThenBy(category => category.Name)
            .Select(category => ToResponse(category))
            .ToListAsync(cancellationToken);

        return Ok(categories);
    }

    /// <summary>
    /// Retrieves a system category or one of the authenticated user's custom categories.
    /// </summary>
    /// <param name="id">The ID of the category.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The category.</returns>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CategoryResponse>> Get(
        Guid id,
        CancellationToken cancellationToken)
    {
        (Guid? userId, ActionResult? failure) = await ResolveCurrentUserAsync(cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        Category? category = await FindVisibleCategoryAsync(id, userId!.Value, track: false, cancellationToken);

        return category is null ? CategoryNotFound() : Ok(ToResponse(category));
    }

    /// <summary>
    /// Creates a custom category for the authenticated user.
    /// </summary>
    /// <param name="request">The category request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created category.</returns>
    [HttpPost]
    public async Task<ActionResult<CategoryResponse>> Create(
        [FromBody] CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        (Guid? userId, ActionResult? failure) = await ResolveCurrentUserAsync(cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        string name = request.Name.Trim();
        string slug = request.Slug;

        ActionResult? conflict = await FindConflictAsync(userId!.Value, name, slug, categoryId: null, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        Category category = new()
        {
            Id = Guid.NewGuid(),
            UserId = userId.Value,
            Name = name,
            Slug = slug,
            IsSystem = false
        };

        dbContext.Categories.Add(category);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            return CategoryConflict();
        }

        return CreatedAtAction(nameof(Get), new { id = category.Id }, ToResponse(category));
    }

    /// <summary>
    /// Updates one of the authenticated user's custom categories.
    /// </summary>
    /// <param name="id">The ID of the category.</param>
    /// <param name="request">The category request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated category.</returns>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CategoryResponse>> Update(
        Guid id,
        [FromBody] UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        (Guid? userId, ActionResult? failure) = await ResolveCurrentUserAsync(cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        Category? category = await FindOwnedCustomCategoryAsync(id, userId!.Value, track: true, cancellationToken);
        if (category is null)
        {
            return CategoryNotFound();
        }

        string name = request.Name.Trim();
        string slug = request.Slug;

        ActionResult? conflict = await FindConflictAsync(userId.Value, name, slug, category.Id, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        category.Name = name;
        category.Slug = slug;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            return CategoryConflict();
        }

        return Ok(ToResponse(category));
    }

    /// <summary>
    /// Deletes one of the authenticated user's custom categories.
    /// </summary>
    /// <param name="id">The ID of the category.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>No content when the category was deleted.</returns>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        (Guid? userId, ActionResult? failure) = await ResolveCurrentUserAsync(cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        Category? category = await FindOwnedCustomCategoryAsync(id, userId!.Value, track: true, cancellationToken);
        if (category is null)
        {
            return CategoryNotFound();
        }

        dbContext.Categories.Remove(category);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Finds a category visible to the authenticated user.
    /// </summary>
    private IQueryable<Category> VisibleCategories(Guid userId, bool track) =>
        (track ? dbContext.Categories : dbContext.Categories.AsNoTracking())
            .Where(category => category.IsSystem || category.UserId == userId);

    /// <summary>
    /// Finds a system category or a custom category owned by the authenticated user.
    /// </summary>
    private Task<Category?> FindVisibleCategoryAsync(
        Guid id,
        Guid userId,
        bool track,
        CancellationToken cancellationToken) =>
        VisibleCategories(userId, track)
            .SingleOrDefaultAsync(category => category.Id == id, cancellationToken);

    /// <summary>
    /// Finds a custom category owned by the authenticated user.
    /// </summary>
    private Task<Category?> FindOwnedCustomCategoryAsync(
        Guid id,
        Guid userId,
        bool track,
        CancellationToken cancellationToken)
    {
        IQueryable<Category> categories = track
            ? dbContext.Categories
            : dbContext.Categories.AsNoTracking();

        return categories.SingleOrDefaultAsync(
            category => category.Id == id
                && !category.IsSystem
                && category.UserId == userId,
            cancellationToken);
    }

    /// <summary>
    /// Checks the database rules that cannot be represented by the request validators.
    /// </summary>
    private async Task<ActionResult?> FindConflictAsync(
        Guid userId,
        string name,
        string slug,
        Guid? categoryId,
        CancellationToken cancellationToken)
    {
        bool systemNameExists = await dbContext.Categories
            .AsNoTracking()
            .AnyAsync(
                category => category.IsSystem
                    && category.Name.ToLower() == name.ToLower(),
                cancellationToken);
        if (systemNameExists)
        {
            return CategoryNameConflict();
        }

        bool systemSlugExists = await dbContext.Categories
            .AsNoTracking()
            .AnyAsync(
                category => category.IsSystem && category.Slug == slug,
                cancellationToken);
        if (systemSlugExists)
        {
            return CategorySlugConflict();
        }

        bool customNameExists = await dbContext.Categories
            .AsNoTracking()
            .AnyAsync(
                category => !category.IsSystem
                    && category.UserId == userId
                    && category.Name.ToLower() == name.ToLower()
                    && (categoryId == null || category.Id != categoryId.Value),
                cancellationToken);
        if (customNameExists)
        {
            return CategoryNameConflict();
        }

        bool customSlugExists = await dbContext.Categories
            .AsNoTracking()
            .AnyAsync(
                category => !category.IsSystem
                    && category.UserId == userId
                    && category.Slug == slug
                    && (categoryId == null || category.Id != categoryId.Value),
                cancellationToken);
        return customSlugExists ? CategorySlugConflict() : null;
    }

    /// <summary>
    /// Converts a category to its response representation.
    /// </summary>
    private static CategoryResponse ToResponse(Category category) => new(
        category.Id,
        category.Name,
        category.Slug,
        category.IsSystem,
        category.CreatedAt,
        category.UpdatedAt);

    /// <summary>
    /// Creates the response returned when no visible category matches the ID.
    /// </summary>
    private NotFoundObjectResult CategoryNotFound() => NotFound(new
    {
        error = "The category could not be found.",
        errorCode = "category_not_found"
    });

    /// <summary>
    /// Creates the response returned when a category name conflicts with another category.
    /// </summary>
    private ConflictObjectResult CategoryNameConflict() => Conflict(new
    {
        error = "A category with this name already exists.",
        errorCode = "category_name_conflict"
    });

    /// <summary>
    /// Creates the response returned when a category slug conflicts with another category.
    /// </summary>
    private ConflictObjectResult CategorySlugConflict() => Conflict(new
    {
        error = "A category with this slug already exists.",
        errorCode = "category_slug_conflict"
    });

    /// <summary>
    /// Creates the response returned when a category conflicts with an existing unique value.
    /// </summary>
    private ConflictObjectResult CategoryConflict() => Conflict(new
    {
        error = "The category conflicts with an existing category.",
        errorCode = "category_conflict"
    });

    /// <summary>
    /// Determines whether the exception is a unique constraint violation.
    /// </summary>
    /// <param name="exception">The exception to check.</param>
    /// <returns>True if the exception is a unique constraint violation; otherwise, false.</returns>
    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation
        };
}

using FinHub.Api.Data;
using FinHub.Api.DTOs.Admin;
using FinHub.Api.Models;
using FinHub.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinHub.Api.Controllers;

/// <summary>
/// Provides read-only access to application users for administrators.
/// </summary>
/// <param name="dbContext">The database context.</param>
/// <param name="authClient">The authentication client.</param>
[ApiController]
[Route("api/admin/users")]
public sealed class UserManagementController(
    FinHubDbContext dbContext,
    SupabaseAuthClient authClient) : AuthenticatedControllerBase(authClient)
{
    /// <summary>
    /// Lists every application user.
    /// </summary>
    /// <param name="sortBy">The field to sort by: email, name (last name, then first name), firstName, lastName, preferredName, role, createdAt, or updatedAt.</param>
    /// <param name="sortOrder">The sort direction: asc or desc.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The users, oldest first.</returns>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserManagementResponse>>> List(
        [FromQuery] string? sortBy,
        [FromQuery] string? sortOrder,
        CancellationToken cancellationToken)
    {
        (Guid? administratorId, ActionResult? failure) =
            await ResolveAdministratorAsync(dbContext, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        BadRequestObjectResult? sortingError = ValidateSorting(sortBy, sortOrder);
        if (sortingError is not null)
        {
            return sortingError;
        }

        var query = dbContext.Profiles
            .AsNoTracking()
            .Join(
                dbContext.AuthUsers.AsNoTracking(),
                profile => profile.Id,
                authUser => authUser.Id,
                (profile, authUser) => new { Profile = profile, AuthUser = authUser });

        var orderedQuery = query
            .OrderBy(user => user.Profile.CreatedAt)
            .ThenBy(user => user.Profile.Id);

        if (!string.IsNullOrWhiteSpace(sortBy))
        {
            bool descending = string.IsNullOrWhiteSpace(sortOrder)
                || sortOrder.Trim().Equals("desc", StringComparison.OrdinalIgnoreCase);
            string field = sortBy.Trim().ToLowerInvariant();

            orderedQuery = field switch
            {
                "email" => descending
                    ? query.OrderByDescending(user => user.AuthUser.Email)
                    : query.OrderBy(user => user.AuthUser.Email),
                "name" => descending
                    ? query.OrderByDescending(user => user.Profile.LastName)
                        .ThenByDescending(user => user.Profile.FirstName)
                    : query.OrderBy(user => user.Profile.LastName)
                        .ThenBy(user => user.Profile.FirstName),
                "firstname" => descending
                    ? query.OrderByDescending(user => user.Profile.FirstName)
                    : query.OrderBy(user => user.Profile.FirstName),
                "lastname" => descending
                    ? query.OrderByDescending(user => user.Profile.LastName)
                    : query.OrderBy(user => user.Profile.LastName),
                "preferredname" => descending
                    ? query.OrderByDescending(user => user.Profile.PreferredName)
                    : query.OrderBy(user => user.Profile.PreferredName),
                "role" => descending
                    ? query.OrderByDescending(user => user.Profile.Role)
                    : query.OrderBy(user => user.Profile.Role),
                "createdat" => descending
                    ? query.OrderByDescending(user => user.Profile.CreatedAt)
                    : query.OrderBy(user => user.Profile.CreatedAt),
                "updatedat" => descending
                    ? query.OrderByDescending(user => user.Profile.UpdatedAt)
                    : query.OrderBy(user => user.Profile.UpdatedAt),
                _ => throw new InvalidOperationException("Validated user sort field was not recognized.")
            };

            orderedQuery = orderedQuery.ThenBy(user => user.Profile.Id);
        }

        List<UserManagementResponse> users = await orderedQuery
            .Select(user => ToResponse(user.Profile, user.AuthUser))
            .ToListAsync(cancellationToken);

        return Ok(users);
    }

    /// <summary>
    /// Validates the requested sorting field and direction.
    /// </summary>
    /// <param name="sortBy">The requested field to sort by.</param>
    /// <param name="sortOrder">The requested sort direction.</param>
    /// <returns>A bad request result when sorting parameters are invalid; otherwise, null.</returns>
    private static BadRequestObjectResult? ValidateSorting(
        string? sortBy,
        string? sortOrder)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
        {
            return string.IsNullOrWhiteSpace(sortOrder)
                ? null
                : new BadRequestObjectResult(new
                {
                    error = "A sortBy field is required when sortOrder is provided.",
                    errorCode = "missing_sort_field"
                });
        }

        string field = sortBy.Trim().ToLowerInvariant();
        if (field != "email" && field != "name" && field != "firstname" && field != "lastname"
            && field != "preferredname" && field != "role" && field != "createdat" && field != "updatedat")
        {
            return new BadRequestObjectResult(new
            {
                error = "Sort by email, name, firstName, lastName, preferredName, role, createdAt, or updatedAt.",
                errorCode = "invalid_sort_field"
            });
        }

        string? direction = sortOrder?.Trim();
        if (!string.IsNullOrWhiteSpace(direction)
            && !direction.Equals("asc", StringComparison.OrdinalIgnoreCase)
            && !direction.Equals("desc", StringComparison.OrdinalIgnoreCase))
        {
            return new BadRequestObjectResult(new
            {
                error = "Sort order must be asc or desc.",
                errorCode = "invalid_sort_order"
            });
        }

        return null;
    }

    /// <summary>
    /// Retrieves one application user.
    /// </summary>
    /// <param name="id">The user ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The user.</returns>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserManagementResponse>> Get(
        Guid id,
        CancellationToken cancellationToken)
    {
        (Guid? administratorId, ActionResult? failure) =
            await ResolveAdministratorAsync(dbContext, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        UserManagementResponse? user = await dbContext.Profiles
            .AsNoTracking()
            .Join(
                dbContext.AuthUsers.AsNoTracking(),
                profile => profile.Id,
                authUser => authUser.Id,
                (profile, authUser) => new { Profile = profile, AuthUser = authUser })
            .Where(item => item.Profile.Id == id)
            .Select(item => ToResponse(item.Profile, item.AuthUser))
            .SingleOrDefaultAsync(cancellationToken);

        return user is null
            ? NotFound(new
            {
                error = "The user could not be found.",
                errorCode = "user_not_found"
            })
            : Ok(user);
    }

    /// <summary>
    /// Converts an application profile to an administrator user response.
    /// </summary>
    /// <param name="profile">The profile to convert.</param>
    /// <returns>The user response.</returns>
    private static UserManagementResponse ToResponse(Profile profile, AuthUser authUser) => new(
        profile.Id,
        authUser.Email,
        profile.FirstName,
        profile.LastName,
        profile.PreferredName,
        profile.DateOfBirth,
        profile.Role,
        profile.CreatedAt,
        profile.UpdatedAt);
}
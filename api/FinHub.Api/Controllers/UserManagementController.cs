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
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The users, oldest first.</returns>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserManagementResponse>>> List(
        CancellationToken cancellationToken)
    {
        (Guid? administratorId, ActionResult? failure) =
            await ResolveAdministratorAsync(dbContext, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        List<UserManagementResponse> users = await dbContext.Profiles
            .AsNoTracking()
            .OrderBy(profile => profile.CreatedAt)
            .ThenBy(profile => profile.Id)
            .Select(profile => ToResponse(profile))
            .ToListAsync(cancellationToken);

        return Ok(users);
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

        Profile? profile = await dbContext.Profiles
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        return profile is null
            ? NotFound(new
            {
                error = "The user could not be found.",
                errorCode = "user_not_found"
            })
            : Ok(ToResponse(profile));
    }

    /// <summary>
    /// Converts an application profile to an administrator user response.
    /// </summary>
    /// <param name="profile">The profile to convert.</param>
    /// <returns>The user response.</returns>
    private static UserManagementResponse ToResponse(Profile profile) => new(
        profile.Id,
        profile.FirstName,
        profile.LastName,
        profile.PreferredName,
        profile.DateOfBirth,
        profile.Role,
        profile.CreatedAt,
        profile.UpdatedAt);
}
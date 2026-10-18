using FinHub.Api.Data;
using FinHub.Api.DTOs.Profile;
using FinHub.Api.Models;
using FinHub.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinHub.Api.Controllers;

/// <summary>
/// Controller responsible for handling user profile-related operations.
/// </summary>
/// <param name="dbContext">The database context.</param>
/// <param name="authClient">The authentication client.</param>
[ApiController]
[Route("api/profile")]
public sealed class ProfileController(
    FinHubDbContext dbContext,
    SupabaseAuthClient authClient) : AuthenticatedControllerBase(authClient)
{
    /// <summary>
    /// Retrieves the authenticated user's profile.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The authenticated user's profile response.</returns>
    [HttpGet]
    public async Task<ActionResult<ProfileResponse>> Get(
        CancellationToken cancellationToken)
    {
        (Guid? userId, ActionResult? failure) = await ResolveCurrentUserAsync(cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        Profile? profile = await dbContext.Profiles
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == userId!.Value, cancellationToken);

        if (profile?.FirstName is null ||
            profile.LastName is null ||
            profile.DateOfBirth is null)
        {
            return NotFound(new
            {
                error = "A complete profile could not be found.",
                errorCode = "profile_not_found"
            });
        }

        return Ok(new ProfileResponse(
            profile.Id,
            profile.FirstName,
            profile.LastName,
            profile.PreferredName,
            profile.DateOfBirth.Value));
    }

    /// <summary>
    /// Updates the user's profile with the provided information.
    /// </summary>
    /// <param name="request">The update profile request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated profile response.</returns>
    [HttpPut]
    public async Task<ActionResult<ProfileResponse>> Update(
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        (Guid? userId, ActionResult? failure) = await ResolveCurrentUserAsync(cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        Profile profile = await dbContext.Profiles
            .SingleOrDefaultAsync(item => item.Id == userId!.Value, cancellationToken)
            ?? new Profile
            {
                Id = userId!.Value
            };

        string firstName = request.FirstName.Trim();
        string lastName = request.LastName.Trim();
        profile.FirstName = firstName;
        profile.LastName = lastName;
        profile.PreferredName = string.IsNullOrWhiteSpace(request.PreferredName)
            ? null
            : request.PreferredName.Trim();
        profile.DateOfBirth = request.DateOfBirth;

        if (dbContext.Entry(profile).State == EntityState.Detached)
        {
            dbContext.Profiles.Add(profile);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new ProfileResponse(
            profile.Id,
            firstName,
            lastName,
            profile.PreferredName,
            request.DateOfBirth));
    }
}

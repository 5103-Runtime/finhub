using FinHub.Api.Data;
using FinHub.Api.Constants;
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
    SupabaseAuthClient authClient) : ControllerBase
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
        string? accessToken = GetAccessToken();
        if (accessToken is null)
        {
            return Unauthorized(new
            {
                error = "Missing or invalid authorization token.",
                errorCode = "missing_token"
            });
        }

        SupabaseUserResult userResult = await authClient.GetUserIdAsync(accessToken, cancellationToken);
        if (userResult.Status == SupabaseUserLookupStatus.ServiceUnavailable)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                error = "Authentication service is temporarily unavailable. Please try again.",
                errorCode = "authentication_service_unavailable"
            });
        }

        if (userResult.UserId is null)
        {
            return Unauthorized(new
            {
                error = "The authorization token is invalid or expired.",
                errorCode = "invalid_token"
            });
        }

        Profile? profile = await dbContext.Profiles
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == userResult.UserId.Value, cancellationToken);

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
        string? accessToken = GetAccessToken();
        if (accessToken is null)
        {
            return Unauthorized(new
            {
                error = "Missing or invalid authorization token.",
                errorCode = "missing_token"
            });
        }

        SupabaseUserResult userResult = await authClient.GetUserIdAsync(accessToken, cancellationToken);
        if (userResult.Status == SupabaseUserLookupStatus.ServiceUnavailable)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                error = "Authentication service is temporarily unavailable. Please try again.",
                errorCode = "authentication_service_unavailable"
            });
        }

        if (userResult.UserId is null)
        {
            return Unauthorized(new
            {
                error = "The authorization token is invalid or expired.",
                errorCode = "invalid_token"
            });
        }

        Profile profile = await dbContext.Profiles
            .SingleOrDefaultAsync(item => item.Id == userResult.UserId.Value, cancellationToken)
            ?? new Profile
            {
                Id = userResult.UserId.Value,
                CreatedAt = DateTime.UtcNow
            };

        string firstName = request.FirstName.Trim();
        string lastName = request.LastName.Trim();
        profile.FirstName = firstName;
        profile.LastName = lastName;
        profile.PreferredName = string.IsNullOrWhiteSpace(request.PreferredName)
            ? null
            : request.PreferredName.Trim();
        profile.DateOfBirth = request.DateOfBirth;
        profile.UpdatedAt = DateTime.UtcNow;

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

    #region Private Helper Methods
    /// <summary>
    /// Retrieves the access token from the HttpOnly authentication cookie.
    /// </summary>
    /// <returns>The access token, or null if it is not found or is invalid.</returns>
    private string? GetAccessToken()
    {
        return Request.Cookies.TryGetValue(AuthCookies.AccessToken, out string? token) &&
            !string.IsNullOrWhiteSpace(token)
                ? token
                : null;
    }
    #endregion
}

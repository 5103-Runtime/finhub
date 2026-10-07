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

        Guid? userId = await authClient.GetUserIdAsync(accessToken, cancellationToken);
        if (userId is null)
        {
            return Unauthorized(new
            {
                error = "The authorization token is invalid or expired.",
                errorCode = "invalid_token"
            });
        }

        Profile profile = await dbContext.Profiles
            .SingleOrDefaultAsync(item => item.Id == userId.Value, cancellationToken)
            ?? new Profile
            {
                Id = userId.Value,
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

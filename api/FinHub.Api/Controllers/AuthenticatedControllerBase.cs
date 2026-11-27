using FinHub.Api.Extensions;
using FinHub.Api.Services;
using FinHub.Api.Data;
using FinHub.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinHub.Api.Controllers;

/// <summary>
/// Base controller for endpoints that act on behalf of the authenticated user.
/// </summary>
/// <param name="authClient">The authentication client.</param>
public abstract class AuthenticatedControllerBase(SupabaseAuthClient authClient) : ControllerBase
{
    /// <summary>
    /// Gets the authentication client.
    /// </summary>
    protected SupabaseAuthClient AuthClient { get; } = authClient;

    /// <summary>
    /// Resolves the authenticated user's ID from the access token cookie.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The user ID, or the failure response the action should return as is.</returns>
    protected async Task<(Guid? UserId, ActionResult? Failure)> ResolveCurrentUserAsync(CancellationToken cancellationToken)
    {
        string? accessToken = Request.GetAccessToken();
        if (accessToken is null)
        {
            return (null, MissingTokenResponse());
        }

        SupabaseUserResult userResult = await AuthClient.GetUserIdAsync(accessToken, cancellationToken);
        if (userResult.Status == SupabaseUserLookupStatus.ServiceUnavailable)
        {
            return (null, AuthenticationServiceUnavailable());
        }

        return userResult.UserId is null ? (null, InvalidTokenResponse()) : (userResult.UserId, null);
    }

    /// <summary>
    /// Resolves the authenticated user and verifies that the application profile has the administrator role.
    /// </summary>
    /// <param name="dbContext">The database context used to look up the application role.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The administrator's ID, or the failure response the action should return as is.</returns>
    protected async Task<(Guid? AdministratorId, ActionResult? Failure)> ResolveAdministratorAsync(
        FinHubDbContext dbContext,
        CancellationToken cancellationToken)
    {
        (Guid? userId, ActionResult? failure) = await ResolveCurrentUserAsync(cancellationToken);
        if (failure is not null)
        {
            return (null, failure);
        }

        Profile? profile = await dbContext.Profiles
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == userId!.Value, cancellationToken);

        if (profile?.Role != "administrator")
        {
            return (null, ForbiddenAdministratorResponse());
        }

        return (userId, null);
    }

    /// <summary>
    /// Creates the response returned when the request carries no access token.
    /// </summary>
    /// <returns>An unauthorized response.</returns>
    protected UnauthorizedObjectResult MissingTokenResponse() => Unauthorized(new
    {
        error = "Missing or invalid authorization token.",
        errorCode = "missing_token"
    });

    /// <summary>
    /// Creates the response returned when the access token is rejected.
    /// </summary>
    /// <returns>An unauthorized response.</returns>
    protected UnauthorizedObjectResult InvalidTokenResponse() => Unauthorized(new
    {
        error = "The authorization token is invalid or expired.",
        errorCode = "invalid_token"
    });

    /// <summary>
    /// Creates the response returned when an authenticated user does not have the administrator role.
    /// </summary>
    /// <returns>A forbidden response.</returns>
    protected ObjectResult ForbiddenAdministratorResponse() => StatusCode(
        StatusCodes.Status403Forbidden,
        new
        {
            error = "Administrator access is required.",
            errorCode = "administrator_required"
        });

    /// <summary>
    /// Creates the response returned when the authentication service is unavailable.
    /// </summary>
    /// <returns>An unavailable-service response.</returns>
    protected ObjectResult AuthenticationServiceUnavailable() => StatusCode(
        StatusCodes.Status503ServiceUnavailable,
        new
        {
            error = "Authentication service is temporarily unavailable. Please try again.",
            errorCode = "authentication_service_unavailable"
        });
}

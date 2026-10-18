using FinHub.Api.Extensions;
using FinHub.Api.Services;
using Microsoft.AspNetCore.Mvc;

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

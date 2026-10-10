using FinHub.Api.DTOs.Auth;
using FinHub.Api.Constants;
using FinHub.Api.Data;
using FinHub.Api.Models;
using FinHub.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace FinHub.Api.Controllers;

/// <summary>
/// Controller responsible for handling authentication-related operations.
/// </summary>
/// <param name="authClient">The authentication client.</param>
/// <param name="dbContext">The database context.</param>
/// <param name="logger">The logger instance.</param>
[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    SupabaseAuthClient authClient,
    FinHubDbContext dbContext,
    ILogger<AuthController> logger) : ControllerBase
{
    /// <summary>
    /// Registers a new user with the provided registration details.
    /// </summary>
    /// <param name="request">The registration request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The authentication response.</returns>
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        AuthResult result = await authClient.RegisterAsync(request, cancellationToken);

        if (!result.Succeeded)
        {
            return StatusCode(result.StatusCode, new
            {
                error = result.Error,
                errorCode = result.ErrorCode
            });
        }

        if (result.Response is null)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                error = "Authentication service returned an incomplete response.",
                errorCode = "invalid_auth_response"
            });
        }

        SetAuthCookies(result.Response);
        return StatusCode(
            result.StatusCode,
            ToClientResponse(result.Response, requiresProfileSetup: true));
    }

    /// <summary>
    /// Logs in a user with the provided login credentials.
    /// </summary>
    /// <param name="request">The login request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The authentication response.</returns>
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        AuthResult result = await authClient.LoginAsync(request, cancellationToken);

        if (!result.Succeeded)
        {
            return StatusCode(result.StatusCode, new
            {
                error = result.Error,
                errorCode = result.ErrorCode
            });
        }

        if (result.Response is null)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                error = "Authentication service returned an incomplete response.",
                errorCode = "invalid_auth_response"
            });
        }

        SetAuthCookies(result.Response);
        bool requiresProfileSetup = !await HasCompleteProfileAsync(
            result.Response.UserId,
            cancellationToken);
        return StatusCode(
            result.StatusCode,
            ToClientResponse(result.Response, requiresProfileSetup));
    }

    /// <summary>
    /// Refreshes the access token using the refresh token cookie.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The refreshed authentication result.</returns>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        string? refreshToken = GetRefreshToken();
        if (refreshToken is null)
        {
            // Avoid returning 401 Unauthorized when the user 
            // first lands on the page and have not logged in yet
            return NoContent();
        }

        AuthResult result = await authClient.RefreshAsync(refreshToken, cancellationToken);
        if (!result.Succeeded)
        {
            DeleteAuthCookiesIfRefreshTokenIsInvalid(result.StatusCode);
            return StatusCode(result.StatusCode, new
            {
                error = result.Error,
                errorCode = result.ErrorCode
            });
        }

        if (result.Response is null)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                error = "Authentication service returned an incomplete response.",
                errorCode = "invalid_auth_response"
            });
        }

        SetAuthCookies(result.Response, refreshToken);
        return Ok(ToClientResponse(result.Response));
    }

    /// <summary>
    /// Retrieves the current authenticated user's session information.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The current authenticated user's session information.</returns>
    [HttpGet("session")]
    public async Task<IActionResult> GetSession(CancellationToken cancellationToken)
    {
        string? accessToken = GetAccessToken();
        string? refreshToken = GetRefreshToken();

        // Anonymous visitors do not need a session refresh.
        if (accessToken is null && refreshToken is null)
        {
            return Ok(UnauthenticatedSession());
        }

        // Refresh the session when only the refresh token remains.
        if (accessToken is null && refreshToken is not null)
        {
            // Refresh the session and update the access token cookie.
            AuthResult refreshResult = await RefreshSessionAsync(refreshToken, cancellationToken);
            if (!refreshResult.Succeeded)
            {
                return RefreshFailureResponse(refreshResult);
            }

            // Ensure the refreshed session includes an access token.
            if (refreshResult.Response?.AccessToken is null)
            {
                return IncompleteAuthResponse();
            }

            accessToken = refreshResult.Response.AccessToken;
        }

        // Validate the current access token with Supabase.
        SupabaseUserResult userResult = await authClient.GetUserIdAsync(accessToken!, cancellationToken);
        if (userResult.Status == SupabaseUserLookupStatus.ServiceUnavailable)
        {
            return AuthenticationServiceUnavailable();
        }

        // If the access token expired, retry validation with a refreshed session.
        if (userResult.Status == SupabaseUserLookupStatus.InvalidToken && refreshToken is not null)
        {
            AuthResult refreshResult = await RefreshSessionAsync(refreshToken, cancellationToken);
            if (!refreshResult.Succeeded)
            {
                return RefreshFailureResponse(refreshResult);
            }

            if (refreshResult.Response?.AccessToken is null)
            {
                return IncompleteAuthResponse();
            }

            userResult = await authClient.GetUserIdAsync(
                refreshResult.Response.AccessToken,
                cancellationToken);
            if (userResult.Status == SupabaseUserLookupStatus.ServiceUnavailable)
            {
                return AuthenticationServiceUnavailable();
            }
        }

        // Return an unauthenticated session when no valid user was found.
        if (userResult.UserId is null)
        {
            return Ok(UnauthenticatedSession());
        }

        // Include whether the authenticated user still needs to complete their profile.
        bool requiresProfileSetup = !await HasCompleteProfileAsync(
            userResult.UserId.Value,
            cancellationToken);
        return Ok(new { authenticated = true, userId = userResult.UserId, requiresProfileSetup });
    }

    /// <summary>
    /// Logs out the current authenticated user from Supabase and deletes the access token cookie.
    /// </summary>
    /// <returns>The result of the logout operation.</returns>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        string? accessToken = GetAccessToken();
        if (accessToken is not null)
        {
            await authClient.LogoutAsync(accessToken, cancellationToken);
        }

        Response.Cookies.Delete(AuthCookies.AccessToken, CookieOptions());
        Response.Cookies.Delete(AuthCookies.RefreshToken, CookieOptions());
        return NoContent();
    }

    /// <summary>
    /// Updates the authenticated user's password.
    /// </summary>
    /// <param name="request">The update password request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The authentication response.</returns>
    [HttpPatch("password")]
    public async Task<IActionResult> UpdatePassword(
        [FromBody] UpdatePasswordRequest request,
        CancellationToken cancellationToken)
    {
        string? accessToken = GetAccessToken();
        if (accessToken is null)
        {
            return Unauthorized(new { error = "Missing or invalid authorization token.", errorCode = "missing_token" });
        }

        AuthResult result = await authClient.UpdatePasswordAsync(accessToken, request, cancellationToken);

        if (!result.Succeeded)
        {
            return StatusCode(result.StatusCode, new
            {
                error = result.Error,
                errorCode = result.ErrorCode
            });
        }

        return StatusCode(result.StatusCode, ToClientResponse(result.Response!));
    }

    /// <summary>
    /// Triggers a password reset email if a user forgets their password.
    /// </summary>
    /// <param name="request">The forgot password request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A confirmation message.</returns>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        AuthResult result = await authClient.ForgotPasswordAsync(request, cancellationToken);

        if (!result.Succeeded)
        {
            logger.LogWarning(
                "Password recovery failed for {Email}: Status: {StatusCode}, Error: {Error}",
                request.Email,
                result.StatusCode,
                result.Error);
        }

        // Return a generic success message to prevent email enumeration attacks
        return Ok(new { message = "A password reset link has been sent to the provided email." });
    }

        /// <summary>
    /// Sets a new password using the recovery token from a password reset email.
    /// </summary>
    /// <param name="request">The reset password request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A confirmation message.</returns>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        AuthResult result = await authClient.ResetPasswordAsync(request, cancellationToken);

        if (!result.Succeeded)
        {
            if (result.StatusCode is StatusCodes.Status401Unauthorized
                or StatusCodes.Status403Forbidden)
            {
                return StatusCode(StatusCodes.Status401Unauthorized, new
                {
                    error = "This reset link is invalid or has expired. Please request a new one.",
                    errorCode = "invalid_reset_token"
                });
            }

            return StatusCode(result.StatusCode, new
            {
                error = result.Error,
                errorCode = result.ErrorCode
            });
        }

        return Ok(new { message = "Your password has been updated." });
    }

    /// <summary>
    /// Retrieves the access token from the request cookies.
    /// </summary>
    /// <returns>The access token, or null if it is not found.</returns>
    private string? GetAccessToken() =>
        Request.Cookies.TryGetValue(AuthCookies.AccessToken, out string? token) &&
        !string.IsNullOrWhiteSpace(token)
            ? token
            : null;

    /// <summary>
    /// Retrieves the refresh token from the request cookies.
    /// </summary>
    /// <returns>The refresh token, or null if it is not found.</returns>
    private string? GetRefreshToken() =>
        Request.Cookies.TryGetValue(AuthCookies.RefreshToken, out string? token) &&
        !string.IsNullOrWhiteSpace(token)
            ? token
            : null;

    /// <summary>
    /// Creates the response returned when no authenticated user is available.
    /// </summary>
    /// <returns>An unauthenticated session response.</returns>
    private static object UnauthenticatedSession() => new
    {
        authenticated = false,
        userId = (Guid?)null,
        requiresProfileSetup = false
    };

    /// <summary>
    /// Creates an error response for a refresh operation that did not succeed.
    /// </summary>
    /// <param name="result">The failed refresh result.</param>
    /// <returns>An unauthenticated response for invalid credentials or the service error response.</returns>
    private IActionResult RefreshFailureResponse(AuthResult result)
    {
        DeleteAuthCookiesIfRefreshTokenIsInvalid(result.StatusCode);
        return result.StatusCode is StatusCodes.Status400BadRequest
            or StatusCodes.Status401Unauthorized
            or StatusCodes.Status403Forbidden
            ? Ok(UnauthenticatedSession())
            : StatusCode(result.StatusCode, new
            {
                error = result.Error,
                errorCode = result.ErrorCode
            });
    }

    /// <summary>
    /// Creates the response returned when the authentication service returns incomplete data.
    /// </summary>
    /// <returns>A bad gateway response indicating an invalid authentication response.</returns>
    private ObjectResult IncompleteAuthResponse() => StatusCode(StatusCodes.Status502BadGateway, new
    {
        error = "Authentication service returned an incomplete response.",
        errorCode = "invalid_auth_response"
    });

    /// <summary>
    /// Creates the response returned when the authentication service is unavailable.
    /// </summary>
    /// <returns>An unavailable-service response.</returns>
    private ObjectResult AuthenticationServiceUnavailable() => StatusCode(
        StatusCodes.Status503ServiceUnavailable,
        new
        {
            error = "Authentication service is temporarily unavailable. Please try again.",
            errorCode = "authentication_service_unavailable"
        });

    /// <summary>
    /// Refreshes the current session and updates the authentication cookies when successful.
    /// </summary>
    /// <param name="refreshToken">The refresh token used to renew the session.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the session refresh.</returns>
    private async Task<AuthResult> RefreshSessionAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        AuthResult result = await authClient.RefreshAsync(refreshToken, cancellationToken);
        if (result.Succeeded && result.Response is not null)
        {
            SetAuthCookies(result.Response, refreshToken);
        }

        return result;
    }

    /// <summary>
    /// Deletes authentication cookies when the refresh token is rejected as invalid.
    /// </summary>
    /// <param name="statusCode">The status code returned by the authentication service.</param>
    private void DeleteAuthCookiesIfRefreshTokenIsInvalid(int statusCode)
    {
        if (statusCode is StatusCodes.Status400BadRequest
            or StatusCodes.Status401Unauthorized
            or StatusCodes.Status403Forbidden)
        {
            Response.Cookies.Delete(AuthCookies.AccessToken, CookieOptions());
            Response.Cookies.Delete(AuthCookies.RefreshToken, CookieOptions());
        }
    }

    /// <summary>
    /// Sets the access and refresh token cookies returned by the authentication service.
    /// </summary>
    /// <param name="response">The authentication response containing the access token.</param>
    /// <param name="fallbackRefreshToken">The existing refresh token to retain when token rotation is not used.</param>
    private void SetAuthCookies(AuthResponse response, string? fallbackRefreshToken = null)
    {
        if (!string.IsNullOrWhiteSpace(response.AccessToken))
        {
            Response.Cookies.Append(
                AuthCookies.AccessToken,
                response.AccessToken,
                CookieOptions(response.ExpiresIn));
        }

        string? refreshToken = response.RefreshToken ?? fallbackRefreshToken;
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            Response.Cookies.Append(
                AuthCookies.RefreshToken,
                refreshToken,
                CookieOptions());
        }
    }

    /// <summary>
    /// Creates cookie options for the access token cookie, including settings for security and expiration.
    /// </summary>
    /// <param name="expiresIn">The time (in seconds) until the cookie expires.</param>
    /// <returns>The cookie options.</returns>
    private static CookieOptions CookieOptions(int? expiresIn = null) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Lax,
        IsEssential = true,
        MaxAge = expiresIn is > 0 ? TimeSpan.FromSeconds(expiresIn.Value) : null,
        Path = "/"
    };

    /// <summary>
    /// Converts the authentication response to a client-friendly format, including the user ID and email confirmation requirement.
    /// </summary>
    /// <param name="response">The authentication response to convert.</param>
    /// <param name="requiresProfileSetup">Indicates if the user still needs to complete their profile.</param>
    /// <returns>The client-friendly response.</returns>
    private static object ToClientResponse(
        AuthResponse response,
        bool requiresProfileSetup = false) => new
        {
            userId = response.UserId,
            requiresEmailConfirmation = response.RequiresEmailConfirmation,
            requiresProfileSetup
        };

    /// <summary>
    /// Checks if the user has a complete profile by verifying that the first name, last name, and date of birth are not null.
    /// </summary>
    /// <param name="userId">The ID of the user to check.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the user has a complete profile, false otherwise.</returns>
    private async Task<bool> HasCompleteProfileAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        Profile? profile = await dbContext.Profiles
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);

        return profile?.FirstName is not null &&
            profile.LastName is not null &&
            profile.DateOfBirth is not null;
    }
}

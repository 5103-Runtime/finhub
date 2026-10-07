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

        SetAccessTokenCookie(result.Response);
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

        SetAccessTokenCookie(result.Response);
        bool requiresProfileSetup = !await HasCompleteProfileAsync(
            result.Response.UserId,
            cancellationToken);
        return StatusCode(
            result.StatusCode,
            ToClientResponse(result.Response, requiresProfileSetup));
    }

    /// <summary>
    /// Retrieves the current authenticated user's session information.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The current authenticated user's session information.</returns>
    [HttpGet("session")]
    public async Task<ActionResult> GetSession(CancellationToken cancellationToken)
    {
        string? accessToken = GetAccessToken();
        if (accessToken is null)
        {
            return Unauthorized();
        }

        Guid? userId = await authClient.GetUserIdAsync(accessToken, cancellationToken);
        if (userId is null)
        {
            return Unauthorized();
        }

        bool requiresProfileSetup = !await HasCompleteProfileAsync(
            userId.Value,
            cancellationToken);
        return Ok(new { userId, requiresProfileSetup });
    }

    /// <summary>
    /// Logs out the current authenticated user by deleting the access token cookie.
    /// </summary>
    /// <returns>The result of the logout operation.</returns>
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(AuthCookies.AccessToken, CookieOptions());
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
    /// Retrieves the access token from the request cookies.
    /// </summary>
    /// <returns>The access token, or null if it is not found.</returns>
    private string? GetAccessToken() =>
        Request.Cookies.TryGetValue(AuthCookies.AccessToken, out string? token) &&
        !string.IsNullOrWhiteSpace(token)
            ? token
            : null;

    /// <summary>
    /// Sets the access token cookie in the response if the access token is present in the authentication response.
    /// </summary>
    /// <param name="response">The authentication response containing the access token.</param>
    private void SetAccessTokenCookie(AuthResponse response)
    {
        if (string.IsNullOrWhiteSpace(response.AccessToken))
        {
            return;
        }

        Response.Cookies.Append(AuthCookies.AccessToken, response.AccessToken, CookieOptions(response.ExpiresIn));
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

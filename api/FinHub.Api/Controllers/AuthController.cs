using FinHub.Api.DTOs.Auth;
using FinHub.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FinHub.Api.Controllers;

/// <summary>
/// Controller responsible for handling authentication-related operations.
/// </summary>
/// <param name="authClient">The authentication client.</param>
[ApiController]
[Route("api/auth")]
public sealed class AuthController(SupabaseAuthClient authClient) : ControllerBase
{
    /// <summary>
    /// Registers a new user with the provided registration details.
    /// </summary>
    /// <param name="request">The registration request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The authentication response.</returns>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(
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

        return StatusCode(result.StatusCode, result.Response);
    }

    /// <summary>
    /// Logs in a user with the provided login credentials.
    /// </summary>
    /// <param name="request">The login request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The authentication response.</returns>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(
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

        return StatusCode(result.StatusCode, result.Response);
    }

    /// <summary>
    /// Updates the authenticated user's password.
    /// </summary>
    /// <param name="request">The update password request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The authentication response.</returns>
    [HttpPatch("password")]
    public async Task<ActionResult<AuthResponse>> UpdatePassword(
        [FromBody] UpdatePasswordRequest request,
        CancellationToken cancellationToken)
    {
        string? authHeader = Request.Headers.Authorization;
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return Unauthorized(new { error = "Missing or invalid authorization token.", errorCode = "missing_token" });
        }

        string accessToken = authHeader["Bearer ".Length..].Trim();
        AuthResult result = await authClient.UpdatePasswordAsync(accessToken, request, cancellationToken);

        if (!result.Succeeded)
        {
            return StatusCode(result.StatusCode, new 
            { 
                error = result.Error, 
                errorCode = result.ErrorCode 
            });
        }

        return StatusCode(result.StatusCode, result.Response);
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
            return StatusCode(result.StatusCode, new 
            { 
                error = result.Error, 
                errorCode = result.ErrorCode 
            });
        }

        // Return a generic success message to prevent email enumeration attacks
        return Ok(new { message = "A password reset link has been sent to the provided email." });
    }
}

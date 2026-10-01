using FinHub.Api.DTOs;
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
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        AuthResult result = await authClient.RegisterAsync(request, cancellationToken);

        if (!result.Succeeded)
        {
            return StatusCode(result.StatusCode, new { error = result.Error });
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
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        AuthResult result = await authClient.LoginAsync(request, cancellationToken);

        if (!result.Succeeded)
        {
            return StatusCode(result.StatusCode, new { error = result.Error });
        }

        return StatusCode(result.StatusCode, result.Response);
    }
}

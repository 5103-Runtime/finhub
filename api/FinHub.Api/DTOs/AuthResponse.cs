namespace FinHub.Api.DTOs;

/// <summary>
/// Represents the response returned after a successful user authentication.
/// </summary>
/// <param name="UserId">The ID of the authenticated user.</param>
/// <param name="AccessToken">The access token for the authenticated user, if applicable.</param>
/// <param name="RefreshToken">The refresh token for the authenticated user, if applicable.</param>
/// <param name="ExpiresIn">The number of seconds until the access token expires, if applicable.</param>
/// <param name="RequiresEmailConfirmation">Indicates if email confirmation is required.</param>
public sealed record AuthResponse(
    Guid UserId,
    string? AccessToken,
    string? RefreshToken,
    int? ExpiresIn,
    bool RequiresEmailConfirmation);
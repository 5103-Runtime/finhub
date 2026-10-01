namespace FinHub.Api.DTOs;

/// <summary>
/// Represents the response returned after a successful user registration.
/// </summary>
/// <param name="UserId">The ID of the registered user.</param>
/// <param name="RequiresEmailConfirmation">Indicates if email confirmation is required.</param>
public sealed record RegisterResponse(
    Guid UserId,
    bool RequiresEmailConfirmation);

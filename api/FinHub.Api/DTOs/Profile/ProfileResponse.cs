namespace FinHub.Api.DTOs.Profile;

/// <summary>
/// Represents the response body for a profile update request.
/// </summary>
/// <param name="Id">The ID of the user.</param>
/// <param name="FirstName">The first name of the user.</param>
/// <param name="LastName">The last name of the user.</param>
/// <param name="PreferredName">The preferred name of the user.</param>
/// <param name="DateOfBirth">The date of birth of the user.</param>
public sealed record ProfileResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string? PreferredName,
    DateOnly DateOfBirth);

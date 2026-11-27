namespace FinHub.Api.DTOs.Admin;

/// <summary>
/// Represents an application user returned to an administrator.
/// </summary>
/// <param name="Id">The user ID.</param>
/// <param name="FirstName">The user's first name.</param>
/// <param name="LastName">The user's last name.</param>
/// <param name="PreferredName">The user's preferred name.</param>
/// <param name="DateOfBirth">The user's date of birth.</param>
/// <param name="Role">The application role: user or administrator.</param>
/// <param name="CreatedAt">The date and time the user was created.</param>
/// <param name="UpdatedAt">The date and time the user was last updated.</param>
public sealed record UserManagementResponse(
    Guid Id,
    string? FirstName,
    string? LastName,
    string? PreferredName,
    DateOnly? DateOfBirth,
    string Role,
    DateTime CreatedAt,
    DateTime UpdatedAt);

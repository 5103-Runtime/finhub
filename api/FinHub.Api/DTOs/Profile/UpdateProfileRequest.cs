using System.ComponentModel.DataAnnotations;
using FinHub.Api.Validations;

namespace FinHub.Api.DTOs.Profile;

/// <summary>
/// Represents the request body for updating a user's profile.
/// </summary>
/// <param name="FirstName">The first name of the user.</param>
/// <param name="LastName">The last name of the user.</param>
/// <param name="PreferredName">The preferred name of the user.</param>
/// <param name="DateOfBirth">The date of birth of the user.</param>
public sealed record UpdateProfileRequest(
    [Required, StringLength(50)] string FirstName,
    [Required, StringLength(50)] string LastName,
    [StringLength(50)] string? PreferredName,
    [DateOfBirth]
    DateOnly DateOfBirth);

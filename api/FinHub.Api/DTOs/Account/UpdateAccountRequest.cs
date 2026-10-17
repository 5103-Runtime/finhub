using System.ComponentModel.DataAnnotations;

namespace FinHub.Api.DTOs.Account;

/// <summary>
/// Represents the request body for updating a financial account's descriptive fields.
/// </summary>
/// <param name="Name">The display name of the account.</param>
/// <param name="Institution">The institution that holds the account, if any.</param>
public sealed record UpdateAccountRequest(
    [Required, StringLength(100)] string Name,
    [StringLength(100)] string? Institution);

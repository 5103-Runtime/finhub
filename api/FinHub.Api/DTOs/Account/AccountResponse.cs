namespace FinHub.Api.DTOs.Account;

/// <summary>
/// Represents the response body for a financial account.
/// </summary>
/// <param name="Id">The ID of the account.</param>
/// <param name="Name">The display name of the account.</param>
/// <param name="Institution">The institution that holds the account, if any.</param>
/// <param name="AccountType">The account type: chequing, savings, or credit_card.</param>
/// <param name="Currency">The ISO 4217 currency code of the account.</param>
/// <param name="CreatedAt">The date and time when the account was created.</param>
/// <param name="UpdatedAt">The date and time when the account was last updated.</param>
public sealed record AccountResponse(
    Guid Id,
    string Name,
    string? Institution,
    string AccountType,
    string Currency,
    DateTime CreatedAt,
    DateTime UpdatedAt);

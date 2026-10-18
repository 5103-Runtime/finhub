using System.ComponentModel.DataAnnotations;

namespace FinHub.Api.DTOs.Account;

/// <summary>
/// Represents the request body for creating a financial account.
/// </summary>
/// <param name="Name">The display name of the account.</param>
/// <param name="Institution">The institution that holds the account, if any.</param>
/// <param name="AccountType">The account type: chequing, savings, or credit_card.</param>
/// <param name="Currency">The ISO 4217 currency code of the account.</param>
public sealed record CreateAccountRequest(
    [Required, StringLength(100)] string Name,
    [StringLength(100)] string? Institution,
    [Required, AllowedValues("chequing", "savings", "credit_card")] string AccountType,
    [AllowedValues("CAD", "USD")] string Currency = "CAD");

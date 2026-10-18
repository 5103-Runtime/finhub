using FinHub.Api.Data;
using FinHub.Api.DTOs.Account;
using FinHub.Api.Models;
using FinHub.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinHub.Api.Controllers;

/// <summary>
/// Controller responsible for managing the authenticated user's financial accounts.
/// </summary>
/// <param name="dbContext">The database context.</param>
/// <param name="authClient">The authentication client.</param>
[ApiController]
[Route("api/accounts")]
public sealed class AccountsController(
    FinHubDbContext dbContext,
    SupabaseAuthClient authClient) : AuthenticatedControllerBase(authClient)
{
    /// <summary>
    /// Lists the authenticated user's financial accounts.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The user's accounts, oldest first.</returns>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AccountResponse>>> List(
        CancellationToken cancellationToken)
    {
        (Guid? userId, ActionResult? failure) = await ResolveCurrentUserAsync(cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        List<AccountResponse> accounts = await dbContext.FinancialAccounts
            .AsNoTracking()
            .Where(account => account.UserId == userId!.Value)
            .OrderBy(account => account.CreatedAt)
            .Select(account => ToResponse(account))
            .ToListAsync(cancellationToken);

        return Ok(accounts);
    }

    /// <summary>
    /// Retrieves one of the authenticated user's financial accounts.
    /// </summary>
    /// <param name="id">The ID of the account.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The account.</returns>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AccountResponse>> Get(
        Guid id,
        CancellationToken cancellationToken)
    {
        (Guid? userId, ActionResult? failure) = await ResolveCurrentUserAsync(cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        FinancialAccount? account = await FindAccountAsync(id, userId!.Value, track: false, cancellationToken);

        return account is null ? AccountNotFound() : Ok(ToResponse(account));
    }

    /// <summary>
    /// Creates a financial account for the authenticated user.
    /// </summary>
    /// <param name="request">The account request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created account.</returns>
    [HttpPost]
    public async Task<ActionResult<AccountResponse>> Create(
        [FromBody] CreateAccountRequest request,
        CancellationToken cancellationToken)
    {
        (Guid? userId, ActionResult? failure) = await ResolveCurrentUserAsync(cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        FinancialAccount account = new()
        {
            Id = Guid.NewGuid(),
            UserId = userId!.Value,
            AccountType = request.AccountType,
            Currency = request.Currency
        };
        ApplyDetails(account, request.Name, request.Institution);

        dbContext.FinancialAccounts.Add(account);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = account.Id }, ToResponse(account));
    }

    /// <summary>
    /// Updates one of the authenticated user's financial accounts.
    /// </summary>
    /// <param name="id">The ID of the account.</param>
    /// <param name="request">The account request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated account.</returns>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AccountResponse>> Update(
        Guid id,
        [FromBody] UpdateAccountRequest request,
        CancellationToken cancellationToken)
    {
        (Guid? userId, ActionResult? failure) = await ResolveCurrentUserAsync(cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        FinancialAccount? account = await FindAccountAsync(id, userId!.Value, track: true, cancellationToken);
        if (account is null)
        {
            return AccountNotFound();
        }

        ApplyDetails(account, request.Name, request.Institution);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(account));
    }

    /// <summary>
    /// Deletes one of the authenticated user's financial accounts.
    /// </summary>
    /// <param name="id">The ID of the account.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>No content when the account was deleted.</returns>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        (Guid? userId, ActionResult? failure) = await ResolveCurrentUserAsync(cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        FinancialAccount? account = await FindAccountAsync(id, userId!.Value, track: true, cancellationToken);
        if (account is null)
        {
            return AccountNotFound();
        }

        dbContext.FinancialAccounts.Remove(account);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Sets the account's descriptive fields, trimming text and blanking an empty institution.
    /// </summary>
    /// <param name="account">The account to update.</param>
    /// <param name="name">The display name of the account.</param>
    /// <param name="institution">The institution that holds the account, if any.</param>
    private static void ApplyDetails(FinancialAccount account, string name, string? institution)
    {
        account.Name = name.Trim();
        account.Institution = string.IsNullOrWhiteSpace(institution)
            ? null
            : institution.Trim();
    }

    /// <summary>
    /// Converts an account to its response representation.
    /// </summary>
    /// <param name="account">The account.</param>
    /// <returns>The account response.</returns>
    private static AccountResponse ToResponse(FinancialAccount account) => new(
        account.Id,
        account.Name,
        account.Institution,
        account.AccountType,
        account.Currency,
        account.CreatedAt,
        account.UpdatedAt);

    /// <summary>
    /// Creates the response returned when no account matches the ID for the authenticated user.
    /// </summary>
    /// <returns>A not-found response.</returns>
    private NotFoundObjectResult AccountNotFound() => NotFound(new
    {
        error = "The account could not be found.",
        errorCode = "account_not_found"
    });

    /// <summary>
    /// Finds one of the user's accounts by ID.
    /// </summary>
    /// <param name="id">The ID of the account.</param>
    /// <param name="userId">The ID of the owning user.</param>
    /// <param name="track">Whether the entity should be tracked for updates.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The account, or null if it does not exist or belongs to another user.</returns>
    private Task<FinancialAccount?> FindAccountAsync(
        Guid id,
        Guid userId,
        bool track,
        CancellationToken cancellationToken)
    {
        IQueryable<FinancialAccount> accounts = track
            ? dbContext.FinancialAccounts
            : dbContext.FinancialAccounts.AsNoTracking();

        return accounts.SingleOrDefaultAsync(
            account => account.Id == id && account.UserId == userId,
            cancellationToken);
    }
}

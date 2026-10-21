using FinHub.Api.Data;
using FinHub.Api.DTOs.Transaction;
using FinHub.Api.Models;
using FinHub.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinHub.Api.Controllers;

/// <summary>
/// Controller responsible for managing the authenticated user's ledger transactions.
/// </summary>
/// <param name="dbContext">The database context.</param>
/// <param name="authClient">The authentication client.</param>
[ApiController]
[Route("api/transactions")]
public sealed class TransactionsController(
    FinHubDbContext dbContext,
    SupabaseAuthClient authClient) : AuthenticatedControllerBase(authClient)
{
    /// <summary>
    /// Lists the authenticated user's transactions, newest first.
    /// </summary>
    /// <param name="accountId"> Optional account ID to filter by. </param>
    /// <param name="cancellationToken"> The cancellation token. </param>
    /// <returns> The user's transactions. </returns>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TransactionResponse>>> List(
        [FromQuery] Guid? accountId, CancellationToken cancellationToken)
    {
        (Guid? userId, ActionResult? failure) = await ResolveCurrentUserAsync(cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        IQueryable<Transaction> query = dbContext.Transactions
            .AsNoTracking()
            .Where(transaction => OwnedAccountIds(userId!.Value).Contains(transaction.AccountId));

        if (accountId is not null)
        {
            query = query.Where(transaction => transaction.AccountId == accountId.Value);
        }

        List<TransactionResponse> transactions = await query
            .OrderByDescending(transaction => transaction.TransactionDate)
            .ThenByDescending(transaction => transaction.StatementLine)
            .ThenByDescending(transaction => transaction.CreatedAt)
            .Select(transaction => ToResponse(transaction))
            .ToListAsync(cancellationToken);

        return Ok(transactions);
    }

    /// <summary>
    /// Retrieves one of the authenticated user's transactions.
    /// </summary>
    /// <param name="id">The ID of the transaction.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The transaction.</returns>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TransactionResponse>> Get(
        Guid id,
        CancellationToken cancellationToken)
    {
        (Guid? userId, ActionResult? failure) = await ResolveCurrentUserAsync(cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        Transaction? transaction = await FindTransactionAsync(id, userId!.Value, track: false, cancellationToken);

        return transaction is null ? TransactionNotFound() : Ok(ToResponse(transaction));
    }

    /// <summary>
    /// Manually creates a transaction in one of the authenticated user's accounts.
    /// </summary>
    /// <param name="request">The transaction request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created transaction.</returns>
    [HttpPost]
    public async Task<ActionResult<TransactionResponse>> Create(
        [FromBody] CreateTransactionRequest request,
        CancellationToken cancellationToken)
    {
        (Guid? userId, ActionResult? failure) = await ResolveCurrentUserAsync(cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        bool ownsAccount = await OwnedAccountIds(userId!.Value)
            .AnyAsync(id => id == request.AccountId, cancellationToken);
        if (!ownsAccount)
        {
            return AccountNotFound();
        }

        Transaction transaction = new()
        {
            Id = Guid.NewGuid(),
            AccountId = request.AccountId
        };
        ApplyDetails(transaction, request.TransactionDate, request.Description, request.Amount);

        dbContext.Transactions.Add(transaction);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = transaction.Id }, ToResponse(transaction));
    }

    /// <summary>
    /// Updates one of the authenticated user's transactions.
    /// </summary>
    /// <param name="id">The ID of the transaction.</param>
    /// <param name="request">The transaction request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated transaction.</returns>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TransactionResponse>> Update(
        Guid id,
        [FromBody] UpdateTransactionRequest request,
        CancellationToken cancellationToken)
    {
        (Guid? userId, ActionResult? failure) = await ResolveCurrentUserAsync(cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        Transaction? transaction = await FindTransactionAsync(id, userId!.Value, track: true, cancellationToken);
        if (transaction is null)
        {
            return TransactionNotFound();
        }

        ApplyDetails(transaction, request.TransactionDate, request.Description, request.Amount);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(transaction));
    }

    /// <summary>
    /// Deletes one of the authenticated user's transactions.
    /// </summary>
    /// <param name="id">The ID of the transaction.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>No content when the transaction was deleted.</returns>
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

        Transaction? transaction = await FindTransactionAsync(id, userId!.Value, track: true, cancellationToken);
        if (transaction is null)
        {
            return TransactionNotFound();
        }

        dbContext.Transactions.Remove(transaction);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Sets the transaction's editable fields, trimming the description.
    /// </summary>
    /// <param name="transaction">The transaction to update.</param>
    /// <param name="transactionDate">The date the transaction occurred.</param>
    /// <param name="description">The description of the transaction.</param>
    /// <param name="amount">The signed amount.</param>
    private static void ApplyDetails(
        Transaction transaction,
        DateOnly transactionDate,
        string description,
        decimal amount)
    {
        transaction.TransactionDate = transactionDate;
        transaction.Description = description.Trim();
        transaction.Amount = amount;
    }

    /// <summary>
    /// Converts a transaction to its response representation.
    /// </summary>
    /// <param name="transaction">The transaction.</param>
    /// <returns>The transaction response.</returns>
    private static TransactionResponse ToResponse(Transaction transaction) => new(
        transaction.Id,
        transaction.AccountId,
        transaction.TransactionDate,
        transaction.Description,
        transaction.Amount,
        transaction.BalanceAfter,
        transaction.StatementLine,
        transaction.CreatedAt,
        transaction.UpdatedAt);

    /// <summary>
    /// Creates the response returned when no transaction matches the ID for the authenticated user.
    /// </summary>
    /// <returns>A not-found response.</returns>
    private NotFoundObjectResult TransactionNotFound() => NotFound(new
    {
        error = "The transaction could not be found.",
        errorCode = "transaction_not_found"
    });

    /// <summary>
    /// Creates the response returned when the account does not exist for the authenticated user.
    /// </summary>
    /// <returns>A not-found response.</returns>
    private NotFoundObjectResult AccountNotFound() => NotFound(new
    {
        error = "The account could not be found.",
        errorCode = "account_not_found"
    });

    /// <summary>
    /// Gets the IDs of the accounts owned by the user.
    /// </summary>
    /// <param name="userId">The ID of the owning user.</param>
    /// <returns>A query of the user's account IDs.</returns>
    private IQueryable<Guid> OwnedAccountIds(Guid userId) => dbContext.FinancialAccounts
        .AsNoTracking()
        .Where(account => account.UserId == userId)
        .Select(account => account.Id);

    /// <summary>
    /// Finds one of the user's transactions by ID.
    /// </summary>
    /// <param name="id">The ID of the transaction.</param>
    /// <param name="userId">The ID of the owning user.</param>
    /// <param name="track">Whether the entity should be tracked for updates.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The transaction, or null if it does not exist or belongs to another user.</returns>
    private Task<Transaction?> FindTransactionAsync(
        Guid id,
        Guid userId,
        bool track,
        CancellationToken cancellationToken)
    {
        IQueryable<Transaction> transactions = track
            ? dbContext.Transactions
            : dbContext.Transactions.AsNoTracking();

        return transactions.SingleOrDefaultAsync(
            transaction => transaction.Id == id
                && OwnedAccountIds(userId).Contains(transaction.AccountId),
            cancellationToken);
    }
}
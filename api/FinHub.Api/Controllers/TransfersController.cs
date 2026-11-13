using FinHub.Api.Data;
using FinHub.Api.DTOs.Transfer;
using FinHub.Api.Models;
using FinHub.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinHub.Api.Controllers;

/// <summary>
/// Creates internal transfers between accounts owned by the authenticated user.
/// </summary>
/// <param name="dbContext">The database context.</param>
/// <param name="authClient">The authentication client.</param>
[ApiController]
[Route("api/transfers")]
public sealed class TransfersController(
    FinHubDbContext dbContext,
    SupabaseAuthClient authClient) : AuthenticatedControllerBase(authClient)
{
    /// <summary>
    /// Gets one transfer owned by the authenticated user.
    /// </summary>
    /// <param name="id">The transfer identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The transfer and its two ledger lines.</returns>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TransferResponse>> Get(
        Guid id,
        CancellationToken cancellationToken)
    {
        (Guid? userId, ActionResult? failure) = await ResolveCurrentUserAsync(cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        Transfer? transfer = await FindTransferAsync(id, userId!.Value, cancellationToken);
        if (transfer is null)
        {
            return TransferNotFound();
        }

        return await CreateResponseAsync(transfer, cancellationToken);
    }
    /// <summary>
    /// Creates an atomic transfer represented by one outgoing and one incoming ledger line.
    /// Existing unlinked ledger lines may be reused when they match the requested transfer.
    /// </summary>
    /// <param name="request">The transfer request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The transfer.</returns>
    [HttpPost]
    public async Task<ActionResult<TransferResponse>> Create(
        [FromBody] CreateTransferRequest request,
        CancellationToken cancellationToken)
    {
        (Guid? userId, ActionResult? failure) = await ResolveCurrentUserAsync(cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        Guid fromAccountId = request.FromAccountId!.Value;
        Guid toAccountId = request.ToAccountId!.Value;
        if (fromAccountId == toAccountId)
        {
            return BadRequest(new
            {
                error = "The source and destination accounts must be different.",
                errorCode = "same_transfer_account"
            });
        }

        Guid currentUserId = userId!.Value;
        List<FinancialAccount> accounts = await dbContext.FinancialAccounts
            .Where(account => account.UserId == currentUserId
                && (account.Id == fromAccountId || account.Id == toAccountId))
            .ToListAsync(cancellationToken);
        FinancialAccount? fromAccount = accounts.SingleOrDefault(account => account.Id == fromAccountId);
        FinancialAccount? toAccount = accounts.SingleOrDefault(account => account.Id == toAccountId);
        if (fromAccount is null || toAccount is null)
        {
            return NotFound(new
            {
                error = "Both transfer accounts must belong to the authenticated user.",
                errorCode = "transfer_account_not_found"
            });
        }
        if (!string.Equals(fromAccount.Currency, toAccount.Currency, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                error = "Transfers currently require accounts with the same currency.",
                errorCode = "different_currency_transfer"
            });
        }

        Guid? categoryId = request.CategoryId;
        if (categoryId is not null
            && !await dbContext.Categories.AnyAsync(
                category => category.Id == categoryId
                    && (category.IsSystem || category.UserId == currentUserId),
                cancellationToken))
        {
            return NotFound(new
            {
                error = "The selected category could not be found.",
                errorCode = "category_not_found"
            });
        }

        decimal amount = Math.Round(request.Amount!.Value, 2, MidpointRounding.AwayFromZero);
        if (amount <= 0m)
        {
            return BadRequest(new
            {
                error = "The transfer amount must be greater than zero.",
                errorCode = "invalid_transfer_amount"
            });
        }

        DateOnly transferDate = request.TransferDate!.Value;
        string description = request.Description.Trim();
        Guid transferId = Guid.NewGuid();
        Guid fromTransactionId = Guid.NewGuid();
        Guid toTransactionId = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;

        if (request.FromTransactionId == request.ToTransactionId
            && request.FromTransactionId is not null)
        {
            return BadRequest(new
            {
                error = "The source and destination transactions must be different.",
                errorCode = "same_transfer_transaction"
            });
        }

        List<Transaction> existingTransactions = request.FromTransactionId is null
            && request.ToTransactionId is null
            ? []
            : await dbContext.Transactions
                .Where(transaction =>
                    (transaction.Id == request.FromTransactionId
                        || transaction.Id == request.ToTransactionId)
                    && (transaction.AccountId == fromAccountId
                        || transaction.AccountId == toAccountId))
                .ToListAsync(cancellationToken);

        Transaction? existingOutgoing = existingTransactions.SingleOrDefault(
            transaction => transaction.Id == request.FromTransactionId);
        Transaction? existingIncoming = existingTransactions.SingleOrDefault(
            transaction => transaction.Id == request.ToTransactionId);

        if ((request.FromTransactionId is not null && existingOutgoing is null)
            || (request.ToTransactionId is not null && existingIncoming is null))
        {
            return NotFound(new
            {
                error = "The existing transfer transaction could not be found.",
                errorCode = "transfer_transaction_not_found"
            });
        }

        BadRequestObjectResult? existingTransactionError = ValidateExistingTransaction(
            existingOutgoing,
            fromAccountId,
            transferDate,
            description,
            -amount,
            "source");
        if (existingTransactionError is not null)
        {
            return existingTransactionError;
        }

        existingTransactionError = ValidateExistingTransaction(
            existingIncoming,
            toAccountId,
            transferDate,
            description,
            amount,
            "destination");
        if (existingTransactionError is not null)
        {
            return existingTransactionError;
        }

        Transfer transfer = new()
        {
            Id = transferId,
            UserId = currentUserId,
            FromAccountId = fromAccountId,
            ToAccountId = toAccountId,
            Amount = amount,
            TransferDate = transferDate,
            Description = description,
            CreatedAt = now,
            UpdatedAt = now
        };
        Transaction outgoing = existingOutgoing ?? CreateTransaction(
            fromTransactionId,
            fromAccountId,
            transferId,
            transferDate,
            description,
            -amount,
            categoryId,
            now);
        Transaction incoming = existingIncoming ?? CreateTransaction(
            toTransactionId,
            toAccountId,
            transferId,
            transferDate,
            description,
            amount,
            categoryId,
            now);

        outgoing.TransferId = transferId;
        incoming.TransferId = transferId;

        dbContext.Transfers.Add(transfer);
        if (existingOutgoing is null)
        {
            dbContext.Transactions.Add(outgoing);
        }
        if (existingIncoming is null)
        {
            dbContext.Transactions.Add(incoming);
        }
        await dbContext.SaveChangesAsync(cancellationToken);

        TransferResponse response = new(
            transfer.Id,
            transfer.FromAccountId,
            transfer.ToAccountId,
            transfer.Amount,
            transfer.TransferDate,
            transfer.Description,
            outgoing.Id,
            incoming.Id);

        return CreatedAtAction(
            nameof(Get),
            new { id = transfer.Id },
            response);
    }

    /// <summary>
    /// Deletes a transfer and both linked ledger lines.
    /// </summary>
    /// <param name="id">The transfer identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>No content when the transfer was deleted.</returns>
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

        Transfer? transfer = await FindTransferAsync(id, userId!.Value, cancellationToken);
        if (transfer is null)
        {
            return TransferNotFound();
        }

        dbContext.Transfers.Remove(transfer);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Creates a transfer response after verifying both ledger lines exist.
    /// </summary>
    /// <param name="transfer">The transfer to represent.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The transfer response or a ledger validation error.</returns>
    private async Task<ActionResult<TransferResponse>> CreateResponseAsync(
        Transfer transfer,
        CancellationToken cancellationToken)
    {
        List<Transaction> transactions = await dbContext.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.TransferId == transfer.Id)
            .ToListAsync(cancellationToken);
        Transaction? outgoing = transactions.SingleOrDefault(
            transaction => transaction.AccountId == transfer.FromAccountId);
        Transaction? incoming = transactions.SingleOrDefault(
            transaction => transaction.AccountId == transfer.ToAccountId);
        if (transactions.Count != 2 || outgoing is null || incoming is null)
        {
            return Conflict(new
            {
                error = "The transfer does not have exactly one ledger line in each account.",
                errorCode = "invalid_transfer_ledger"
            });
        }

        return new TransferResponse(
            transfer.Id,
            transfer.FromAccountId,
            transfer.ToAccountId,
            transfer.Amount,
            transfer.TransferDate,
            transfer.Description,
            outgoing.Id,
            incoming.Id);
    }

    /// <summary>
    /// Finds a transfer owned by the user and whose accounts are also user-owned.
    /// </summary>
    /// <param name="id">The transfer identifier.</param>
    /// <param name="userId">The authenticated user identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching transfer, or null when it is not visible to the user.</returns>
    private Task<Transfer?> FindTransferAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken) =>
        dbContext.Transfers.SingleOrDefaultAsync(
            transfer => transfer.Id == id
                && transfer.UserId == userId
                && dbContext.FinancialAccounts.Any(account =>
                    account.Id == transfer.FromAccountId
                    && account.UserId == userId)
                && dbContext.FinancialAccounts.Any(account =>
                    account.Id == transfer.ToAccountId
                    && account.UserId == userId),
            cancellationToken);

    /// <summary>
    /// Creates the response returned when a transfer cannot be found.
    /// </summary>
    /// <returns>A not-found response.</returns>
    private NotFoundObjectResult TransferNotFound() => NotFound(new
    {
        error = "The transfer could not be found.",
        errorCode = "transfer_not_found"
    });

    /// <summary>
    /// Creates a ledger line for a new transfer.
    /// </summary>
    /// <param name="id">The transaction identifier.</param>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="transferId">The transfer identifier.</param>
    /// <param name="transferDate">The transfer date.</param>
    /// <param name="description">The transfer description.</param>
    /// <param name="amount">The signed ledger amount.</param>
    /// <param name="categoryId">The optional category identifier.</param>
    /// <param name="now">The creation and update timestamp.</param>
    /// <returns>The new transfer ledger line.</returns>
    private static Transaction CreateTransaction(
        Guid id,
        Guid accountId,
        Guid transferId,
        DateOnly transferDate,
        string description,
        decimal amount,
        Guid? categoryId,
        DateTime now) => new()
        {
            Id = id,
            AccountId = accountId,
            TransferId = transferId,
            CategoryId = categoryId,
            TransactionDate = transferDate,
            Description = description,
            Amount = amount,
            CreatedAt = now,
            UpdatedAt = now
        };

    /// <summary>
    /// Validates an existing unlinked transaction before reusing it for a transfer.
    /// </summary>
    /// <param name="transaction">The existing transaction, if supplied.</param>
    /// <param name="expectedAccountId">The account the transaction must belong to.</param>
    /// <param name="expectedDate">The expected transaction date.</param>
    /// <param name="expectedDescription">The expected description.</param>
    /// <param name="expectedAmount">The expected signed amount.</param>
    /// <param name="leg">The transfer leg name used in error messages.</param>
    /// <returns>A validation error, or null when the transaction is valid.</returns>
    private static BadRequestObjectResult? ValidateExistingTransaction(
        Transaction? transaction,
        Guid expectedAccountId,
        DateOnly expectedDate,
        string expectedDescription,
        decimal expectedAmount,
        string leg)
    {
        if (transaction is null)
        {
            return null;
        }

        if (transaction.TransferId is not null)
        {
            return new BadRequestObjectResult(new
            {
                error = $"The existing {leg} transaction is already linked to a transfer.",
                errorCode = "transfer_transaction_already_linked"
            });
        }

        if (transaction.AccountId != expectedAccountId)
        {
            return new BadRequestObjectResult(new
            {
                error = $"The existing {leg} transaction belongs to a different account.",
                errorCode = "transfer_transaction_account_mismatch"
            });
        }

        if (transaction.TransactionDate != expectedDate
            || transaction.Description != expectedDescription
            || transaction.Amount != expectedAmount)
        {
            return new BadRequestObjectResult(new
            {
                error = $"The existing {leg} transaction does not match the transfer date, description, or amount.",
                errorCode = "transfer_transaction_details_mismatch"
            });
        }

        return null;
    }
}

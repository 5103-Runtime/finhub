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
    /// Supports accountId, categoryId, uncategorized, fromDate, toDate,
    /// minAmount, maxAmount, and search query parameters.
    /// </summary>
    /// <param name="filters">Optional filters to apply to the transaction list.</param>
    /// <param name="sortBy">The field to sort by - amount, date, or description.</param>
    /// <param name="sortOrder">The sort direction - asc or desc.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The user's transactions.</returns>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TransactionResponse>>> List(
        [FromQuery] TransactionFilters filters,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortOrder,
        CancellationToken cancellationToken)
    {
        (Guid? userId, ActionResult? failure) = await ResolveCurrentUserAsync(cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        Guid currentUserId = userId!.Value;
        BadRequestObjectResult? filterError = ValidateFilters(filters);
        if (filterError is not null)
        {
            return filterError;
        }

        BadRequestObjectResult? sortingError = ValidateSorting(sortBy, sortOrder);
        if (sortingError is not null)
        {
            return sortingError;
        }

        IQueryable<Transaction> query = dbContext.Transactions
            .AsNoTracking()
            .Where(transaction => OwnedAccountIds(currentUserId).Contains(transaction.AccountId));

        query = ApplyFilters(query, filters);
        query = ApplySorting(query, sortBy, sortOrder);

        List<TransactionResponse> transactions = await query
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

        Guid accountId = request.AccountId!.Value;
        bool ownsAccount = await OwnedAccountIds(userId!.Value)
            .AnyAsync(id => id == accountId, cancellationToken);
        if (!ownsAccount)
        {
            return AccountNotFound();
        }

        Guid? categoryId = request.CategoryId;
        if (categoryId is not null
            && !await IsCategoryAvailableAsync(categoryId.Value, userId.Value, cancellationToken))
        {
            return CategoryNotFound();
        }

        DateOnly transactionDate = request.TransactionDate!.Value;
        string description = request.Description.Trim();
        decimal amount = Math.Round(
            request.Amount!.Value, 2, MidpointRounding.AwayFromZero
        );

        Transaction? duplicate = await FindDuplicateTransactionAsync(
            accountId,
            transactionDate,
            description,
            amount,
            excludeTransactionId: null,
            cancellationToken
        );
        if (duplicate is not null && !request.ConfirmPotentialDuplicate)
        {
            return PotentialDuplicateConflict(duplicate);
        }

        Transaction transaction = new()
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            CategoryId = categoryId
        };
        ApplyDetails(
            transaction,
            transactionDate,
            description,
            amount);

        dbContext.Transactions.Add(transaction);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(Get),
            new { id = transaction.Id },
            ToResponse(transaction, duplicate is not null));
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

        DateOnly transactionDate = request.TransactionDate!.Value;
        string description = request.Description.Trim();
        decimal amount = Math.Round(
            request.Amount!.Value, 2, MidpointRounding.AwayFromZero
        );
        bool transactionDetailsChanged = HasTransactionDetailsChanged(
            transaction,
            transactionDate,
            description,
            amount);
        Guid? categoryId = request.CategoryId;
        if (categoryId is not null
            && !await IsCategoryAvailableAsync(categoryId.Value, userId.Value, cancellationToken))
        {
            return CategoryNotFound();
        }

        if (transaction.TransferId is not null && transactionDetailsChanged)
        {
            return await UpdateTransferFromTransactionAsync(
                transaction,
                transactionDate,
                description,
                amount,
                categoryId,
                request.ConfirmPotentialDuplicate,
                cancellationToken);
        }

        Transaction? duplicate = transactionDetailsChanged
            ? await FindDuplicateTransactionAsync(
                transaction.AccountId,
                transactionDate,
                description,
                amount,
                excludeTransactionId: transaction.Id,
                cancellationToken)
            : null;
        if (duplicate is not null && !request.ConfirmPotentialDuplicate)
        {
            return PotentialDuplicateConflict(duplicate);
        }

        ApplyDetails(
            transaction,
            transactionDate,
            description,
            amount);
        transaction.CategoryId = categoryId;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(transaction, duplicate is not null));
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
        if (transaction.TransferId is not null)
        {
            return Conflict(new
            {
                error = "This transaction belongs to a transfer. Delete the complete transfer instead.",
                errorCode = "transfer_delete_requires_confirmation",
                transferId = transaction.TransferId,
                requiresTransferDeletion = true
            });
        }

        dbContext.Transactions.Remove(transaction);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Validates combinations and limits for transaction list filters.
    /// </summary>
    private static BadRequestObjectResult? ValidateFilters(TransactionFilters filters)
    {
        if (filters.FromDate is not null
            && filters.ToDate is not null
            && filters.FromDate > filters.ToDate)
        {
            return new BadRequestObjectResult(new
            {
                error = "The fromDate must be on or before the toDate.",
                errorCode = "invalid_date_range"
            });
        }

        if (filters.MinAmount is not null
            && filters.MaxAmount is not null
            && filters.MinAmount > filters.MaxAmount)
        {
            return new BadRequestObjectResult(new
            {
                error = "The minAmount must be less than or equal to the maxAmount.",
                errorCode = "invalid_amount_range"
            });
        }

        if (filters.Uncategorized && filters.CategoryId is not null)
        {
            return new BadRequestObjectResult(new
            {
                error = "Category and uncategorized filters cannot be used together.",
                errorCode = "conflicting_category_filters"
            });
        }

        if (filters.Search?.Trim().Length > 100)
        {
            return new BadRequestObjectResult(new
            {
                error = "The search filter must be 100 characters or less.",
                errorCode = "search_too_long"
            });
        }

        return null;
    }

    /// <summary>
    /// Validates the requested sorting field and direction.
    /// </summary>
    private static BadRequestObjectResult? ValidateSorting(
        string? sortBy,
        string? sortOrder)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
        {
            if (!string.IsNullOrWhiteSpace(sortOrder))
            {
                return new BadRequestObjectResult(new
                {
                    error = "A sortBy field is required when sortOrder is provided.",
                    errorCode = "missing_sort_field"
                });
            }

            return null;
        }

        string field = sortBy.Trim().ToLowerInvariant();

        if (field != "amount" && field != "date" && field != "description")
        {
            return new BadRequestObjectResult(new
            {
                error = "Sort by amount, date, or description.",
                errorCode = "invalid_sort_field"
            });
        }

        if (!string.IsNullOrWhiteSpace(sortOrder))
        {
            string direction = sortOrder.Trim().ToLowerInvariant();

            if (direction != "asc" && direction != "desc")
            {
                return new BadRequestObjectResult(new
                {
                    error = "Sort order must be asc or desc.",
                    errorCode = "invalid_sort_order"
                });
            }
        }

        return null;
    }

    /// <summary>
    /// Applies user-supplied transaction filters to an ownership-scoped query.
    /// </summary>
    private static IQueryable<Transaction> ApplyFilters(
        IQueryable<Transaction> query,
        TransactionFilters filters)
    {
        if (filters.AccountId is not null)
        {
            query = query.Where(transaction => transaction.AccountId == filters.AccountId.Value);
        }

        if (filters.CategoryId is not null)
        {
            query = query.Where(transaction => transaction.CategoryId == filters.CategoryId.Value);
        }
        else if (filters.Uncategorized)
        {
            query = query.Where(transaction => transaction.CategoryId == null);
        }

        if (filters.FromDate is not null)
        {
            query = query.Where(transaction => transaction.TransactionDate >= filters.FromDate.Value);
        }

        if (filters.ToDate is not null)
        {
            query = query.Where(transaction => transaction.TransactionDate <= filters.ToDate.Value);
        }

        if (filters.MinAmount is not null)
        {
            query = query.Where(transaction => transaction.Amount >= filters.MinAmount.Value);
        }

        if (filters.MaxAmount is not null)
        {
            query = query.Where(transaction => transaction.Amount <= filters.MaxAmount.Value);
        }

        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            string pattern = $"%{EscapeLikePattern(filters.Search.Trim())}%";
            query = query.Where(transaction =>
                EF.Functions.ILike(transaction.Description, pattern, @"\"));
        }

        return query;
    }

    /// <summary>
    /// Applies the requested sorting to the transaction query.
    /// Defaults to newest transactions first.
    /// </summary>
    private static IQueryable<Transaction> ApplySorting(
        IQueryable<Transaction> query,
        string? sortBy,
        string? sortOrder)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
        {
            return query
                .OrderByDescending(transaction => transaction.TransactionDate)
                .ThenByDescending(transaction => transaction.StatementLine)
                .ThenByDescending(transaction => transaction.CreatedAt);
        }

        bool descending = string.IsNullOrWhiteSpace(sortOrder)
            || sortOrder.Trim().Equals("desc", StringComparison.OrdinalIgnoreCase);

        string field = sortBy.Trim().ToLowerInvariant();

        return field switch
        {
            "amount" => descending
                ? query.OrderByDescending(transaction => transaction.Amount)
                    .ThenByDescending(transaction => transaction.TransactionDate)
                    .ThenByDescending(transaction => transaction.StatementLine)
                    .ThenByDescending(transaction => transaction.CreatedAt)
                : query.OrderBy(transaction => transaction.Amount)
                    .ThenByDescending(transaction => transaction.TransactionDate)
                    .ThenByDescending(transaction => transaction.StatementLine)
                    .ThenByDescending(transaction => transaction.CreatedAt),

            "date" => descending
                ? query.OrderByDescending(transaction => transaction.TransactionDate)
                    .ThenByDescending(transaction => transaction.StatementLine)
                    .ThenByDescending(transaction => transaction.CreatedAt)
                : query.OrderBy(transaction => transaction.TransactionDate)
                    .ThenByDescending(transaction => transaction.StatementLine)
                    .ThenByDescending(transaction => transaction.CreatedAt),

            "description" => descending
                ? query.OrderByDescending(transaction => transaction.Description)
                    .ThenByDescending(transaction => transaction.TransactionDate)
                    .ThenByDescending(transaction => transaction.StatementLine)
                    .ThenByDescending(transaction => transaction.CreatedAt)
                : query.OrderBy(transaction => transaction.Description)
                    .ThenByDescending(transaction => transaction.TransactionDate)
                    .ThenByDescending(transaction => transaction.StatementLine)
                    .ThenByDescending(transaction => transaction.CreatedAt),
            _ => query
        };
    }

    /// <summary>
    /// Escapes PostgreSQL LIKE wildcards so search input is treated as literal text.
    /// </summary>
    private static string EscapeLikePattern(string value) =>
        value.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");

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
        transaction.Amount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Updates the parent transfer and both linked ledger lines atomically.
    /// </summary>
    /// <param name="editedTransaction">The transfer ledger line being edited.</param>
    /// <param name="transactionDate">The new date for the transfer.</param>
    /// <param name="description">The new description for the transfer.</param>
    /// <param name="amount">The signed amount from the edited ledger line.</param>
    /// <param name="categoryId">The category for the edited ledger line.</param>
    /// <param name="confirmPotentialDuplicate">Whether to save after a potential duplicate warning.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated ledger line response or a validation error.</returns>
    private async Task<ActionResult<TransactionResponse>> UpdateTransferFromTransactionAsync(
        Transaction editedTransaction,
        DateOnly transactionDate,
        string description,
        decimal amount,
        Guid? categoryId,
        bool confirmPotentialDuplicate,
        CancellationToken cancellationToken)
    {
        Transfer? transfer = await dbContext.Transfers
            .SingleOrDefaultAsync(
                candidate => candidate.Id == editedTransaction.TransferId,
                cancellationToken);
        if (transfer is null)
        {
            return Conflict(new
            {
                error = "The transaction references a transfer that could not be found.",
                errorCode = "invalid_transfer_reference"
            });
        }

        List<Transaction> legs = await dbContext.Transactions
            .Where(candidate => candidate.TransferId == transfer.Id)
            .ToListAsync(cancellationToken);
        Transaction? outgoing = legs.SingleOrDefault(
            candidate => candidate.AccountId == transfer.FromAccountId);
        Transaction? incoming = legs.SingleOrDefault(
            candidate => candidate.AccountId == transfer.ToAccountId);
        if (outgoing is null || incoming is null || legs.Count != 2)
        {
            return Conflict(new
            {
                error = "The transfer does not have exactly one ledger line in each account.",
                errorCode = "invalid_transfer_ledger"
            });
        }

        bool editingOutgoing = editedTransaction.Id == outgoing.Id;
        if (!editingOutgoing && editedTransaction.Id != incoming.Id)
        {
            return Conflict(new
            {
                error = "The transaction is not one of the transfer ledger lines.",
                errorCode = "invalid_transfer_ledger"
            });
        }

        if (editingOutgoing ? amount >= 0m : amount <= 0m)
        {
            return BadRequest(new
            {
                error = "Transfer transaction amounts must keep their outgoing or incoming sign.",
                errorCode = "invalid_transfer_transaction_amount"
            });
        }

        decimal transferAmount = Math.Round(
            Math.Abs(amount),
            2,
            MidpointRounding.AwayFromZero);
        if (transferAmount <= 0m)
        {
            return BadRequest(new
            {
                error = "The transfer amount must be greater than zero after rounding to two decimal places.",
                errorCode = "invalid_transfer_amount"
            });
        }

        string normalizedDescription = description.Trim();
        Transfer? duplicate = await dbContext.Transfers
            .SingleOrDefaultAsync(
                candidate => candidate.Id != transfer.Id
                    && candidate.UserId == transfer.UserId
                    && candidate.FromAccountId == transfer.FromAccountId
                    && candidate.ToAccountId == transfer.ToAccountId
                    && candidate.TransferDate == transactionDate
                    && candidate.Description == normalizedDescription
                    && candidate.Amount == transferAmount,
                cancellationToken);
        if (duplicate is not null && !confirmPotentialDuplicate)
        {
            return Conflict(new
            {
                error = "This transfer may be a duplicate. Set confirmPotentialDuplicate to true to save it.",
                errorCode = "potential_duplicate",
                existingTransferId = duplicate.Id
            });
        }

        DateTime now = DateTime.UtcNow;
        await using var databaseTransaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);

        transfer.Amount = transferAmount;
        transfer.TransferDate = transactionDate;
        transfer.Description = normalizedDescription;
        transfer.UpdatedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);

        ApplyDetails(outgoing, transactionDate, normalizedDescription, -transferAmount);
        ApplyDetails(incoming, transactionDate, normalizedDescription, transferAmount);
        outgoing.CategoryId = editingOutgoing ? categoryId : outgoing.CategoryId;
        incoming.CategoryId = editingOutgoing ? incoming.CategoryId : categoryId;
        outgoing.UpdatedAt = now;
        incoming.UpdatedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);
        await databaseTransaction.CommitAsync(cancellationToken);

        Transaction updatedTransaction = editingOutgoing
            ? outgoing
            : incoming;
        return Ok(ToResponse(updatedTransaction));
    }

    /// <summary>
    /// Checks whether the duplicate-defining transaction details changed.
    /// Category is intentionally excluded because it is mutable metadata.
    /// </summary>
    /// <param name="transaction">The existing transaction.</param>
    /// <param name="transactionDate">The normalized transaction date.</param>
    /// <param name="description">The normalized description.</param>
    /// <param name="amount">The normalized amount.</param>
    /// <returns>True when the date, description, or amount changed.</returns>
    private static bool HasTransactionDetailsChanged(
        Transaction transaction,
        DateOnly transactionDate,
        string description,
        decimal amount) =>
        transaction.TransactionDate != transactionDate
        || transaction.Description != description
        || transaction.Amount != amount;

    /// <summary>
    /// Checks whether another transaction has the same date, description,
    /// and amount on the same account.
    /// </summary>
    private async Task<Transaction?> FindDuplicateTransactionAsync(
        Guid accountId,
        DateOnly transactionDate,
        string description,
        decimal amount,
        Guid? excludeTransactionId,
        CancellationToken cancellationToken)
    {
        IQueryable<Transaction> query = dbContext.Transactions
            .AsNoTracking()
            .Where(transaction =>
                transaction.AccountId == accountId &&
                transaction.TransactionDate == transactionDate &&
                transaction.Description == description &&
                transaction.Amount == amount);

        // Exclude the current transaction when checking an update.
        if (excludeTransactionId.HasValue)
        {
            query = query.Where(
                transaction => transaction.Id != excludeTransactionId.Value);
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Creates the response returned when confirmation is required before saving a potential duplicate.
    /// </summary>
    /// <param name="duplicate">The existing matching transaction.</param>
    /// <returns>A conflict response describing the potential duplicate.</returns>
    private ConflictObjectResult PotentialDuplicateConflict(Transaction duplicate) => Conflict(new
    {
        error = "This transaction may be a duplicate. Set confirmPotentialDuplicate to true to save it.",
        errorCode = "potential_duplicate",
        potentialDuplicate = true,
        existingTransaction = ToResponse(duplicate)
    });

    /// <summary>
    /// Converts a transaction to its response representation.
    /// </summary>
    /// <param name="transaction">The transaction.</param>
    /// <param name="potentialDuplicate">Whether the transaction may duplicate an existing transaction.</param>
    /// <returns>The transaction response.</returns>
    private static TransactionResponse ToResponse(Transaction transaction, bool? potentialDuplicate = null) => new(
        transaction.Id,
        transaction.AccountId,
        transaction.TransferId,
        transaction.CategoryId,
        transaction.TransactionDate,
        transaction.Description,
        transaction.Amount,
        transaction.BalanceAfter,
        transaction.StatementLine,
        transaction.CreatedAt,
        transaction.UpdatedAt,
        potentialDuplicate);

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
    /// Creates the response returned when the category does not exist for the authenticated user.
    /// </summary>
    /// <returns>A not-found response.</returns>
    private NotFoundObjectResult CategoryNotFound() => NotFound(new
    {
        error = "The category could not be found.",
        errorCode = "category_not_found"
    });

    /// <summary>
    /// Checks whether a category is system-owned or belongs to the authenticated user.
    /// </summary>
    private Task<bool> IsCategoryAvailableAsync(
        Guid categoryId,
        Guid userId,
        CancellationToken cancellationToken) =>
        dbContext.Categories.AnyAsync(
            category => category.Id == categoryId
                && (category.IsSystem || category.UserId == userId),
            cancellationToken);

    /// <summary>
    /// Gets the IDs of the accounts owned by the user.
    /// </summary>
    /// <param name="userId">The ID of the owning user.</param>
    /// <returns>A query of the user's account IDs.</returns>
    private IQueryable<Guid> OwnedAccountIds(Guid userId) => dbContext.FinancialAccounts
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

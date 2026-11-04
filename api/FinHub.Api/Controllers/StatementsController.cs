using FinHub.Api.Data;
using FinHub.Api.DTOs.Statement;
using FinHub.Api.DTOs.Transaction;
using FinHub.Api.Models;
using FinHub.Api.Services;
using FinHub.Api.Statements;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinHub.Api.Controllers;

/// <summary>
/// Controller responsible for turning the authenticated user's uploaded statements into candidate transactions
/// and saving the lines the user confirms.
/// </summary>
/// <param name="dbContext">The database context.</param>
/// <param name="authClient">The authentication client.</param>
/// <param name="parsers">The statement parsers, one per file type.</param>
[ApiController]
[Route("api/statements")]
public sealed class StatementsController(
    FinHubDbContext dbContext,
    SupabaseAuthClient authClient,
    IEnumerable<IStatementParser> parsers) : AuthenticatedControllerBase(authClient)
{
    /// <summary>
    /// The largest request body accepted. A statement export is a few kilobytes; the parser's row and cell caps assume a bounded file.
    /// </summary>
    private const long MaxRequestBytes = 1024 * 1024;

    /// <summary>
    /// Parses an uploaded statement into candidate transactions for one of the authenticated user's accounts.
    /// Nothing is saved; a candidate the account already holds is flagged as a potential duplicate.
    /// </summary>
    /// <param name="request">The uploaded file and the account it belongs to.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The candidate transactions, in file order.</returns>
    [HttpPost("parse")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxRequestBytes)]
    public async Task<ActionResult<ParsedStatementResponse>> Parse(
        [FromForm] ParseStatementRequest request,
        CancellationToken cancellationToken)
    {
        (Guid? userId, ActionResult? failure) = await ResolveCurrentUserAsync(cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        Guid accountId = request.AccountId!.Value;
        if (!await OwnsAccountAsync(accountId, userId!.Value, cancellationToken))
        {
            return AccountNotFound();
        }

        IFormFile file = request.File!;
        IStatementParser? parser = parsers.FirstOrDefault(candidate => candidate.CanParse(file.FileName));
        if (parser is null)
        {
            return BadRequest(new
            {
                error = "Only CSV exports are supported. Upload a .csv file.",
                errorCode = "unsupported_file_type"
            });
        }

        ParsedStatement statement;
        try
        {
            await using Stream content = file.OpenReadStream();
            statement = await parser.ParseAsync(content, cancellationToken);
        }
        catch (StatementFormatException exception)
        {
            return BadRequest(new
            {
                error = exception.Message,
                errorCode = exception.ErrorCode,
                line = exception.Line
            });
        }

        HashSet<TransactionKey> existing = await FindExistingAsync(
            accountId,
            statement.Transactions.Select(transaction => transaction.TransactionDate).ToList(),
            cancellationToken);

        return Ok(ToResponse(statement, existing));
    }

    /// <summary>
    /// Saves the confirmed lines of a parsed statement into one of the authenticated user's accounts, all or nothing.
    /// A line the account already holds is refused unless the request confirms it.
    /// </summary>
    /// <param name="request">The account and the lines to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The saved transactions, in request order.</returns>
    [HttpPost("import")]
    public async Task<ActionResult<IReadOnlyList<TransactionResponse>>> Import(
        [FromBody] ImportStatementRequest request,
        CancellationToken cancellationToken)
    {
        (Guid? userId, ActionResult? failure) = await ResolveCurrentUserAsync(cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        Guid currentUserId = userId!.Value;
        Guid accountId = request.AccountId!.Value;
        if (!await OwnsAccountAsync(accountId, currentUserId, cancellationToken))
        {
            return AccountNotFound();
        }

        HashSet<Guid> missingCategoryIds = await FindMissingCategoriesAsync(request.Transactions, currentUserId, cancellationToken);
        if (missingCategoryIds.Count > 0)
        {
            return CategoryNotFound(request.Transactions
                .Where(line => line.CategoryId is Guid categoryId && missingCategoryIds.Contains(categoryId))
                .Select(line => line.StatementLine!.Value)
                .ToList());
        }

        HashSet<TransactionKey> existing = await FindExistingAsync(
            accountId,
            request.Transactions.Select(line => line.TransactionDate!.Value).ToList(),
            cancellationToken);
        List<int> unconfirmed = UnconfirmedDuplicates(request.Transactions, existing);
        if (unconfirmed.Count > 0)
        {
            return PotentialDuplicatesConflict(unconfirmed);
        }

        List<Transaction> transactions = request.Transactions.Select(line => ToTransaction(line, accountId)).ToList();
        dbContext.Transactions.AddRange(transactions);
        await dbContext.SaveChangesAsync(cancellationToken);

        return StatusCode(StatusCodes.Status201Created, transactions.Select(ToTransactionResponse).ToList());
    }

    /// <summary>
    /// Converts a parsed statement to its response representation, flagging the candidates the account already holds.
    /// </summary>
    /// <param name="statement">The parsed statement.</param>
    /// <param name="existing">The keys of the account's transactions dated within the statement.</param>
    /// <returns>The statement response.</returns>
    internal static ParsedStatementResponse ToResponse(ParsedStatement statement, IReadOnlySet<TransactionKey> existing) => new(
        statement.Bank,
        statement.AccountType,
        statement.SkippedRows,
        statement.Transactions
            .Select(transaction => new ParsedTransactionResponse(
                transaction.StatementLine,
                transaction.TransactionDate,
                transaction.Description,
                transaction.Amount,
                transaction.BalanceAfter,
                existing.Contains(KeyOf(transaction))))
            .ToList());

    /// <summary>
    /// Lists the statement lines that match a transaction the account already holds and were not confirmed, in request order.
    /// </summary>
    /// <param name="lines">The lines to save.</param>
    /// <param name="existing">The keys of the account's transactions dated within the request.</param>
    /// <returns>The statement lines the import refuses.</returns>
    internal static List<int> UnconfirmedDuplicates(IEnumerable<ImportTransactionRequest> lines, IReadOnlySet<TransactionKey> existing) =>
        lines
            .Where(line => !line.ConfirmPotentialDuplicate && existing.Contains(KeyOf(line)))
            .Select(line => line.StatementLine!.Value)
            .ToList();

    /// <summary>
    /// Builds a candidate's duplicate key, with the amount rounded the way the ledger stores it.
    /// </summary>
    /// <param name="transaction">The candidate.</param>
    /// <returns>The key to look up among the account's transactions.</returns>
    internal static TransactionKey KeyOf(ParsedTransaction transaction) => new(
        transaction.TransactionDate,
        transaction.Description,
        RoundToCents(transaction.Amount));

    /// <summary>
    /// Builds a line's duplicate key, normalized the way the ledger stores it: description trimmed, amount rounded.
    /// </summary>
    /// <param name="line">The line to save.</param>
    /// <returns>The key to look up among the account's transactions.</returns>
    internal static TransactionKey KeyOf(ImportTransactionRequest line) => new(
        line.TransactionDate!.Value,
        line.Description.Trim(),
        RoundToCents(line.Amount!.Value));

    /// <summary>
    /// Turns a line into the transaction to save. The date, description, and amount come from the line's key, so what
    /// the duplicate check compared is exactly what is stored.
    /// </summary>
    /// <param name="line">The line to save.</param>
    /// <param name="accountId">The ID of the account the statement belongs to.</param>
    /// <returns>The transaction, not yet tracked.</returns>
    internal static Transaction ToTransaction(ImportTransactionRequest line, Guid accountId)
    {
        TransactionKey key = KeyOf(line);
        return new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            CategoryId = line.CategoryId,
            TransactionDate = key.TransactionDate,
            Description = key.Description,
            Amount = key.Amount,
            BalanceAfter = line.BalanceAfter is decimal balanceAfter ? RoundToCents(balanceAfter) : null,
            StatementLine = line.StatementLine!.Value
        };
    }

    /// <summary>
    /// Rounds a money value to cents, half away from zero, as POST /api/transactions does before saving.
    /// </summary>
    /// <param name="value">The value to round.</param>
    /// <returns>The value with at most two decimals.</returns>
    private static decimal RoundToCents(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Converts a saved transaction to the representation GET /api/transactions returns.
    /// </summary>
    /// <param name="transaction">The transaction.</param>
    /// <returns>The transaction response.</returns>
    private static TransactionResponse ToTransactionResponse(Transaction transaction) => new(
        transaction.Id,
        transaction.AccountId,
        transaction.CategoryId,
        transaction.TransactionDate,
        transaction.Description,
        transaction.Amount,
        transaction.BalanceAfter,
        transaction.StatementLine,
        transaction.CreatedAt,
        transaction.UpdatedAt);

    /// <summary>
    /// Checks whether an account belongs to the user.
    /// </summary>
    /// <param name="accountId">The ID of the account.</param>
    /// <param name="userId">The ID of the owning user.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True when the account exists and the user owns it.</returns>
    private Task<bool> OwnsAccountAsync(Guid accountId, Guid userId, CancellationToken cancellationToken) =>
        dbContext.FinancialAccounts.AnyAsync(account => account.Id == accountId && account.UserId == userId, cancellationToken);

    /// <summary>
    /// Finds the category IDs the lines name that are neither system categories nor the user's.
    /// </summary>
    /// <param name="lines">The lines to save.</param>
    /// <param name="userId">The ID of the authenticated user.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The category IDs the user cannot use; empty when every line's category is available.</returns>
    private async Task<HashSet<Guid>> FindMissingCategoriesAsync(
        IEnumerable<ImportTransactionRequest> lines,
        Guid userId,
        CancellationToken cancellationToken)
    {
        HashSet<Guid> categoryIds = lines
            .Where(line => line.CategoryId is not null)
            .Select(line => line.CategoryId!.Value)
            .ToHashSet();
        if (categoryIds.Count == 0)
        {
            return categoryIds;
        }

        List<Guid> available = await dbContext.Categories
            .Where(category => categoryIds.Contains(category.Id) && (category.IsSystem || category.UserId == userId))
            .Select(category => category.Id)
            .ToListAsync(cancellationToken);
        categoryIds.ExceptWith(available);

        return categoryIds;
    }

    /// <summary>
    /// Loads the keys of the account's transactions dated within the given dates, the only ones a line can match.
    /// </summary>
    /// <param name="accountId">The ID of the account the statement belongs to.</param>
    /// <param name="dates">The dates of the lines.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The keys of the account's transactions in the lines' date range.</returns>
    private async Task<HashSet<TransactionKey>> FindExistingAsync(
        Guid accountId,
        IReadOnlyCollection<DateOnly> dates,
        CancellationToken cancellationToken)
    {
        if (dates.Count == 0)
        {
            return [];
        }

        DateOnly firstDate = dates.Min();
        DateOnly lastDate = dates.Max();

        return await dbContext.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.AccountId == accountId
                && transaction.TransactionDate >= firstDate
                && transaction.TransactionDate <= lastDate)
            .Select(transaction => new TransactionKey(transaction.TransactionDate, transaction.Description, transaction.Amount))
            .ToHashSetAsync(cancellationToken);
    }

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
    /// Creates the response returned when lines name a category the authenticated user cannot use.
    /// </summary>
    /// <param name="statementLines">The lines naming such a category.</param>
    /// <returns>A not-found response.</returns>
    private NotFoundObjectResult CategoryNotFound(IReadOnlyList<int> statementLines) => NotFound(new
    {
        error = "A category could not be found.",
        errorCode = "category_not_found",
        statementLines
    });

    /// <summary>
    /// Creates the response returned when lines match transactions the account already holds and were not confirmed.
    /// </summary>
    /// <param name="statementLines">The lines that may be duplicates.</param>
    /// <returns>A conflict response naming the lines.</returns>
    private ConflictObjectResult PotentialDuplicatesConflict(IReadOnlyList<int> statementLines) => Conflict(new
    {
        error = "Some lines may already be in the ledger. Set confirmPotentialDuplicate on them to import them anyway.",
        errorCode = "potential_duplicates",
        statementLines
    });

    /// <summary>
    /// The fields the duplicate check compares: the same date, description, and amount that POST /api/transactions checks before saving.
    /// </summary>
    /// <param name="TransactionDate">The date the transaction occurred.</param>
    /// <param name="Description">The trimmed description.</param>
    /// <param name="Amount">The signed amount, rounded to cents.</param>
    internal sealed record TransactionKey(DateOnly TransactionDate, string Description, decimal Amount);
}

using FinHub.Api.Data;
using FinHub.Api.DTOs.Statement;
using FinHub.Api.Services;
using FinHub.Api.Statements;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinHub.Api.Controllers;

/// <summary>
/// Controller responsible for turning the authenticated user's uploaded statements into candidate transactions.
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

        Guid currentUserId = userId!.Value;
        Guid accountId = request.AccountId!.Value;
        bool ownsAccount = await dbContext.FinancialAccounts
            .AnyAsync(account => account.Id == accountId && account.UserId == currentUserId, cancellationToken);
        if (!ownsAccount)
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

        HashSet<TransactionKey> existing = await FindExistingAsync(accountId, statement.Transactions, cancellationToken);

        return Ok(ToResponse(statement, existing));
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
    /// Builds a candidate's duplicate key, with the amount rounded the way POST /api/transactions would store it.
    /// </summary>
    /// <param name="transaction">The candidate.</param>
    /// <returns>The key to look up among the account's transactions.</returns>
    internal static TransactionKey KeyOf(ParsedTransaction transaction) => new(
        transaction.TransactionDate,
        transaction.Description,
        Math.Round(transaction.Amount, 2, MidpointRounding.AwayFromZero));

    /// <summary>
    /// Loads the keys of the account's transactions dated within the statement, the only ones a candidate can match.
    /// </summary>
    /// <param name="accountId">The ID of the account the statement belongs to.</param>
    /// <param name="candidates">The parsed transactions.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The keys of the account's transactions in the candidates' date range.</returns>
    private async Task<HashSet<TransactionKey>> FindExistingAsync(
        Guid accountId,
        IReadOnlyList<ParsedTransaction> candidates,
        CancellationToken cancellationToken)
    {
        if (candidates.Count == 0)
        {
            return [];
        }

        DateOnly firstDate = candidates.Min(candidate => candidate.TransactionDate);
        DateOnly lastDate = candidates.Max(candidate => candidate.TransactionDate);

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
    /// The fields the duplicate check compares: the same date, description, and amount that POST /api/transactions checks before saving.
    /// </summary>
    /// <param name="TransactionDate">The date the transaction occurred.</param>
    /// <param name="Description">The trimmed description.</param>
    /// <param name="Amount">The signed amount, rounded to cents.</param>
    internal sealed record TransactionKey(DateOnly TransactionDate, string Description, decimal Amount);
}

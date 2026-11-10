using System.Globalization;
using FinHub.Api.Data;
using FinHub.Api.DTOs.Overview;
using FinHub.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinHub.Api.Controllers;

/// <summary>
/// Provides server-side aggregates for the authenticated user's dashboard.
/// </summary>
/// <param name="dbContext">The database context.</param>
/// <param name="authClient">The authentication client.</param>
[ApiController]
[Route("api/overview")]
public sealed class OverviewController(
    FinHubDbContext dbContext,
    SupabaseAuthClient authClient) : AuthenticatedControllerBase(authClient)
{
    /// <summary>
    /// The default number of months to include in the dashboard trend.
    /// </summary>
    private const int DefaultMonths = 6;

    /// <summary>
    /// The minimum number of months to include in the dashboard trend.
    /// </summary>
    private const int MinimumMonths = 3;

    /// <summary>
    /// The maximum number of months to include in the dashboard trend.
    /// </summary>
    private const int MaximumMonths = 12;

    /// <summary>
    /// Gets account balances and cash-flow aggregates for the dashboard.
    /// </summary>
    /// <param name="months">The number of calendar months in the trend.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The authenticated user's dashboard data.</returns>
    [HttpGet]
    public async Task<ActionResult<OverviewDashboardResponse>> Get(
        [FromQuery] int? months,
        CancellationToken cancellationToken)
    {
        (Guid? userId, ActionResult? failure) = await ResolveCurrentUserAsync(cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        int monthCount = months ?? DefaultMonths;
        if (monthCount is < MinimumMonths or > MaximumMonths)
        {
            return BadRequest(new
            {
                error = $"The months value must be between {MinimumMonths} and {MaximumMonths}.",
                errorCode = "invalid_month_range"
            });
        }

        DashboardPeriod period = CreatePeriod(monthCount);
        List<DashboardAccount> accounts = await LoadAccountsAsync(userId!.Value, cancellationToken);
        List<DashboardTransaction> transactions = await LoadTransactionsAsync(
            userId.Value,
            accounts,
            cancellationToken);
        Dictionary<Guid, string> categoryNames = await LoadCategoryNamesAsync(
            userId.Value,
            cancellationToken);

        List<string> currencies = accounts
            .Select(account => account.Currency)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();

        List<OverviewAccountBalance> accountBalances = BuildAccountBalances(accounts, transactions);
        List<OverviewCurrencySummary> balances = BuildCurrencySummaries(
            currencies,
            transactions,
            _ => true);
        List<OverviewCurrencySummary> balancesBeforeCurrentMonth = BuildCurrencySummaries(
            currencies,
            transactions,
            transaction => transaction.TransactionDate <= period.PreviousMonthEnd);
        List<OverviewCurrencySummary> currentPeriod = BuildCurrencySummaries(
            currencies,
            transactions,
            transaction => transaction.TransactionDate >= period.CurrentMonth
                && transaction.TransactionDate <= period.Today);
        List<OverviewCurrencySummary> previousPeriod = BuildCurrencySummaries(
            currencies,
            transactions,
            transaction => transaction.TransactionDate >= period.PreviousMonthStart
                && transaction.TransactionDate <= period.PreviousMonthEnd);
        List<OverviewMonthlyPoint> monthlyTrend = BuildMonthlyTrend(
            currencies,
            transactions,
            period,
            monthCount);
        List<OverviewCategorySpending> categorySpending = BuildCategorySpending(
            transactions,
            categoryNames,
            period);

        return Ok(new OverviewDashboardResponse(
            period.PeriodStart,
            period.Today,
            monthCount,
            currencies,
            accountBalances,
            balances,
            balancesBeforeCurrentMonth,
            currentPeriod,
            previousPeriod,
            monthlyTrend,
            categorySpending));
    }

    /// <summary>
    /// Calculates the calendar boundaries used by the dashboard aggregates.
    /// </summary>
    /// <param name="monthCount">The number of months in the trend.</param>
    /// <returns>The dashboard period boundaries.</returns>
    internal static DashboardPeriod CreatePeriod(int monthCount)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        DateOnly currentMonth = new(today.Year, today.Month, 1);

        return new DashboardPeriod(
            currentMonth.AddMonths(-(monthCount - 1)),
            today,
            currentMonth,
            currentMonth.AddMonths(-1),
            currentMonth.AddDays(-1));
    }

    /// <summary>
    /// Loads the authenticated user's account fields needed for aggregation.
    /// </summary>
    /// <param name="userId">The authenticated user's ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The user's dashboard account projections.</returns>
    private Task<List<DashboardAccount>> LoadAccountsAsync(
        Guid userId,
        CancellationToken cancellationToken) =>
        dbContext.FinancialAccounts
            .AsNoTracking()
            .Where(account => account.UserId == userId)
            .Select(account => new DashboardAccount(
                account.Id,
                account.Name,
                account.AccountType,
                account.Currency))
            .ToListAsync(cancellationToken);

    /// <summary>
    /// Loads transactions belonging to the authenticated user's accounts.
    /// </summary>
    /// <param name="userId">The authenticated user's ID.</param>
    /// <param name="accounts">The user's accounts.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The transaction fields needed for dashboard aggregation.</returns>
    private Task<List<DashboardTransaction>> LoadTransactionsAsync(
        Guid userId,
        List<DashboardAccount> accounts,
        CancellationToken cancellationToken)
    {
        if (accounts.Count == 0)
        {
            return Task.FromResult<List<DashboardTransaction>>([]);
        }

        return (
            from transaction in dbContext.Transactions.AsNoTracking()
            join account in dbContext.FinancialAccounts.AsNoTracking()
                on transaction.AccountId equals account.Id
            where account.UserId == userId
            select new DashboardTransaction(
                transaction.AccountId,
                transaction.CategoryId,
                transaction.TransactionDate,
                transaction.Amount,
                account.Currency))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Loads system and user-owned category names for category aggregation.
    /// </summary>
    /// <param name="userId">The authenticated user's ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Category names keyed by category ID.</returns>
    private Task<Dictionary<Guid, string>> LoadCategoryNamesAsync(
        Guid userId,
        CancellationToken cancellationToken) =>
        dbContext.Categories
            .AsNoTracking()
            .Where(category => category.IsSystem || category.UserId == userId)
            .ToDictionaryAsync(category => category.Id, category => category.Name, cancellationToken);

    /// <summary>
    /// Builds signed balances for the user's accounts.
    /// </summary>
    /// <param name="accounts">The user's accounts.</param>
    /// <param name="transactions">The user's transactions.</param>
    /// <returns>Account balances ordered by absolute balance and name.</returns>
    internal static List<OverviewAccountBalance> BuildAccountBalances(
        IEnumerable<DashboardAccount> accounts,
        IEnumerable<DashboardTransaction> transactions) =>
        accounts
            .Select(account => new OverviewAccountBalance(
                account.Id,
                account.Name,
                account.AccountType,
                account.Currency,
                transactions
                    .Where(transaction => transaction.AccountId == account.Id)
                    .Sum(transaction => transaction.Amount)))
            .OrderByDescending(account => Math.Abs(account.Balance))
            .ThenBy(account => account.Name)
            .ToList();

    /// <summary>
    /// Builds one income, spending, and net summary for each currency.
    /// </summary>
    /// <param name="currencies">The currencies to summarize.</param>
    /// <param name="transactions">The user's transactions.</param>
    /// <param name="includeTransaction">The date or scope filter to apply.</param>
    /// <returns>The currency summaries.</returns>
    internal static List<OverviewCurrencySummary> BuildCurrencySummaries(
        IEnumerable<string> currencies,
        IEnumerable<DashboardTransaction> transactions,
        Func<DashboardTransaction, bool> includeTransaction) =>
        currencies
            .Select(currency => CreateSummary(
                currency,
                transactions.Where(transaction =>
                    transaction.Currency == currency
                    && includeTransaction(transaction))))
            .ToList();

    /// <summary>
    /// Builds one income, spending, and net point for each month and currency.
    /// </summary>
    /// <param name="currencies">The currencies to summarize.</param>
    /// <param name="transactions">The user's transactions.</param>
    /// <param name="period">The dashboard period boundaries.</param>
    /// <param name="monthCount">The number of months to include.</param>
    /// <returns>The monthly trend points.</returns>
    internal static List<OverviewMonthlyPoint> BuildMonthlyTrend(
        IEnumerable<string> currencies,
        IEnumerable<DashboardTransaction> transactions,
        DashboardPeriod period,
        int monthCount)
    {
        List<OverviewMonthlyPoint> monthlyTrend = [];

        for (int offset = 0; offset < monthCount; offset++)
        {
            DateOnly month = period.PeriodStart.AddMonths(offset);
            DateOnly monthEnd = month.AddMonths(1).AddDays(-1);

            foreach (string currency in currencies)
            {
                DashboardTransaction[] monthTransactions = transactions
                    .Where(transaction =>
                        transaction.Currency == currency
                        && transaction.TransactionDate >= month
                        && transaction.TransactionDate <= monthEnd)
                    .ToArray();
                OverviewCurrencySummary summary = CreateSummary(currency, monthTransactions);

                monthlyTrend.Add(new OverviewMonthlyPoint(
                    month,
                    month.ToDateTime(TimeOnly.MinValue).ToString("MMM yyyy", CultureInfo.InvariantCulture),
                    currency,
                    summary.Income,
                    summary.Spending,
                    summary.Net));
            }
        }

        return monthlyTrend;
    }

    /// <summary>
    /// Aggregates negative transactions by category for the dashboard period.
    /// </summary>
    /// <param name="transactions">The user's transactions.</param>
    /// <param name="categoryNames">Category names keyed by category ID.</param>
    /// <param name="period">The dashboard period boundaries.</param>
    /// <returns>Category spending ordered from highest to lowest amount.</returns>
    internal static List<OverviewCategorySpending> BuildCategorySpending(
        IEnumerable<DashboardTransaction> transactions,
        IReadOnlyDictionary<Guid, string> categoryNames,
        DashboardPeriod period) =>
        transactions
            .Where(transaction =>
                transaction.TransactionDate >= period.PeriodStart
                && transaction.TransactionDate <= period.Today
                && transaction.Amount < 0)
            .GroupBy(transaction => new
            {
                transaction.CategoryId,
                transaction.Currency
            })
            .Select(group => new OverviewCategorySpending(
                group.Key.CategoryId?.ToString() ?? "uncategorized",
                group.Key.CategoryId is Guid categoryId
                    && categoryNames.TryGetValue(categoryId, out string? name)
                    ? name
                    : "Uncategorized",
                group.Key.Currency,
                group.Sum(transaction => Math.Abs(transaction.Amount))))
            .OrderByDescending(category => category.Amount)
            .ThenBy(category => category.CategoryName)
            .ToList();

    /// <summary>
    /// Calculates income, spending, and net movement for a currency-scoped
    /// collection of transactions.
    /// </summary>
    /// <param name="currency">The currency represented by the transactions.</param>
    /// <param name="transactions">The transactions to aggregate.</param>
    /// <returns>The calculated currency summary.</returns>
    private static OverviewCurrencySummary CreateSummary(
        string currency,
        IEnumerable<DashboardTransaction> transactions)
    {
        decimal income = transactions
            .Where(transaction => transaction.Amount > 0)
            .Sum(transaction => transaction.Amount);
        decimal spending = transactions
            .Where(transaction => transaction.Amount < 0)
            .Sum(transaction => Math.Abs(transaction.Amount));

        return new OverviewCurrencySummary(currency, income, spending, income - spending);
    }

    /// <summary>
    /// Represents the calendar boundaries used by dashboard calculations.
    /// </summary>
    /// <param name="PeriodStart">The first date in the requested trend.</param>
    /// <param name="Today">The current UTC date.</param>
    /// <param name="CurrentMonth">The first date of the current month.</param>
    /// <param name="PreviousMonthStart">The first date of the previous month.</param>
    /// <param name="PreviousMonthEnd">The last date of the previous month.</param>
    internal sealed record DashboardPeriod(
        DateOnly PeriodStart,
        DateOnly Today,
        DateOnly CurrentMonth,
        DateOnly PreviousMonthStart,
        DateOnly PreviousMonthEnd);

    /// <summary>
    /// Represents the account fields required while building dashboard aggregates.
    /// </summary>
    /// <param name="Id">The account identifier.</param>
    /// <param name="Name">The account display name.</param>
    /// <param name="AccountType">The account type.</param>
    /// <param name="Currency">The account currency.</param>
    internal sealed record DashboardAccount(
        Guid Id,
        string Name,
        string AccountType,
        string Currency);

    /// <summary>
    /// Represents the transaction fields required by dashboard calculations.
    /// </summary>
    /// <param name="AccountId">The account containing the transaction.</param>
    /// <param name="CategoryId">The optional transaction category.</param>
    /// <param name="TransactionDate">The date of the transaction.</param>
    /// <param name="Amount">The signed transaction amount.</param>
    /// <param name="Currency">The currency of the owning account.</param>
    internal sealed record DashboardTransaction(
        Guid AccountId,
        Guid? CategoryId,
        DateOnly TransactionDate,
        decimal Amount,
        string Currency);
}

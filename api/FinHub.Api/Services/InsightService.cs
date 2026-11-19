using FinHub.Api.Data;
using FinHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FinHub.Api.Services;

/// <summary>
/// Computes a user's insights from their transactions on each request; nothing is stored.
/// </summary>
/// <param name="dbContext">The database context.</param>
/// <param name="timeProvider">The provider used to determine the current time.</param>
/// <param name="dashboardTimeZone">The time zone used for date boundaries.</param>
public sealed class InsightService(
    FinHubDbContext dbContext,
    TimeProvider timeProvider,
    TimeZoneInfo dashboardTimeZone) : IInsightService
{
    /// <summary>
    /// The default number of months to evaluate.
    /// </summary>
    public const int DefaultMonths = 6;

    /// <summary>
    /// The minimum number of months to evaluate.
    /// </summary>
    public const int MinimumMonths = 3;

    /// <summary>
    /// The maximum number of months to evaluate.
    /// </summary>
    public const int MaximumMonths = 12;

    /// <inheritdoc />
    public async Task<IReadOnlyList<Insight>> GetInsightsAsync(
        Guid userId,
        int months,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(months, MinimumMonths);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(months, MaximumMonths);

        DateOnly today = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), dashboardTimeZone).DateTime);
        DateOnly loadFrom = InsightRules.GetHistoryStart(months, today)
            .AddMonths(-InsightRules.CategorySpikeLookbackMonths);

        List<InsightTransaction> transactions = await (
            from transaction in dbContext.Transactions.AsNoTracking()
            join account in dbContext.FinancialAccounts.AsNoTracking()
                on transaction.AccountId equals account.Id
            where account.UserId == userId
                && transaction.TransactionDate >= loadFrom
                && transaction.TransactionDate <= today
            select new InsightTransaction(
                transaction.Id,
                transaction.AccountId,
                transaction.TransferId,
                transaction.CategoryId,
                transaction.TransactionDate,
                transaction.Amount,
                account.Currency))
            .ToListAsync(cancellationToken);
        List<InsightPreference> preferences = await dbContext.InsightPreferences
            .AsNoTracking()
            .Where(preference => preference.UserId == userId)
            .ToListAsync(cancellationToken);
        List<InsightMute> mutes = await dbContext.InsightMutes
            .AsNoTracking()
            .Where(mute => mute.UserId == userId)
            .ToListAsync(cancellationToken);
        List<InsightState> states = await dbContext.InsightStates
            .AsNoTracking()
            .Where(state => state.UserId == userId)
            .ToListAsync(cancellationToken);

        return InsightRules.FindInsights(
            transactions,
            preferences,
            mutes,
            states,
            months,
            today);
    }
}

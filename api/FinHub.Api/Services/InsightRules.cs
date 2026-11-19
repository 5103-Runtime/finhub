using FinHub.Api.Constants;
using FinHub.Api.Models;

namespace FinHub.Api.Services;

/// <summary>
/// Computes insights from transactions that are already loaded.
/// </summary>
internal static class InsightRules
{
    /// <summary>
    /// The default percent above the baseline at which a category spike is reported.
    /// </summary>
    internal const decimal DefaultCategorySpikeThreshold = 50m;

    /// <summary>
    /// The number of months averaged into a category spike baseline:
    /// the most recent months before the evaluated one that have spending in the category.
    /// </summary>
    internal const int CategorySpikeBaselineMonths = 3;

    /// <summary>
    /// The number of full calendar months before the evaluated one searched for baseline months.
    /// </summary>
    internal const int CategorySpikeLookbackMonths = 12;

    /// <summary>
    /// The default multiple of the category median at which an unusual purchase is reported.
    /// </summary>
    internal const decimal DefaultUnusualPurchaseThreshold = 3m;

    /// <summary>
    /// The number of days before a purchase whose purchases make up its median.
    /// </summary>
    internal const int UnusualPurchaseWindowDays = 90;

    /// <summary>
    /// The number of earlier purchases a median needs before a purchase can be reported.
    /// </summary>
    internal const int UnusualPurchaseMinimumPurchases = 3;

    /// <summary>
    /// The multiple of the threshold at which an insight becomes critical.
    /// </summary>
    internal const decimal CriticalThresholdMultiplier = 2m;

    /// <summary>
    /// The multiple of the dismissed amount at which a dismissed insight is shown again.
    /// </summary>
    internal const decimal DismissedResurfaceMultiplier = 1.25m;

    /// <summary>
    /// Computes the insights to show for the given number of months, ending with the current month.
    /// Muted transactions are left out, turned-off insight types are skipped,
    /// and the user's read and dismissed markers are applied.
    /// </summary>
    /// <param name="transactions">The user's transactions, reaching back far enough for every baseline.</param>
    /// <param name="preferences">The user's preferences.</param>
    /// <param name="mutes">The user's mutes.</param>
    /// <param name="states">The user's insight states.</param>
    /// <param name="months">The number of calendar months to evaluate.</param>
    /// <param name="today">The current date; later transactions are ignored.</param>
    /// <returns>
    /// Category spikes from the newest month to the oldest, then unusual purchases,
    /// each from highest to lowest amount.
    /// </returns>
    internal static List<Insight> FindInsights(
        IEnumerable<InsightTransaction> transactions,
        IEnumerable<InsightPreference> preferences,
        IEnumerable<InsightMute> mutes,
        IEnumerable<InsightState> states,
        int months,
        DateOnly today)
    {
        List<InsightTransaction> included = ExcludeMuted(transactions, mutes);
        List<InsightPreference> preferenceList = preferences.ToList();
        DateOnly historyStart = GetHistoryStart(months, today);
        List<Insight> insights = [];

        if (ResolveThreshold(
            preferenceList,
            InsightTypes.CategorySpike,
            DefaultCategorySpikeThreshold) is decimal spikeThreshold)
        {
            for (int offset = months - 1; offset >= 0; offset--)
            {
                insights.AddRange(FindCategorySpikes(
                    included,
                    historyStart.AddMonths(offset),
                    today,
                    spikeThreshold));
            }
        }

        if (ResolveThreshold(
            preferenceList,
            InsightTypes.UnusualPurchase,
            DefaultUnusualPurchaseThreshold) is decimal purchaseThreshold)
        {
            insights.AddRange(FindUnusualPurchases(
                included,
                historyStart,
                today,
                purchaseThreshold));
        }

        return ApplyStates(insights, states);
    }

    /// <summary>
    /// Gets the first day of the oldest month evaluated.
    /// </summary>
    /// <param name="months">The number of calendar months to evaluate, ending with the current month.</param>
    /// <param name="today">The current date.</param>
    /// <returns>The first day of the oldest evaluated month.</returns>
    internal static DateOnly GetHistoryStart(int months, DateOnly today) =>
        new DateOnly(today.Year, today.Month, 1).AddMonths(-(months - 1));

    /// <summary>
    /// Finds categories whose spending in one month is above the average of their
    /// most recent earlier months with spending.
    /// </summary>
    /// <param name="transactions">The user's transactions.</param>
    /// <param name="month">Any date in the calendar month to evaluate.</param>
    /// <param name="today">The current date; later transactions are ignored.</param>
    /// <param name="threshold">The percent above the baseline at which a spike is reported.</param>
    /// <returns>One insight per category and currency, ordered from highest to lowest amount.</returns>
    internal static List<Insight> FindCategorySpikes(
        IEnumerable<InsightTransaction> transactions,
        DateOnly month,
        DateOnly today,
        decimal threshold)
    {
        DateOnly periodStart = new(month.Year, month.Month, 1);
        DateOnly nextPeriodStart = periodStart.AddMonths(1);
        DateOnly lookbackStart = periodStart.AddMonths(-CategorySpikeLookbackMonths);
        List<Insight> insights = [];

        var groups = transactions
            .Where(transaction =>
                IsSpending(transaction)
                && transaction.TransactionDate >= lookbackStart
                && transaction.TransactionDate < nextPeriodStart
                && transaction.TransactionDate <= today)
            .GroupBy(transaction => new
            {
                CategoryId = transaction.CategoryId!.Value,
                transaction.Currency
            });

        foreach (var group in groups)
        {
            decimal amount = group
                .Where(transaction => transaction.TransactionDate >= periodStart)
                .Sum(transaction => -transaction.Amount);
            decimal[] baselineMonths = group
                .Where(transaction => transaction.TransactionDate < periodStart)
                .GroupBy(transaction => new DateOnly(
                    transaction.TransactionDate.Year,
                    transaction.TransactionDate.Month,
                    1))
                .OrderByDescending(monthGroup => monthGroup.Key)
                .Take(CategorySpikeBaselineMonths)
                .Select(monthGroup => monthGroup.Sum(transaction => -transaction.Amount))
                .ToArray();

            if (baselineMonths.Length == 0)
            {
                continue;
            }

            decimal baseline = Math.Round(
                baselineMonths.Average(),
                2,
                MidpointRounding.AwayFromZero);

            if (baseline <= 0m || amount < baseline * (1m + threshold / 100m))
            {
                continue;
            }

            decimal criticalAmount =
                baseline * (1m + threshold * CriticalThresholdMultiplier / 100m);

            insights.Add(new Insight(
                InsightTypes.CategorySpike,
                GetSeverity(amount, criticalAmount),
                group.Key.CategoryId,
                null,
                group.Key.Currency,
                periodStart,
                amount,
                baseline));
        }

        return insights
            .OrderByDescending(insight => insight.Amount)
            .ThenBy(insight => insight.CategoryId)
            .ThenBy(insight => insight.Currency, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Finds purchases that are well above the median of the earlier purchases
    /// in the same category and currency.
    /// </summary>
    /// <param name="transactions">The user's transactions.</param>
    /// <param name="from">The first date whose purchases are evaluated.</param>
    /// <param name="today">The current date; later transactions are ignored.</param>
    /// <param name="threshold">The multiple of the median at which a purchase is reported.</param>
    /// <returns>One insight per purchase, ordered from highest to lowest amount.</returns>
    internal static List<Insight> FindUnusualPurchases(
        IEnumerable<InsightTransaction> transactions,
        DateOnly from,
        DateOnly today,
        decimal threshold)
    {
        List<Insight> insights = [];

        var groups = transactions
            .Where(transaction =>
                IsSpending(transaction)
                && transaction.TransactionDate <= today)
            .GroupBy(transaction => new
            {
                CategoryId = transaction.CategoryId!.Value,
                transaction.Currency
            });

        foreach (var group in groups)
        {
            foreach (InsightTransaction purchase in group
                .Where(transaction => transaction.TransactionDate >= from))
            {
                DateOnly windowStart = purchase.TransactionDate.AddDays(-UnusualPurchaseWindowDays);
                decimal[] earlierAmounts = group
                    .Where(transaction =>
                        transaction.TransactionDate >= windowStart
                        && transaction.TransactionDate < purchase.TransactionDate)
                    .Select(transaction => -transaction.Amount)
                    .Order()
                    .ToArray();

                if (earlierAmounts.Length < UnusualPurchaseMinimumPurchases)
                {
                    continue;
                }

                decimal amount = -purchase.Amount;
                decimal median = GetMedian(earlierAmounts);

                if (amount < median * threshold)
                {
                    continue;
                }

                insights.Add(new Insight(
                    InsightTypes.UnusualPurchase,
                    GetSeverity(amount, median * threshold * CriticalThresholdMultiplier),
                    null,
                    purchase.Id,
                    null,
                    null,
                    amount,
                    median));
            }
        }

        return insights
            .OrderByDescending(insight => insight.Amount)
            .ThenBy(insight => insight.TransactionId)
            .ToList();
    }

    /// <summary>
    /// Calculates the median of amounts that are already sorted.
    /// </summary>
    /// <param name="sortedAmounts">The amounts in ascending order; at least one.</param>
    /// <returns>The middle amount, or the average of the two middle amounts, to two decimal places.</returns>
    internal static decimal GetMedian(IReadOnlyList<decimal> sortedAmounts)
    {
        int middle = sortedAmounts.Count / 2;

        return sortedAmounts.Count % 2 == 1
            ? sortedAmounts[middle]
            : Math.Round(
                (sortedAmounts[middle - 1] + sortedAmounts[middle]) / 2m,
                2,
                MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Removes transactions on muted accounts or in muted categories,
    /// so they neither raise an insight nor count toward a baseline.
    /// </summary>
    /// <param name="transactions">The user's transactions.</param>
    /// <param name="mutes">The user's mutes.</param>
    /// <returns>The transactions that are not muted.</returns>
    internal static List<InsightTransaction> ExcludeMuted(
        IEnumerable<InsightTransaction> transactions,
        IEnumerable<InsightMute> mutes)
    {
        HashSet<Guid> mutedAccountIds = [];
        HashSet<Guid> mutedCategoryIds = [];

        foreach (InsightMute mute in mutes)
        {
            if (mute.AccountId is Guid accountId)
            {
                mutedAccountIds.Add(accountId);
            }

            if (mute.CategoryId is Guid categoryId)
            {
                mutedCategoryIds.Add(categoryId);
            }
        }

        return transactions
            .Where(transaction =>
                !mutedAccountIds.Contains(transaction.AccountId)
                && (transaction.CategoryId is not Guid categoryId
                    || !mutedCategoryIds.Contains(categoryId)))
            .ToList();
    }

    /// <summary>
    /// Resolves the threshold to use for an insight type from the user's preferences.
    /// </summary>
    /// <param name="preferences">The user's preferences.</param>
    /// <param name="insightType">The insight type.</param>
    /// <param name="defaultThreshold">The threshold used when the user has not set one.</param>
    /// <returns>The threshold, or null when the user has turned the insight type off.</returns>
    internal static decimal? ResolveThreshold(
        IEnumerable<InsightPreference> preferences,
        string insightType,
        decimal defaultThreshold)
    {
        InsightPreference? preference = preferences
            .FirstOrDefault(candidate => candidate.InsightType == insightType);

        if (preference is null)
        {
            return defaultThreshold;
        }

        return preference.IsEnabled
            ? preference.Threshold ?? defaultThreshold
            : null;
    }

    /// <summary>
    /// Applies the user's read and dismissed markers to computed insights.
    /// A dismissed insight is left out unless its amount has reached
    /// <see cref="DismissedResurfaceMultiplier"/> times the amount it was dismissed at;
    /// it then comes back unread until it is read again.
    /// </summary>
    /// <param name="insights">The computed insights.</param>
    /// <param name="states">The user's insight states.</param>
    /// <returns>The insights to show, in their original order, with their read flag set.</returns>
    internal static List<Insight> ApplyStates(
        IEnumerable<Insight> insights,
        IEnumerable<InsightState> states)
    {
        List<InsightState> stateList = states.ToList();
        List<Insight> visible = [];

        foreach (Insight insight in insights)
        {
            InsightState? state = stateList
                .FirstOrDefault(candidate => IsStateOf(candidate, insight));

            if (state is null)
            {
                visible.Add(insight with { IsRead = false });
                continue;
            }

            if (state.DismissedAt is not null
                && insight.Amount < state.DismissedValue * DismissedResurfaceMultiplier)
            {
                continue;
            }

            visible.Add(insight with
            {
                IsRead = state.ReadAt is not null
                    && (state.DismissedAt is null || state.ReadAt > state.DismissedAt)
            });
        }

        return visible;
    }

    /// <summary>
    /// Determines whether a state belongs to an insight by comparing the subject fields.
    /// </summary>
    /// <param name="state">The stored state.</param>
    /// <param name="insight">The computed insight.</param>
    /// <returns>True when the state and the insight have the same type and subject.</returns>
    internal static bool IsStateOf(InsightState state, Insight insight) =>
        state.InsightType == insight.InsightType
        && state.CategoryId == insight.CategoryId
        && state.TransactionId == insight.TransactionId
        && state.Currency == insight.Currency
        && state.PeriodStart == insight.PeriodStart;

    /// <summary>
    /// Determines whether a transaction counts as spending in a category.
    /// Transfer legs, uncategorized transactions, and money in do not.
    /// </summary>
    /// <param name="transaction">The transaction to check.</param>
    /// <returns>True when the transaction is categorized money out that is not a transfer leg.</returns>
    internal static bool IsSpending(InsightTransaction transaction) =>
        transaction.TransferId is null
        && transaction.CategoryId is not null
        && transaction.Amount < 0m;

    /// <summary>
    /// Derives the severity of an insight that has already reached its threshold.
    /// </summary>
    /// <param name="amount">The amount the insight is about.</param>
    /// <param name="criticalAmount">The amount at which the insight becomes critical.</param>
    /// <returns>The severity.</returns>
    internal static string GetSeverity(decimal amount, decimal criticalAmount) =>
        amount >= criticalAmount
            ? InsightSeverities.Critical
            : InsightSeverities.Warning;
}

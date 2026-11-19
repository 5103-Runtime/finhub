using FinHub.Api.Constants;

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
    /// The multiple of the threshold at which an insight becomes critical.
    /// </summary>
    internal const decimal CriticalThresholdMultiplier = 2m;

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

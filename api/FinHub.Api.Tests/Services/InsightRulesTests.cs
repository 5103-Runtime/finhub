using FinHub.Api.Constants;
using FinHub.Api.Models;
using FinHub.Api.Services;

namespace FinHub.Api.Tests.Services;

[TestClass]
public sealed class InsightRulesTests
{
    private static readonly DateOnly Today = new(2026, 11, 18);
    private static readonly DateOnly CurrentMonth = new(2026, 11, 1);
    private static readonly DateOnly August = new(2026, 8, 10);
    private static readonly DateOnly September = new(2026, 9, 10);
    private static readonly DateOnly October = new(2026, 10, 10);
    private static readonly DateOnly November = new(2026, 11, 10);
    private static readonly Guid ChequingId = Guid.NewGuid();
    private static readonly Guid VisaId = Guid.NewGuid();
    private static readonly Guid FoodCategoryId = Guid.NewGuid();
    private static readonly Guid RentCategoryId = Guid.NewGuid();

    [TestMethod]
    public void FindCategorySpikes_ReportsAWarningAtTheThreshold()
    {
        List<InsightTransaction> transactions =
        [
            .. BaselineOf(100m),
            Spend(-150m, November)
        ];

        List<Insight> insights = InsightRules.FindCategorySpikes(transactions, CurrentMonth, Today, 50m);

        Assert.AreEqual(1, insights.Count);
        Assert.AreEqual(
            new Insight(
                InsightTypes.CategorySpike,
                InsightSeverities.Warning,
                FoodCategoryId,
                null,
                "CAD",
                new DateOnly(2026, 11, 1),
                150m,
                100m),
            insights[0]);
    }

    [TestMethod]
    public void FindCategorySpikes_ReportsNothingBelowTheThreshold()
    {
        List<InsightTransaction> transactions =
        [
            .. BaselineOf(100m),
            Spend(-149.99m, November)
        ];

        Assert.AreEqual(0, InsightRules.FindCategorySpikes(transactions, CurrentMonth, Today, 50m).Count);
    }

    [TestMethod]
    public void FindCategorySpikes_ReportsCriticalAtDoubleTheThreshold()
    {
        List<InsightTransaction> justBelow =
        [
            .. BaselineOf(100m),
            Spend(-199.99m, November)
        ];
        List<InsightTransaction> reached =
        [
            .. BaselineOf(100m),
            Spend(-200m, November)
        ];

        Assert.AreEqual(
            InsightSeverities.Warning,
            InsightRules.FindCategorySpikes(justBelow, CurrentMonth, Today, 50m)[0].Severity);
        Assert.AreEqual(
            InsightSeverities.Critical,
            InsightRules.FindCategorySpikes(reached, CurrentMonth, Today, 50m)[0].Severity);
    }

    [TestMethod]
    public void FindCategorySpikes_UsesTheGivenThreshold()
    {
        List<InsightTransaction> transactions =
        [
            .. BaselineOf(100m),
            Spend(-150m, November)
        ];

        Assert.AreEqual(0, InsightRules.FindCategorySpikes(transactions, CurrentMonth, Today, 100m).Count);
        Assert.AreEqual(
            InsightSeverities.Critical,
            InsightRules.FindCategorySpikes(transactions, CurrentMonth, Today, 25m)[0].Severity);
    }

    [TestMethod]
    public void FindCategorySpikes_AveragesOnlyTheMonthsWithSpending()
    {
        List<InsightTransaction> transactions =
        [
            Spend(-100m, August),
            Spend(-200m, October),
            Spend(-1000m, November)
        ];

        List<Insight> insights = InsightRules.FindCategorySpikes(transactions, CurrentMonth, Today, 50m);

        Assert.AreEqual(1, insights.Count);
        Assert.AreEqual(150m, insights[0].Baseline);
        Assert.AreEqual(InsightSeverities.Critical, insights[0].Severity);
    }

    [TestMethod]
    public void FindCategorySpikes_ReportsNothingForABillPaidEveryFewMonths()
    {
        List<InsightTransaction> transactions =
        [
            Spend(-300m, new DateOnly(2026, 5, 10)),
            Spend(-300m, August),
            Spend(-300m, November)
        ];

        Assert.AreEqual(0, InsightRules.FindCategorySpikes(transactions, CurrentMonth, Today, 50m).Count);
    }

    [TestMethod]
    public void FindCategorySpikes_ReportsNothingWithoutPriorSpending()
    {
        List<InsightTransaction> transactions = [Spend(-500m, November)];

        Assert.AreEqual(0, InsightRules.FindCategorySpikes(transactions, CurrentMonth, Today, 50m).Count);
    }

    [TestMethod]
    public void FindCategorySpikes_NeedsTwoEarlierMonthsWithSpending()
    {
        List<InsightTransaction> oneEarlierMonth =
        [
            Spend(-15m, new DateOnly(2026, 3, 10)),
            Spend(-40m, November)
        ];
        List<InsightTransaction> twoEarlierMonths =
        [
            Spend(-15m, new DateOnly(2026, 3, 10)),
            Spend(-15m, new DateOnly(2026, 6, 10)),
            Spend(-40m, November)
        ];

        Assert.AreEqual(0, InsightRules.FindCategorySpikes(oneEarlierMonth, CurrentMonth, Today, 50m).Count);
        Assert.AreEqual(1, InsightRules.FindCategorySpikes(twoEarlierMonths, CurrentMonth, Today, 50m).Count);
    }

    [TestMethod]
    public void FindCategorySpikes_SumsSeveralTransactionsInTheMonth()
    {
        List<InsightTransaction> transactions =
        [
            .. BaselineOf(100m),
            Spend(-80m, new DateOnly(2026, 11, 1)),
            Spend(-70m, Today)
        ];

        List<Insight> insights = InsightRules.FindCategorySpikes(transactions, CurrentMonth, Today, 50m);

        Assert.AreEqual(1, insights.Count);
        Assert.AreEqual(150m, insights[0].Amount);
    }

    [TestMethod]
    public void FindCategorySpikes_IgnoresTransferLegsUncategorizedAndMoneyIn()
    {
        List<InsightTransaction> transactions =
        [
            .. BaselineOf(100m),
            Spend(-100m, November),
            Spend(-500m, November, transferId: Guid.NewGuid()),
            Spend(-500m, November, uncategorized: true),
            Spend(500m, November)
        ];

        Assert.AreEqual(0, InsightRules.FindCategorySpikes(transactions, CurrentMonth, Today, 50m).Count);
    }

    [TestMethod]
    public void FindCategorySpikes_UsesTheThreeMostRecentMonthsWithSpending()
    {
        List<InsightTransaction> transactions =
        [
            Spend(-900m, new DateOnly(2026, 7, 31)),
            .. BaselineOf(100m),
            Spend(-150m, November)
        ];

        List<Insight> insights = InsightRules.FindCategorySpikes(transactions, CurrentMonth, Today, 50m);

        Assert.AreEqual(1, insights.Count);
        Assert.AreEqual(100m, insights[0].Baseline);
    }

    [TestMethod]
    public void FindCategorySpikes_LooksBackTwelveMonthsForSpending()
    {
        List<InsightTransaction> transactions =
        [
            Spend(-150m, new DateOnly(2025, 11, 1)),
            Spend(-150m, new DateOnly(2026, 2, 10)),
            Spend(-1000m, November)
        ];

        List<Insight> insights = InsightRules.FindCategorySpikes(transactions, CurrentMonth, Today, 50m);

        Assert.AreEqual(1, insights.Count);
        Assert.AreEqual(150m, insights[0].Baseline);
    }

    [TestMethod]
    public void FindCategorySpikes_IgnoresTransactionsOutsideTheWindow()
    {
        List<InsightTransaction> transactions =
        [
            Spend(-10m, new DateOnly(2025, 10, 31)),
            .. BaselineOf(100m),
            Spend(-100m, November),
            Spend(-500m, Today.AddDays(1))
        ];
        List<InsightTransaction> onlyOlderSpending =
        [
            Spend(-10m, new DateOnly(2025, 10, 31)),
            Spend(-500m, November)
        ];

        Assert.AreEqual(0, InsightRules.FindCategorySpikes(transactions, CurrentMonth, Today, 50m).Count);
        Assert.AreEqual(0, InsightRules.FindCategorySpikes(onlyOlderSpending, CurrentMonth, Today, 50m).Count);
    }

    [TestMethod]
    public void FindCategorySpikes_EvaluatesAnEarlierMonthInFull()
    {
        List<InsightTransaction> transactions =
        [
            .. BaselineOf(100m),
            Spend(-60m, new DateOnly(2026, 10, 31)),
            Spend(-5000m, November)
        ];

        List<Insight> insights = InsightRules.FindCategorySpikes(
            transactions,
            October,
            Today,
            50m);

        Assert.AreEqual(1, insights.Count);
        Assert.AreEqual(new DateOnly(2026, 10, 1), insights[0].PeriodStart);
        Assert.AreEqual(160m, insights[0].Amount);
        Assert.AreEqual(100m, insights[0].Baseline);
    }

    [TestMethod]
    public void FindCategorySpikes_ReportsNothingForAMonthAfterToday()
    {
        List<InsightTransaction> transactions =
        [
            .. BaselineOf(100m),
            Spend(-500m, new DateOnly(2026, 12, 1))
        ];

        Assert.AreEqual(
            0,
            InsightRules.FindCategorySpikes(
                transactions,
                new DateOnly(2026, 12, 1),
                Today,
                50m).Count);
    }

    [TestMethod]
    public void FindCategorySpikes_KeepsCurrenciesAndCategoriesApart()
    {
        List<InsightTransaction> transactions =
        [
            .. BaselineOf(100m),
            Spend(-100m, November),
            Spend(-100m, November, currency: "USD"),
            Spend(-100m, November, categoryId: RentCategoryId)
        ];

        Assert.AreEqual(0, InsightRules.FindCategorySpikes(transactions, CurrentMonth, Today, 50m).Count);
    }

    [TestMethod]
    public void FindCategorySpikes_OrdersFromHighestToLowestAmount()
    {
        List<InsightTransaction> transactions =
        [
            .. BaselineOf(100m),
            .. BaselineOf(1000m, RentCategoryId),
            Spend(-150m, November),
            Spend(-1500m, November, categoryId: RentCategoryId)
        ];

        List<Insight> insights = InsightRules.FindCategorySpikes(transactions, CurrentMonth, Today, 50m);

        Assert.AreEqual(2, insights.Count);
        Assert.AreEqual(RentCategoryId, insights[0].CategoryId);
        Assert.AreEqual(FoodCategoryId, insights[1].CategoryId);
    }

    [TestMethod]
    public void FindUnusualPurchases_ReportsAWarningAtTheThreshold()
    {
        InsightTransaction purchase = Spend(-60m, November);
        List<InsightTransaction> transactions = [.. UsualPurchases(20m), purchase];

        List<Insight> insights = InsightRules.FindUnusualPurchases(transactions, CurrentMonth, Today, 3m);

        Assert.AreEqual(1, insights.Count);
        Assert.AreEqual(
            new Insight(
                InsightTypes.UnusualPurchase,
                InsightSeverities.Warning,
                null,
                purchase.Id,
                null,
                null,
                60m,
                20m),
            insights[0]);
    }

    [TestMethod]
    public void FindUnusualPurchases_ReportsNothingBelowTheThreshold()
    {
        List<InsightTransaction> transactions =
        [
            .. UsualPurchases(20m),
            Spend(-59.99m, November)
        ];

        Assert.AreEqual(0, InsightRules.FindUnusualPurchases(transactions, CurrentMonth, Today, 3m).Count);
    }

    [TestMethod]
    public void FindUnusualPurchases_ReportsCriticalAtDoubleTheThreshold()
    {
        List<InsightTransaction> justBelow =
        [
            .. UsualPurchases(20m),
            Spend(-119.99m, November)
        ];
        List<InsightTransaction> reached =
        [
            .. UsualPurchases(20m),
            Spend(-120m, November)
        ];

        Assert.AreEqual(
            InsightSeverities.Warning,
            InsightRules.FindUnusualPurchases(justBelow, CurrentMonth, Today, 3m)[0].Severity);
        Assert.AreEqual(
            InsightSeverities.Critical,
            InsightRules.FindUnusualPurchases(reached, CurrentMonth, Today, 3m)[0].Severity);
    }

    [TestMethod]
    public void FindUnusualPurchases_UsesTheGivenThreshold()
    {
        List<InsightTransaction> transactions =
        [
            .. UsualPurchases(20m),
            Spend(-60m, November)
        ];

        Assert.AreEqual(0, InsightRules.FindUnusualPurchases(transactions, CurrentMonth, Today, 4m).Count);
        Assert.AreEqual(
            InsightSeverities.Critical,
            InsightRules.FindUnusualPurchases(transactions, CurrentMonth, Today, 1.5m)[0].Severity);
    }

    [TestMethod]
    public void FindUnusualPurchases_NeedsThreeEarlierPurchases()
    {
        List<InsightTransaction> transactions =
        [
            Spend(-20m, September),
            Spend(-20m, October),
            Spend(-500m, November)
        ];

        Assert.AreEqual(0, InsightRules.FindUnusualPurchases(transactions, CurrentMonth, Today, 3m).Count);
    }

    [TestMethod]
    public void FindUnusualPurchases_ComparesWithTheNinetyDaysBeforeThePurchase()
    {
        List<InsightTransaction> oldestJustOutside =
        [
            Spend(-20m, new DateOnly(2026, 8, 11)),
            Spend(-20m, September),
            Spend(-20m, October),
            Spend(-500m, November)
        ];
        List<InsightTransaction> oldestJustInside =
        [
            Spend(-20m, new DateOnly(2026, 8, 12)),
            Spend(-20m, September),
            Spend(-20m, October),
            Spend(-500m, November)
        ];

        Assert.AreEqual(0, InsightRules.FindUnusualPurchases(oldestJustOutside, CurrentMonth, Today, 3m).Count);
        Assert.AreEqual(1, InsightRules.FindUnusualPurchases(oldestJustInside, CurrentMonth, Today, 3m).Count);
    }

    [TestMethod]
    public void FindUnusualPurchases_ComparesOnlyWithEarlierDays()
    {
        List<InsightTransaction> transactions =
        [
            Spend(-20m, September),
            Spend(-20m, October),
            Spend(-20m, November),
            Spend(-500m, November),
            Spend(-20m, new DateOnly(2026, 11, 11))
        ];

        Assert.AreEqual(0, InsightRules.FindUnusualPurchases(transactions, CurrentMonth, Today, 3m).Count);
    }

    [TestMethod]
    public void FindUnusualPurchases_UsesTheMedianNotTheAverage()
    {
        List<InsightTransaction> transactions =
        [
            Spend(-10m, new DateOnly(2026, 10, 5)),
            Spend(-20m, new DateOnly(2026, 10, 15)),
            Spend(-500m, new DateOnly(2026, 10, 25)),
            Spend(-60m, November)
        ];

        List<Insight> insights = InsightRules.FindUnusualPurchases(transactions, CurrentMonth, Today, 3m);

        Assert.AreEqual(1, insights.Count);
        Assert.AreEqual(20m, insights[0].Baseline);
    }

    [TestMethod]
    public void GetMedian_AveragesTheTwoMiddleAmountsOfAnEvenCount()
    {
        Assert.AreEqual(20m, InsightRules.GetMedian([10m, 20m, 500m]));
        Assert.AreEqual(25m, InsightRules.GetMedian([10m, 20m, 30m, 40m]));
        Assert.AreEqual(10.01m, InsightRules.GetMedian([10m, 10.01m]));
    }

    [TestMethod]
    public void FindUnusualPurchases_IgnoresTransferLegsUncategorizedAndMoneyIn()
    {
        List<InsightTransaction> transactions =
        [
            .. UsualPurchases(20m),
            Spend(-500m, November, transferId: Guid.NewGuid()),
            Spend(-500m, November, uncategorized: true),
            Spend(500m, November)
        ];

        Assert.AreEqual(0, InsightRules.FindUnusualPurchases(transactions, CurrentMonth, Today, 3m).Count);
    }

    [TestMethod]
    public void FindUnusualPurchases_KeepsCurrenciesAndCategoriesApart()
    {
        List<InsightTransaction> transactions =
        [
            .. UsualPurchases(20m),
            Spend(-500m, November, currency: "USD"),
            Spend(-500m, November, categoryId: RentCategoryId)
        ];

        Assert.AreEqual(0, InsightRules.FindUnusualPurchases(transactions, CurrentMonth, Today, 3m).Count);
    }

    [TestMethod]
    public void FindUnusualPurchases_EvaluatesOnlyPurchasesFromTheGivenDateToToday()
    {
        List<InsightTransaction> transactions =
        [
            .. UsualPurchases(20m),
            Spend(-500m, new DateOnly(2026, 10, 31)),
            Spend(-500m, Today.AddDays(1))
        ];

        Assert.AreEqual(0, InsightRules.FindUnusualPurchases(transactions, CurrentMonth, Today, 3m).Count);
        Assert.AreEqual(
            1,
            InsightRules.FindUnusualPurchases(transactions, new DateOnly(2026, 10, 1), Today, 3m).Count);
    }

    [TestMethod]
    public void FindUnusualPurchases_OrdersFromHighestToLowestAmount()
    {
        InsightTransaction smaller = Spend(-70m, new DateOnly(2026, 11, 2));
        InsightTransaction larger = Spend(-90m, new DateOnly(2026, 11, 3));
        List<InsightTransaction> transactions = [.. UsualPurchases(20m), smaller, larger];

        List<Insight> insights = InsightRules.FindUnusualPurchases(transactions, CurrentMonth, Today, 3m);

        Assert.AreEqual(2, insights.Count);
        Assert.AreEqual(larger.Id, insights[0].TransactionId);
        Assert.AreEqual(smaller.Id, insights[1].TransactionId);
    }

    /// <summary>
    /// Creates three food purchases of the same amount in October 2026,
    /// so the median before a November purchase equals the given amount.
    /// </summary>
    /// <param name="amount">The positive amount of each purchase.</param>
    /// <returns>The three earlier purchases.</returns>
    private static InsightTransaction[] UsualPurchases(decimal amount) =>
    [
        Spend(-amount, new DateOnly(2026, 10, 5)),
        Spend(-amount, new DateOnly(2026, 10, 15)),
        Spend(-amount, new DateOnly(2026, 10, 25))
    ];

    [TestMethod]
    public void ExcludeMuted_RemovesMutedAccountsAndCategories()
    {
        InsightTransaction kept = Spend(-10m, November);
        InsightTransaction uncategorized = Spend(-10m, November, uncategorized: true);
        List<InsightTransaction> transactions =
        [
            kept,
            uncategorized,
            Spend(-10m, November, categoryId: RentCategoryId),
            Spend(-10m, November) with { AccountId = VisaId }
        ];
        List<InsightMute> mutes =
        [
            new() { CategoryId = RentCategoryId },
            new() { AccountId = VisaId }
        ];

        List<InsightTransaction> result = InsightRules.ExcludeMuted(transactions, mutes);

        CollectionAssert.AreEqual(new[] { kept, uncategorized }, result);
    }

    [TestMethod]
    public void ExcludeMuted_KeepsEverythingWithoutMutes()
    {
        List<InsightTransaction> transactions = [Spend(-10m, November), Spend(-20m, October)];

        Assert.AreEqual(2, InsightRules.ExcludeMuted(transactions, []).Count);
    }

    [TestMethod]
    public void ResolveThreshold_UsesTheDefaultWithoutAPreferenceOrThreshold()
    {
        List<InsightPreference> preferences =
        [
            Preference(InsightTypes.CategorySpike, threshold: null),
            Preference(InsightTypes.UnusualPurchase, threshold: 5m)
        ];

        Assert.AreEqual(50m, InsightRules.ResolveThreshold([], InsightTypes.CategorySpike, 50m));
        Assert.AreEqual(50m, InsightRules.ResolveThreshold(preferences, InsightTypes.CategorySpike, 50m));
    }

    [TestMethod]
    public void ResolveThreshold_UsesTheThresholdOfTheMatchingType()
    {
        List<InsightPreference> preferences =
        [
            Preference(InsightTypes.CategorySpike, threshold: 80m),
            Preference(InsightTypes.UnusualPurchase, threshold: 5m)
        ];

        Assert.AreEqual(5m, InsightRules.ResolveThreshold(preferences, InsightTypes.UnusualPurchase, 3m));
    }

    [TestMethod]
    public void ResolveThreshold_ReturnsNullWhenTheTypeIsTurnedOff()
    {
        List<InsightPreference> preferences =
        [
            Preference(InsightTypes.CategorySpike, threshold: 80m, isEnabled: false)
        ];

        Assert.IsNull(InsightRules.ResolveThreshold(preferences, InsightTypes.CategorySpike, 50m));
        Assert.AreEqual(3m, InsightRules.ResolveThreshold(preferences, InsightTypes.UnusualPurchase, 3m));
    }

    [TestMethod]
    public void ApplyStates_ShowsAnInsightWithoutAStateAsUnread()
    {
        Insight insight = Spike(150m);

        List<Insight> result = InsightRules.ApplyStates([insight], []);

        Assert.AreEqual(1, result.Count);
        Assert.IsFalse(result[0].IsRead);
    }

    [TestMethod]
    public void ApplyStates_MarksAReadInsightAsRead()
    {
        Insight insight = Spike(150m);

        List<Insight> result = InsightRules.ApplyStates(
            [insight],
            [StateOf(insight, readAt: Moment(1))]);

        Assert.AreEqual(1, result.Count);
        Assert.IsTrue(result[0].IsRead);
    }

    [TestMethod]
    public void ApplyStates_LeavesOutADismissedInsight()
    {
        Insight unchanged = Spike(150m);
        Insight slightlyHigher = Spike(187.49m);

        Assert.AreEqual(
            0,
            InsightRules.ApplyStates(
                [unchanged],
                [StateOf(unchanged, dismissedAt: Moment(1), dismissedValue: 150m)]).Count);
        Assert.AreEqual(
            0,
            InsightRules.ApplyStates(
                [slightlyHigher],
                [StateOf(slightlyHigher, dismissedAt: Moment(1), dismissedValue: 150m)]).Count);
    }

    [TestMethod]
    public void ApplyStates_ShowsADismissedInsightAgainAsUnreadWhenMateriallyHigher()
    {
        Insight insight = Spike(187.50m);

        List<Insight> result = InsightRules.ApplyStates(
            [insight],
            [StateOf(insight, readAt: Moment(1), dismissedAt: Moment(2), dismissedValue: 150m)]);

        Assert.AreEqual(1, result.Count);
        Assert.IsFalse(result[0].IsRead);
    }

    [TestMethod]
    public void ApplyStates_MarksAnInsightReadAfterItCameBackAsRead()
    {
        Insight insight = Spike(187.50m);

        List<Insight> result = InsightRules.ApplyStates(
            [insight],
            [StateOf(insight, readAt: Moment(3), dismissedAt: Moment(2), dismissedValue: 150m)]);

        Assert.AreEqual(1, result.Count);
        Assert.IsTrue(result[0].IsRead);
    }

    [TestMethod]
    public void ApplyStates_MatchesAStateOnlyToItsOwnSubject()
    {
        Insight insight = Spike(150m);
        Insight purchase = new(
            InsightTypes.UnusualPurchase,
            InsightSeverities.Warning,
            null,
            Guid.NewGuid(),
            null,
            null,
            60m,
            20m);
        List<InsightState> states =
        [
            StateOf(insight with { CategoryId = RentCategoryId }, readAt: Moment(1)),
            StateOf(insight with { Currency = "USD" }, readAt: Moment(1)),
            StateOf(insight with { PeriodStart = new DateOnly(2026, 10, 1) }, readAt: Moment(1)),
            StateOf(purchase with { TransactionId = Guid.NewGuid() }, readAt: Moment(1))
        ];

        List<Insight> result = InsightRules.ApplyStates([insight, purchase], states);

        Assert.AreEqual(2, result.Count);
        Assert.IsFalse(result[0].IsRead);
        Assert.IsFalse(result[1].IsRead);
    }

    [TestMethod]
    public void ApplyStates_AppliesAStateToAnUnusualPurchase()
    {
        Insight purchase = new(
            InsightTypes.UnusualPurchase,
            InsightSeverities.Warning,
            null,
            Guid.NewGuid(),
            null,
            null,
            60m,
            20m);

        Assert.AreEqual(
            0,
            InsightRules.ApplyStates(
                [purchase],
                [StateOf(purchase, dismissedAt: Moment(1), dismissedValue: 60m)]).Count);
        Assert.IsTrue(
            InsightRules.ApplyStates(
                [purchase],
                [StateOf(purchase, readAt: Moment(1))])[0].IsRead);
    }

    [TestMethod]
    public void GetHistoryStart_CountsTheCurrentMonth()
    {
        Assert.AreEqual(new DateOnly(2026, 11, 1), InsightRules.GetHistoryStart(1, Today));
        Assert.AreEqual(new DateOnly(2026, 9, 1), InsightRules.GetHistoryStart(3, Today));
        Assert.AreEqual(new DateOnly(2025, 12, 1), InsightRules.GetHistoryStart(12, Today));
    }

    [TestMethod]
    public void FindInsights_ReturnsSpikesNewestMonthFirstThenPurchases()
    {
        InsightTransaction purchase = Spend(-500m, November, categoryId: RentCategoryId);
        List<InsightTransaction> transactions =
        [
            Spend(-100m, new DateOnly(2026, 7, 10)),
            Spend(-100m, August),
            Spend(-200m, September),
            Spend(-100m, October),
            Spend(-300m, November),
            Spend(-300m, September, categoryId: RentCategoryId),
            .. UsualPurchases(100m).Select(usual => usual with { CategoryId = RentCategoryId }),
            purchase
        ];

        List<Insight> insights = InsightRules.FindInsights(transactions, [], [], [], 3, Today);

        Assert.AreEqual(4, insights.Count);
        Assert.AreEqual(InsightTypes.CategorySpike, insights[0].InsightType);
        Assert.AreEqual(RentCategoryId, insights[0].CategoryId);
        Assert.AreEqual(CurrentMonth, insights[0].PeriodStart);
        Assert.AreEqual(InsightTypes.CategorySpike, insights[1].InsightType);
        Assert.AreEqual(FoodCategoryId, insights[1].CategoryId);
        Assert.AreEqual(CurrentMonth, insights[1].PeriodStart);
        Assert.AreEqual(InsightTypes.CategorySpike, insights[2].InsightType);
        Assert.AreEqual(new DateOnly(2026, 9, 1), insights[2].PeriodStart);
        Assert.AreEqual(InsightTypes.UnusualPurchase, insights[3].InsightType);
        Assert.AreEqual(purchase.Id, insights[3].TransactionId);
    }

    [TestMethod]
    public void FindInsights_EvaluatesOnlyTheGivenNumberOfMonths()
    {
        List<InsightTransaction> transactions =
        [
            Spend(-100m, new DateOnly(2026, 6, 10)),
            Spend(-100m, new DateOnly(2026, 7, 10)),
            Spend(-500m, August),
            Spend(-100m, September),
            Spend(-100m, October),
            Spend(-100m, November)
        ];

        Assert.AreEqual(0, InsightRules.FindInsights(transactions, [], [], [], 3, Today).Count);
        Assert.AreEqual(1, InsightRules.FindInsights(transactions, [], [], [], 4, Today).Count);
    }

    [TestMethod]
    public void FindInsights_SkipsAnInsightTypeThatIsTurnedOff()
    {
        List<InsightTransaction> transactions =
        [
            Spend(-60m, September),
            .. UsualPurchases(20m),
            Spend(-500m, November)
        ];

        List<Insight> all = InsightRules.FindInsights(transactions, [], [], [], 3, Today);
        List<Insight> withoutSpikes = InsightRules.FindInsights(
            transactions,
            [Preference(InsightTypes.CategorySpike, threshold: null, isEnabled: false)],
            [],
            [],
            3,
            Today);

        Assert.AreEqual(2, all.Count);
        Assert.AreEqual(1, withoutSpikes.Count);
        Assert.AreEqual(InsightTypes.UnusualPurchase, withoutSpikes[0].InsightType);
    }

    [TestMethod]
    public void FindInsights_UsesTheUsersThresholds()
    {
        List<InsightTransaction> transactions =
        [
            Spend(-60m, September),
            .. UsualPurchases(20m),
            Spend(-500m, November)
        ];

        List<Insight> insights = InsightRules.FindInsights(
            transactions,
            [
                Preference(InsightTypes.CategorySpike, threshold: 900m),
                Preference(InsightTypes.UnusualPurchase, threshold: 30m)
            ],
            [],
            [],
            3,
            Today);

        Assert.AreEqual(0, insights.Count);
    }

    [TestMethod]
    public void FindInsights_LeavesOutMutedCategoriesAndAccounts()
    {
        List<InsightTransaction> transactions =
        [
            Spend(-60m, September),
            .. UsualPurchases(20m),
            Spend(-500m, November)
        ];

        Assert.AreEqual(
            0,
            InsightRules.FindInsights(
                transactions,
                [],
                [new InsightMute { CategoryId = FoodCategoryId }],
                [],
                3,
                Today).Count);
        Assert.AreEqual(
            0,
            InsightRules.FindInsights(
                transactions,
                [],
                [new InsightMute { AccountId = ChequingId }],
                [],
                3,
                Today).Count);
    }

    [TestMethod]
    public void FindInsights_AppliesReadAndDismissedStates()
    {
        InsightTransaction purchase = Spend(-500m, November);
        List<InsightTransaction> transactions = [Spend(-60m, September), .. UsualPurchases(20m), purchase];
        List<InsightState> states =
        [
            new()
            {
                InsightType = InsightTypes.UnusualPurchase,
                TransactionId = purchase.Id,
                DismissedAt = Moment(1),
                DismissedValue = 500m
            },
            new()
            {
                InsightType = InsightTypes.CategorySpike,
                CategoryId = FoodCategoryId,
                Currency = "CAD",
                PeriodStart = CurrentMonth,
                ReadAt = Moment(1)
            }
        ];

        List<Insight> insights = InsightRules.FindInsights(transactions, [], [], states, 3, Today);

        Assert.AreEqual(1, insights.Count);
        Assert.AreEqual(InsightTypes.CategorySpike, insights[0].InsightType);
        Assert.IsTrue(insights[0].IsRead);
    }

    /// <summary>
    /// Creates a food category spike in CAD for November 2026 against a baseline of 100.
    /// </summary>
    /// <param name="amount">The amount the spike shows.</param>
    /// <returns>A new category spike insight.</returns>
    private static Insight Spike(decimal amount) =>
        new(
            InsightTypes.CategorySpike,
            InsightSeverities.Warning,
            FoodCategoryId,
            null,
            "CAD",
            CurrentMonth,
            amount,
            100m);

    /// <summary>
    /// Creates the stored state of an insight, copying its type and subject.
    /// </summary>
    /// <param name="insight">The insight the state belongs to.</param>
    /// <param name="readAt">When the insight was read, if it was.</param>
    /// <param name="dismissedAt">When the insight was dismissed, if it was.</param>
    /// <param name="dismissedValue">The amount the insight showed when it was dismissed.</param>
    /// <returns>A new insight state.</returns>
    private static InsightState StateOf(
        Insight insight,
        DateTime? readAt = null,
        DateTime? dismissedAt = null,
        decimal? dismissedValue = null) =>
        new()
        {
            InsightType = insight.InsightType,
            CategoryId = insight.CategoryId,
            TransactionId = insight.TransactionId,
            Currency = insight.Currency,
            PeriodStart = insight.PeriodStart,
            ReadAt = readAt,
            DismissedAt = dismissedAt,
            DismissedValue = dismissedValue
        };

    /// <summary>
    /// Creates a preference for an insight type.
    /// </summary>
    /// <param name="insightType">The insight type.</param>
    /// <param name="threshold">The user's threshold, if set.</param>
    /// <param name="isEnabled">Whether the insight type is turned on.</param>
    /// <returns>A new insight preference.</returns>
    private static InsightPreference Preference(
        string insightType,
        decimal? threshold,
        bool isEnabled = true) =>
        new()
        {
            InsightType = insightType,
            IsEnabled = isEnabled,
            Threshold = threshold
        };

    /// <summary>
    /// Creates a point in time on November 1, 2026; a higher hour is later.
    /// </summary>
    /// <param name="hour">The hour of the day.</param>
    /// <returns>The date and time in UTC.</returns>
    private static DateTime Moment(int hour) =>
        new(2026, 11, 1, hour, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Creates one transaction in each of the three months before November 2026,
    /// so the category's baseline equals the given amount.
    /// </summary>
    /// <param name="monthlyAmount">The positive amount spent in each month.</param>
    /// <param name="categoryId">The category; defaults to food.</param>
    /// <returns>The three baseline transactions.</returns>
    private static InsightTransaction[] BaselineOf(decimal monthlyAmount, Guid? categoryId = null) =>
    [
        Spend(-monthlyAmount, August, categoryId: categoryId),
        Spend(-monthlyAmount, September, categoryId: categoryId),
        Spend(-monthlyAmount, October, categoryId: categoryId)
    ];

    /// <summary>
    /// Creates a transaction on the chequing account.
    /// The category defaults to food and the currency to CAD.
    /// </summary>
    /// <param name="amount">The signed amount of the transaction.</param>
    /// <param name="transactionDate">The date of the transaction.</param>
    /// <param name="categoryId">The category of the transaction.</param>
    /// <param name="currency">The currency of the transaction.</param>
    /// <param name="transferId">The optional transfer the transaction belongs to.</param>
    /// <param name="uncategorized">Whether the transaction has no category.</param>
    /// <returns>A new transaction with the specified properties.</returns>
    private static InsightTransaction Spend(
        decimal amount,
        DateOnly transactionDate,
        Guid? categoryId = null,
        string currency = "CAD",
        Guid? transferId = null,
        bool uncategorized = false) =>
        new(
            Guid.NewGuid(),
            ChequingId,
            transferId,
            uncategorized ? null : categoryId ?? FoodCategoryId,
            transactionDate,
            amount,
            currency);
}

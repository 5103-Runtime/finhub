using FinHub.Api.Constants;
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
            Spend(-300m, September),
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

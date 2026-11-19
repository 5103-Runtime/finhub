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

using FinHub.Api.Controllers;
using FinHub.Api.DTOs.Overview;

namespace FinHub.Api.Tests.Controllers;

[TestClass]
public sealed class OverviewControllerTests
{
    private static readonly Guid ChequingId = Guid.NewGuid();
    private static readonly Guid SavingsId = Guid.NewGuid();
    private static readonly Guid FoodCategoryId = Guid.NewGuid();
    private static readonly Guid TransferId = Guid.NewGuid();

    [TestMethod]
    public void CreatePeriod_CreatesTheRequestedNumberOfCalendarMonths()
    {
        DateOnly today = new(2026, 11, 9);
        OverviewController.DashboardPeriod period = OverviewController.CreatePeriod(6, today);

        Assert.AreEqual(period.CurrentMonth.AddMonths(-5), period.PeriodStart);
        Assert.AreEqual(today, period.Today);
        Assert.AreEqual(1, period.CurrentMonth.Day);
        Assert.AreEqual(period.CurrentMonth.AddMonths(-1), period.PreviousMonthStart);
        Assert.AreEqual(period.CurrentMonth.AddDays(-1), period.PreviousMonthEnd);
    }

    [TestMethod]
    public void BuildCurrencySummaries_SeparatesIncomeSpendingAndNet()
    {
        List<OverviewController.DashboardTransaction> transactions =
        [
            Transaction("CAD", 1000m),
            Transaction("CAD", -250m),
            Transaction("USD", 500m)
        ];

        List<OverviewCurrencySummary> summaries = OverviewController.BuildCurrencySummaries(
            ["CAD", "USD"],
            transactions,
            _ => true);

        Assert.AreEqual(new OverviewCurrencySummary("CAD", 1000m, 250m, 750m), summaries[0]);
        Assert.AreEqual(new OverviewCurrencySummary("USD", 500m, 0m, 500m), summaries[1]);
    }

    [TestMethod]
    public void BuildCurrencySummaries_ExcludesTransferLegs()
    {
        List<OverviewController.DashboardTransaction> transactions =
        [
            Transaction("CAD", 100m),
            Transaction("CAD", -100m, transferId: TransferId)
        ];

        List<OverviewCurrencySummary> summaries = OverviewController.BuildCurrencySummaries(
            ["CAD"],
            transactions,
            _ => true);

        Assert.AreEqual(new OverviewCurrencySummary("CAD", 100m, 0m, 100m), summaries[0]);
    }

    [TestMethod]
    public void BuildAccountBalances_OrdersDatabaseBalancesByAbsoluteBalanceThenName()
    {
        List<OverviewController.DashboardAccount> accounts =
        [
            new(ChequingId, "Chequing", "chequing", "CAD"),
            new(SavingsId, "Savings", "savings", "CAD")
        ];
        Dictionary<Guid, decimal> balancesByAccountId =
            new()
            {
                [ChequingId] = -1200m,
                [SavingsId] = 1300m
            };

        List<OverviewAccountBalance> balances = OverviewController.BuildAccountBalances(
            accounts,
            balancesByAccountId);

        Assert.AreEqual(SavingsId, balances[0].AccountId);
        Assert.AreEqual(1300m, balances[0].Balance);
        Assert.AreEqual(ChequingId, balances[1].AccountId);
        Assert.AreEqual(-1200m, balances[1].Balance);
    }

    [TestMethod]
    public void BuildMonthlyTrend_CreatesOnePointPerMonthAndCurrency()
    {
        OverviewController.DashboardPeriod period = OverviewController.CreatePeriod(
            3,
            new DateOnly(2026, 11, 9));
        List<OverviewController.DashboardTransaction> transactions =
        [
            Transaction("CAD", 100m, transactionDate: period.PeriodStart),
            Transaction("CAD", -25m, transactionDate: period.CurrentMonth),
            Transaction("CAD", 500m, transactionDate: period.Today.AddDays(1))
        ];

        List<OverviewMonthlyPoint> trend = OverviewController.BuildMonthlyTrend(
            ["CAD"],
            transactions,
            period,
            3);

        Assert.AreEqual(3, trend.Count);
        Assert.AreEqual(period.PeriodStart, trend[0].Month);
        Assert.AreEqual(100m, trend[0].Income);
        Assert.AreEqual(0m, trend[0].Spending);
        Assert.AreEqual(100m, trend[0].Net);
        Assert.AreEqual(period.CurrentMonth, trend[2].Month);
        Assert.AreEqual(25m, trend[2].Spending);
        Assert.AreEqual(-25m, trend[2].Net);
    }

    [TestMethod]
    public void BuildCategorySpending_GroupsNegativeTransactionsAndLabelsMissingCategories()
    {
        OverviewController.DashboardPeriod period = OverviewController.CreatePeriod(
            3,
            new DateOnly(2026, 11, 9));
        List<OverviewController.DashboardTransaction> transactions =
        [
            Transaction("CAD", -40m, categoryId: FoodCategoryId, transactionDate: period.PeriodStart),
            Transaction("CAD", -15m, categoryId: FoodCategoryId, transactionDate: period.CurrentMonth),
            Transaction("CAD", -20m, transactionDate: period.CurrentMonth),
            Transaction("CAD", 100m, categoryId: FoodCategoryId, transactionDate: period.CurrentMonth)
        ];

        List<OverviewCategorySpending> spending = OverviewController.BuildCategorySpending(
            transactions,
            new Dictionary<Guid, string> { [FoodCategoryId] = "Food" },
            period);

        Assert.AreEqual(2, spending.Count);
        Assert.AreEqual("Food", spending[0].CategoryName);
        Assert.AreEqual(55m, spending[0].Amount);
        Assert.AreEqual("uncategorized", spending[1].CategoryId);
        Assert.AreEqual("Uncategorized", spending[1].CategoryName);
        Assert.AreEqual(20m, spending[1].Amount);
    }

    /// <summary>
    /// Creates a dashboard transaction with the specified properties.
    /// The account and category IDs default to the chequing account and no category, 
    /// and the transaction date defaults to January 15, 2026.
    /// </summary>
    /// <param name="currency">The currency of the transaction.</param>
    /// <param name="amount">The amount of the transaction.</param>
    /// <param name="accountId">The ID of the account associated with the transaction.</param>
    /// <param name="transferId">The optional transfer ID associated with the transaction.</param>
    /// <param name="categoryId">The ID of the category associated with the transaction.</param>
    /// <param name="transactionDate">The date of the transaction.</param>
    /// <returns>A new dashboard transaction with the specified properties.</returns>
    private static OverviewController.DashboardTransaction Transaction(
        string currency,
        decimal amount,
        Guid? accountId = null,
        Guid? transferId = null,
        Guid? categoryId = null,
        DateOnly? transactionDate = null) =>
        new(
            accountId ?? ChequingId,
            transferId,
            categoryId,
            transactionDate ?? new DateOnly(2026, 1, 15),
            amount,
            currency);
}

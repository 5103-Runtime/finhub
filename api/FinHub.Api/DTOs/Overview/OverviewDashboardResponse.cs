namespace FinHub.Api.DTOs.Overview;

/// <summary>
/// Aggregated data used by the authenticated user's financial dashboard.
/// Monetary values are never converted between currencies; each summary is
/// explicitly tagged with the account currency that produced it.
/// </summary>
/// <param name="PeriodStart">The first date included in the requested trend period.</param>
/// <param name="PeriodEnd">The last date included in the requested trend period.</param>
/// <param name="Months">The number of calendar months included in the trend.</param>
/// <param name="Currencies">The currencies used by the user's accounts.</param>
/// <param name="Accounts">The calculated balance for each user-owned account.</param>
/// <param name="Balances">Cumulative income, spending, and net movement through the current date.</param>
/// <param name="BalancesBeforeCurrentMonth">Cumulative income, spending, and net movement through the end of the previous month.</param>
/// <param name="CurrentPeriod">Income, spending, and net movement from the start of the current month through the current date.</param>
/// <param name="PreviousPeriod">Income, spending, and net movement for the complete previous month.</param>
/// <param name="MonthlyTrend">Monthly income, spending, and net movement for each currency.</param>
/// <param name="CategorySpending">Spending grouped by category for the requested trend period.</param>
public sealed record OverviewDashboardResponse(
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    int Months,
    IReadOnlyList<string> Currencies,
    IReadOnlyList<OverviewAccountBalance> Accounts,
    IReadOnlyList<OverviewCurrencySummary> Balances,
    IReadOnlyList<OverviewCurrencySummary> BalancesBeforeCurrentMonth,
    IReadOnlyList<OverviewCurrencySummary> CurrentPeriod,
    IReadOnlyList<OverviewCurrencySummary> PreviousPeriod,
    IReadOnlyList<OverviewMonthlyPoint> MonthlyTrend,
    IReadOnlyList<OverviewCategorySpending> CategorySpending);

/// <summary>
/// A calculated balance for one owned account.
/// </summary>
/// <param name="AccountId">The ID of the account.</param>
/// <param name="Name">The display name of the account.</param>
/// <param name="AccountType">The type of the account.</param>
/// <param name="Currency">The currency of the account.</param>
/// <param name="Balance">The signed sum of all transactions in the account.</param>
public sealed record OverviewAccountBalance(
    Guid AccountId,
    string Name,
    string AccountType,
    string Currency,
    decimal Balance);

/// <summary>
/// Income, spending, and net movement for one currency.
/// Spending is represented as a positive display value.
/// </summary>
/// <param name="Currency">The currency represented by the summary.</param>
/// <param name="Income">The sum of positive transaction amounts.</param>
/// <param name="Spending">The absolute sum of negative transaction amounts.</param>
/// <param name="Net">Income minus spending.</param>
public sealed record OverviewCurrencySummary(
    string Currency,
    decimal Income,
    decimal Spending,
    decimal Net);

/// <summary>
/// One month in the cash-flow trend.
/// </summary>
/// <param name="Month">The first date of the month.</param>
/// <param name="Label">The display label for the month.</param>
/// <param name="Currency">The currency represented by the point.</param>
/// <param name="Income">The income during the month.</param>
/// <param name="Spending">The spending during the month.</param>
/// <param name="Net">Income minus spending during the month.</param>
public sealed record OverviewMonthlyPoint(
    DateOnly Month,
    string Label,
    string Currency,
    decimal Income,
    decimal Spending,
    decimal Net);

/// <summary>
/// Spending grouped by category for the selected dashboard period.
/// </summary>
/// <param name="CategoryId">The category ID, or uncategorized when no category exists.</param>
/// <param name="CategoryName">The display name of the category.</param>
/// <param name="Currency">The currency represented by the spending amount.</param>
/// <param name="Amount">The absolute spending amount.</param>
public sealed record OverviewCategorySpending(
    string CategoryId,
    string CategoryName,
    string Currency,
    decimal Amount);

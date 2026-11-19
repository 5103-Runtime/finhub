namespace FinHub.Api.Constants;

/// <summary>
/// Contains the insight types stored in the insight tables.
/// </summary>
public static class InsightTypes
{
    /// <summary>
    /// A category's spending this month is well above its recent monthly average.
    /// </summary>
    public const string CategorySpike = "category_spike";

    /// <summary>
    /// A single purchase is well above the usual purchase in its category.
    /// </summary>
    public const string UnusualPurchase = "unusual_purchase";
}

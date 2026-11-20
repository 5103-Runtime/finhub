namespace FinHub.Api.Constants;

/// <summary>
/// Contains the severities an insight can have. Severity is derived on each request and is not stored.
/// </summary>
public static class InsightSeverities
{
    /// <summary>
    /// The amount reached the user's threshold.
    /// </summary>
    public const string Warning = "warning";

    /// <summary>
    /// The amount reached double the user's threshold.
    /// </summary>
    public const string Critical = "critical";
}

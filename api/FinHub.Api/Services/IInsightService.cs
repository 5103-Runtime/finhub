namespace FinHub.Api.Services;

/// <summary>
/// Defines insight evaluation operations.
/// </summary>
public interface IInsightService
{
    /// <summary>
    /// Computes the insights to show a user from their transactions,
    /// preferences, mutes, and read and dismissed markers.
    /// </summary>
    /// <param name="userId">The owner of the transactions.</param>
    /// <param name="months">
    /// The number of calendar months to evaluate, ending with the current month;
    /// between <see cref="InsightService.MinimumMonths"/> and <see cref="InsightService.MaximumMonths"/>.
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// Category spikes from the newest month to the oldest, then unusual purchases,
    /// each from highest to lowest amount.
    /// </returns>
    Task<IReadOnlyList<Insight>> GetInsightsAsync(
        Guid userId,
        int months,
        CancellationToken cancellationToken);
}

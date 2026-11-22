using FinHub.Api.Constants;
using FinHub.Api.Data;
using FinHub.Api.DTOs.Insight;
using FinHub.Api.Models;
using FinHub.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FinHub.Api.Controllers;

/// <summary>
/// Provides direct authenticated alert-preference routes.
/// </summary>
/// <param name="dbContext">The database context.</param>
/// <param name="authClient">The authentication client.</param>
[ApiController]
[Route("api/preferences")]
public sealed class PreferencesController(
    FinHubDbContext dbContext,
    SupabaseAuthClient authClient) : AuthenticatedControllerBase(authClient)
{
    /// <summary>
    /// Gets the authenticated user's alert preferences.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The user's alert preferences.</returns>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<InsightPreferenceResponse>>> List(
        CancellationToken cancellationToken)
    {
        (Guid? userId, ActionResult? failure) = await ResolveCurrentUserAsync(cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        List<InsightPreferenceResponse> preferences = await dbContext.InsightPreferences
            .AsNoTracking()
            .Where(preference => preference.UserId == userId!.Value)
            .OrderBy(preference => preference.InsightType)
            .Select(preference => new InsightPreferenceResponse(
                preference.Id,
                preference.InsightType,
                preference.IsEnabled,
                preference.Threshold,
                preference.CreatedAt,
                preference.UpdatedAt))
            .ToListAsync(cancellationToken);

        return Ok(preferences);
    }

    /// <summary>
    /// Creates or updates one alert preference.
    /// </summary>
    /// <param name="insightType">The insight type to update.</param>
    /// <param name="request">The enabled state and optional threshold.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created or updated preference.</returns>
    [HttpPut("{insightType}")]
    public async Task<ActionResult<InsightPreferenceResponse>> Update(
        string insightType,
        [FromBody] UpdateInsightPreferenceRequest request,
        CancellationToken cancellationToken)
    {
        if (insightType is not (InsightTypes.CategorySpike or InsightTypes.UnusualPurchase))
        {
            return BadRequest(new
            {
                error = "The insight type must be category_spike or unusual_purchase.",
                errorCode = "invalid_insight_type"
            });
        }

        (Guid? userId, ActionResult? failure) = await ResolveCurrentUserAsync(cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        Guid currentUserId = userId!.Value;
        InsightPreference? preference = await dbContext.InsightPreferences
            .Where(row => row.UserId == currentUserId && row.InsightType == insightType)
            .SingleOrDefaultAsync(cancellationToken);

        if (preference is null)
        {
            preference = new InsightPreference
            {
                Id = Guid.NewGuid(),
                UserId = currentUserId,
                InsightType = insightType
            };
            dbContext.InsightPreferences.Add(preference);
        }

        preference.IsEnabled = request.IsEnabled ?? true;
        preference.Threshold = request.Threshold;
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            return Conflict(new
            {
                error = "The insight preference changed concurrently.",
                errorCode = "insight_preference_conflict"
            });
        }

        return Ok(new InsightPreferenceResponse(
            preference.Id,
            preference.InsightType,
            preference.IsEnabled,
            preference.Threshold,
            preference.CreatedAt,
            preference.UpdatedAt));
    }

    /// <summary>
    /// Determines whether the database exception is a unique constraint violation.
    /// </summary>
    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation
        };
}

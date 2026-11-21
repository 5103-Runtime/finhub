using FinHub.Api.Constants;
using FinHub.Api.Data;
using FinHub.Api.DTOs.Insight;
using FinHub.Api.Models;
using FinHub.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
    [HttpPost("{insightType}")]
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

        if (request.IsEnabled is null)
        {
            return BadRequest(new
            {
                error = "isEnabled is required.",
                errorCode = "invalid_insight_preference"
            });
        }

        if (request.Threshold is <= 0)
        {
            return BadRequest(new
            {
                error = "Threshold must be greater than zero.",
                errorCode = "invalid_insight_threshold"
            });
        }

        (Guid? userId, ActionResult? failure) = await ResolveCurrentUserAsync(cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        InsightPreference? preference = await dbContext.InsightPreferences
            .Where(row => row.UserId == userId!.Value && row.InsightType == insightType)
            .SingleOrDefaultAsync(cancellationToken);

        if (preference is null)
        {
            preference = new InsightPreference
            {
                Id = Guid.NewGuid(),
                UserId = userId!.Value,
                InsightType = insightType
            };
            dbContext.InsightPreferences.Add(preference);
        }

        preference.IsEnabled = request.IsEnabled.Value;
        preference.Threshold = request.Threshold;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new InsightPreferenceResponse(
            preference.Id,
            preference.InsightType,
            preference.IsEnabled,
            preference.Threshold,
            preference.CreatedAt,
            preference.UpdatedAt));
    }
}

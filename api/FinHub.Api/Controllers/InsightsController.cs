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
/// Provides authenticated alert, insight, and insight-preference operations.
/// </summary>
/// <param name="dbContext">The database context.</param>
/// <param name="authClient">The authentication client.</param>
/// <param name="insightService">The insight evaluation service.</param>
/// <param name="timeProvider">The provider used to determine state timestamps.</param>
[ApiController]
[Route("api/insights")]
[Route("api/alerts")]
public sealed class InsightsController(
    FinHubDbContext dbContext,
    SupabaseAuthClient authClient,
    IInsightService insightService,
    TimeProvider timeProvider) : AuthenticatedControllerBase(authClient)
{
    private const int DefaultMonths = InsightService.DefaultMonths;
    private const int MinimumMonths = InsightService.MinimumMonths;
    private const int MaximumMonths = InsightService.MaximumMonths;

    /// <summary>
    /// Gets the authenticated user's computed insights.
    /// </summary>
    /// <param name="months">The number of calendar months to evaluate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The visible computed insights.</returns>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<InsightAlertResponse>>> List(
        [FromQuery] int? months,
        CancellationToken cancellationToken) =>
        await GetInsightsAsync(months, includeOnlyUnread: false, cancellationToken);

    /// <summary>
    /// Gets unread alerts for the authenticated user.
    /// </summary>
    /// <param name="months">The number of calendar months to evaluate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The authenticated user's unread insights.</returns>
    [HttpGet("unread")]
    public async Task<ActionResult<IReadOnlyList<InsightAlertResponse>>> Unread(
        [FromQuery] int? months,
        CancellationToken cancellationToken) =>
        await GetInsightsAsync(months, includeOnlyUnread: true, cancellationToken);

    /// <summary>
    /// Gets the authenticated user's insight history.
    /// </summary>
    /// <param name="months">The number of calendar months to evaluate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The authenticated user's visible insight history.</returns>
    [HttpGet("history")]
    public async Task<ActionResult<IReadOnlyList<InsightAlertResponse>>> History(
        [FromQuery] int? months,
        CancellationToken cancellationToken) =>
        await GetInsightsAsync(months, includeOnlyUnread: false, cancellationToken);

    /// <summary>
    /// Marks one alert as read.
    /// </summary>
    /// <param name="request">The insight subject.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated alert state response.</returns>
    [HttpPost("read")]
    public async Task<ActionResult<InsightAlertResponse?>> MarkRead(
        [FromBody] UpdateInsightStateRequest request,
        CancellationToken cancellationToken)
    {
        return await UpdateStateAsync(request, isDismissed: false, cancellationToken);
    }

    /// <summary>
    /// Dismisses one alert.
    /// </summary>
    /// <param name="request">The insight subject.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated dismissed-alert response.</returns>
    [HttpPost("dismiss")]
    public async Task<ActionResult<InsightAlertResponse?>> Dismiss(
        [FromBody] UpdateInsightStateRequest request,
        CancellationToken cancellationToken)
    {
        return await UpdateStateAsync(request, isDismissed: true, cancellationToken);
    }

    /// <summary>
    /// Loads and validates the requested insight collection.
    /// </summary>
    /// <param name="months">The number of calendar months to evaluate.</param>
    /// <param name="includeOnlyUnread">Whether to exclude already-read insights.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The insight responses or an authentication, range, or service failure.</returns>
    private async Task<ActionResult<IReadOnlyList<InsightAlertResponse>>> GetInsightsAsync(
        int? months,
        bool includeOnlyUnread,
        CancellationToken cancellationToken)
    {
        (Guid? userId, ActionResult? failure) = await ResolveCurrentUserAsync(cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        int monthCount = months ?? DefaultMonths;
        if (monthCount is < MinimumMonths or > MaximumMonths)
        {
            return BadRequest(new
            {
                error = $"The months value must be between {MinimumMonths} and {MaximumMonths}.",
                errorCode = "invalid_month_range"
            });
        }

        IReadOnlyList<Insight> insights =
            await insightService.GetInsightsAsync(
                userId!.Value,
                monthCount,
                cancellationToken);
        if (includeOnlyUnread)
        {
            insights = insights.Where(insight => !insight.IsRead).ToList();
        }

        List<InsightAlertResponse> responses = insights
            .Select(ToAlertResponse)
            .ToList();
        return Ok(responses);
    }

    /// <summary>
    /// Creates or updates the authenticated user's read or dismissed insight state.
    /// </summary>
    /// <param name="request">The insight subject and state values.</param>
    /// <param name="isDismissed">Whether to record a dismissal instead of a read.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The state response or an authentication or validation failure.</returns>
    private async Task<ActionResult<InsightAlertResponse?>> UpdateStateAsync(
        UpdateInsightStateRequest request,
        bool isDismissed,
        CancellationToken cancellationToken)
    {
        (Guid? userId, ActionResult? failure) = await ResolveCurrentUserAsync(cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        ActionResult? validationFailure = ValidateStateRequest(request);
        if (validationFailure is not null)
        {
            return validationFailure;
        }

        Guid currentUserId = userId!.Value;
        IReadOnlyList<Insight> insights =
            await insightService.GetInsightsAsync(
                currentUserId,
                DefaultMonths,
                cancellationToken);
        Insight? insight = insights.SingleOrDefault(candidate =>
            candidate.InsightType == request.InsightType
            && candidate.CategoryId == request.CategoryId
            && candidate.TransactionId == request.TransactionId
            && (request.InsightType == InsightTypes.UnusualPurchase
                || (candidate.Currency == request.Currency
                    && candidate.PeriodStart == request.PeriodStart)));
        if (insight is null)
        {
            return NotFound(new
            {
                error = "The insight could not be found or is no longer active.",
                errorCode = "insight_not_found"
            });
        }

        string? stateCurrency =
            request.InsightType == InsightTypes.UnusualPurchase
                ? null
                : request.Currency;
        DateOnly? statePeriod =
            request.InsightType == InsightTypes.UnusualPurchase
                ? null
                : request.PeriodStart;

        InsightState? state = await dbContext.InsightStates
            .Where(row => row.UserId == currentUserId
                && row.InsightType == request.InsightType
                && row.CategoryId == request.CategoryId
                && row.TransactionId == request.TransactionId
                && row.Currency == stateCurrency
                && row.PeriodStart == statePeriod)
            .SingleOrDefaultAsync(cancellationToken);

        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        if (state is null)
        {
            state = new InsightState
            {
                Id = Guid.NewGuid(),
                UserId = currentUserId,
                InsightType = request.InsightType,
                CategoryId = request.CategoryId,
                TransactionId = request.TransactionId,
                Currency = stateCurrency,
                PeriodStart = statePeriod
            };
            dbContext.InsightStates.Add(state);
        }

        if (isDismissed)
        {
            state.DismissedAt = now;
            state.DismissedValue = insight.Amount;
        }
        else
        {
            state.ReadAt = now;
            state.DismissedAt = null;
            state.DismissedValue = null;
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            return Conflict(new
            {
                error = "The insight state changed concurrently.",
                errorCode = "insight_state_conflict"
            });
        }
        catch (PostgresException exception) when (
            exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return Conflict(new
            {
                error = "The insight state changed concurrently.",
                errorCode = "insight_state_conflict"
            });
        }

        Insight updatedInsight = insight with
        {
            IsRead = !isDismissed
        };
        return Ok(ToAlertResponse(updatedInsight));
    }

    /// <summary>
    /// Validates that the state request's subject fields match the insight type.
    /// </summary>
    /// <param name="request">The state request to validate.</param>
    /// <returns>A bad-request response when invalid; otherwise null.</returns>
    private ActionResult? ValidateStateRequest(UpdateInsightStateRequest request)
    {
        if (request.InsightType == InsightTypes.CategorySpike)
        {
            if (request.CategoryId is null
                || string.IsNullOrWhiteSpace(request.Currency)
                || request.PeriodStart is null
                || request.TransactionId is not null)
            {
                return BadRequest(new
                {
                    error = "A category-spike state requires categoryId, currency, and periodStart.",
                    errorCode = "invalid_insight_subject"
                });
            }
            if (request.PeriodStart is DateOnly periodStart
                && periodStart.Day != 1)
            {
                return BadRequest(new
                {
                    error = "The periodStart value must be the first day of a month.",
                    errorCode = "invalid_insight_period"
                });
            }
        }
        else if (request.InsightType == InsightTypes.UnusualPurchase)
        {
            if (request.TransactionId is null
                || request.CategoryId is not null
                || request.PeriodStart is not null)
            {
                return BadRequest(new
                {
                    error = "An unusual-purchase state requires transactionId only.",
                    errorCode = "invalid_insight_subject"
                });
            }
        }
        else
        {
            return BadRequest(new
            {
                error = "The insight type must be category_spike or unusual_purchase.",
                errorCode = "invalid_insight_type"
            });
        }

        return null;
    }

    /// <summary>
    /// Converts a computed insight to its API response representation.
    /// </summary>
    /// <param name="insight">The computed insight.</param>
    /// <returns>The alert response representation.</returns>
    private static InsightAlertResponse ToAlertResponse(Insight insight) => new(
        BuildId(insight),
        insight.InsightType,
        insight.Severity,
        insight.CategoryId,
        insight.TransactionId,
        insight.Currency,
        insight.PeriodStart,
        insight.Amount,
        insight.Baseline,
        insight.IsRead);

    /// <summary>
    /// Builds a stable alert ID from a computed insight subject.
    /// </summary>
    /// <param name="insight">The computed insight.</param>
    /// <returns>The stable insight subject ID.</returns>
    private static string BuildId(Insight insight) =>
        insight.InsightType == InsightTypes.CategorySpike
            ? $"category_spike:{insight.CategoryId}:{insight.Currency}:{insight.PeriodStart:yyyy-MM-dd}"
            : $"unusual_purchase:{insight.TransactionId}";

    /// <summary>
    /// Determines whether the database exception is a unique constraint violation.
    /// </summary>
    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation
        };
}

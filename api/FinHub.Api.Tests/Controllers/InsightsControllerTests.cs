using System.Net;
using System.Text;
using FinHub.Api.Constants;
using FinHub.Api.Controllers;
using FinHub.Api.Data;
using FinHub.Api.DTOs.Insight;
using FinHub.Api.Models;
using FinHub.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace FinHub.Api.Tests.Controllers;

/// <summary>
/// Tests the authenticated alert, history, and preference operations.
/// </summary>
[TestClass]
public sealed class InsightsControllerTests
{
    private static readonly Guid UserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid CategoryId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid TransactionId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    /// <summary>
    /// Verifies that unread alert requests use the unread filtering operation.
    /// </summary>
    [TestMethod]
    public async Task Unread_ReturnsOnlyUnreadInsights()
    {
        using FinHubDbContext dbContext = CreateDbContext();
        RecordingInsightService service = new(
        [
            new Insight(
                InsightTypes.CategorySpike,
                InsightSeverities.Warning,
                CategoryId,
                null,
                "CAD",
                new DateOnly(2026, 11, 1),
                150m,
                100m,
                IsRead: false),
            new Insight(
                InsightTypes.UnusualPurchase,
                InsightSeverities.Critical,
                null,
                Guid.NewGuid(),
                "CAD",
                null,
                300m,
                100m,
                IsRead: true)
        ]);
        InsightsController controller = CreateController(dbContext, service);

        ActionResult<IReadOnlyList<InsightAlertResponse>> result =
            await controller.Unread(6, CancellationToken.None);

        Assert.IsInstanceOfType<OkObjectResult>(result.Result);
        Assert.AreEqual(1, service.GetInsightsCallCount);
        IReadOnlyList<InsightAlertResponse>? alerts =
            (result.Result as OkObjectResult)?.Value as IReadOnlyList<InsightAlertResponse>;
        Assert.AreEqual(1, alerts?.Count);
        Assert.IsFalse(alerts?[0].IsRead);
    }

    /// <summary>
    /// Verifies that history requests use the history operation.
    /// </summary>
    [TestMethod]
    public async Task History_ReturnsVisibleInsightHistory()
    {
        using FinHubDbContext dbContext = CreateDbContext();
        RecordingInsightService service = new(
        [
            new Insight(
                InsightTypes.UnusualPurchase,
                InsightSeverities.Critical,
                null,
                TransactionId,
                null,
                null,
                300m,
                100m,
                IsRead: true)
        ]);
        InsightsController controller = CreateController(dbContext, service);

        ActionResult<IReadOnlyList<InsightAlertResponse>> result =
            await controller.History(6, CancellationToken.None);

        Assert.IsInstanceOfType<OkObjectResult>(result.Result);
        Assert.AreEqual(1, service.GetInsightsCallCount);
        IReadOnlyList<InsightAlertResponse>? alerts =
            (result.Result as OkObjectResult)?.Value as IReadOnlyList<InsightAlertResponse>;
        Assert.IsTrue(alerts?[0].IsRead);
    }

    /// <summary>
    /// Verifies that month counts outside the supported range are rejected.
    /// </summary>
    [TestMethod]
    [DataRow(2)]
    [DataRow(13)]
    public async Task List_RejectsMonthCountsOutsideTheSupportedRange(int months)
    {
        using FinHubDbContext dbContext = CreateDbContext();
        InsightsController controller = CreateController(dbContext, new RecordingInsightService([]));

        ActionResult<IReadOnlyList<InsightAlertResponse>> result =
            await controller.List(months, CancellationToken.None);

        Assert.IsInstanceOfType<BadRequestObjectResult>(result.Result);
    }

    /// <summary>
    /// Verifies that a user's preference is created when it does not exist.
    /// </summary>
    [TestMethod]
    public async Task UpdatePreference_CreatesPreferenceForAuthenticatedUser()
    {
        using FinHubDbContext dbContext = CreateDbContext();
        PreferencesController controller = CreatePreferencesController(dbContext);
        UpdateInsightPreferenceRequest request = new()
        {
            IsEnabled = true,
            Threshold = 75m
        };

        ActionResult<InsightPreferenceResponse> result =
            await controller.Update(
                InsightTypes.CategorySpike,
                request,
                CancellationToken.None);

        Assert.IsInstanceOfType<OkObjectResult>(result.Result);
        InsightPreference preference =
            dbContext.InsightPreferences.Single(row => row.UserId == UserId);
        Assert.AreEqual(InsightTypes.CategorySpike, preference.InsightType);
        Assert.IsTrue(preference.IsEnabled);
        Assert.AreEqual(75m, preference.Threshold);
    }

    /// <summary>
    /// Verifies that an existing preference is updated instead of duplicated.
    /// </summary>
    [TestMethod]
    public async Task UpdatePreference_UpdatesExistingPreference()
    {
        using FinHubDbContext dbContext = CreateDbContext();
        dbContext.InsightPreferences.Add(new InsightPreference
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            InsightType = InsightTypes.UnusualPurchase,
            IsEnabled = true,
            Threshold = 3m
        });
        await dbContext.SaveChangesAsync();
        PreferencesController controller = CreatePreferencesController(dbContext);

        ActionResult<InsightPreferenceResponse> result =
            await controller.Update(
                InsightTypes.UnusualPurchase,
                new UpdateInsightPreferenceRequest
                {
                    IsEnabled = false,
                    Threshold = null
                },
                CancellationToken.None);

        Assert.IsInstanceOfType<OkObjectResult>(result.Result);
        Assert.AreEqual(1, dbContext.InsightPreferences.Count());
        InsightPreference preference = dbContext.InsightPreferences.Single();
        Assert.IsFalse(preference.IsEnabled);
        Assert.IsNull(preference.Threshold);
    }

    /// <summary>
    /// Verifies that read state is persisted for a category-spike insight.
    /// </summary>
    [TestMethod]
    public async Task MarkRead_PersistsCategorySpikeReadState()
    {
        using FinHubDbContext dbContext = CreateDbContext();
        RecordingInsightService service = new(
        [
            new Insight(
                InsightTypes.CategorySpike,
                InsightSeverities.Warning,
                CategoryId,
                null,
                "CAD",
                new DateOnly(2026, 11, 1),
                150m,
                100m)
        ]);
        InsightsController controller = CreateController(dbContext, service);
        UpdateInsightStateRequest request = new()
        {
            InsightType = InsightTypes.CategorySpike,
            CategoryId = CategoryId,
            Currency = "CAD",
            PeriodStart = new DateOnly(2026, 11, 1)
        };

        ActionResult<InsightAlertResponse?> result =
            await controller.MarkRead(request, CancellationToken.None);

        Assert.IsInstanceOfType<OkObjectResult>(result.Result);
        InsightState state = dbContext.InsightStates.Single();
        Assert.AreEqual(UserId, state.UserId);
        Assert.AreEqual(CategoryId, state.CategoryId);
        Assert.IsNotNull(state.ReadAt);
        Assert.IsNull(state.DismissedAt);
        InsightAlertResponse? response =
            (result.Result as OkObjectResult)?.Value as InsightAlertResponse;
        Assert.AreEqual(150m, response?.Amount);
        Assert.AreEqual(100m, response?.Baseline);
        Assert.AreEqual(InsightSeverities.Warning, response?.Severity);
        Assert.IsTrue(response?.IsRead);
    }

    /// <summary>
    /// Verifies that state actions validate older history alerts within the supported window.
    /// </summary>
    [TestMethod]
    public async Task MarkRead_AcceptsAnAlertFromTheMaximumHistoryWindow()
    {
        using FinHubDbContext dbContext = CreateDbContext();
        RecordingInsightService service = new(
        [
            new Insight(
                InsightTypes.CategorySpike,
                InsightSeverities.Warning,
                CategoryId,
                null,
                "CAD",
                new DateOnly(2025, 12, 1),
                150m,
                100m)
        ]);
        InsightsController controller = CreateController(dbContext, service);
        UpdateInsightStateRequest request = new()
        {
            InsightType = InsightTypes.CategorySpike,
            CategoryId = CategoryId,
            Currency = "CAD",
            PeriodStart = new DateOnly(2025, 12, 1)
        };

        ActionResult<InsightAlertResponse?> result =
            await controller.MarkRead(request, CancellationToken.None);

        Assert.IsInstanceOfType<OkObjectResult>(result.Result);
        Assert.AreEqual(InsightService.MaximumMonths, service.LastRequestedMonths);
        Assert.AreEqual(1, dbContext.InsightStates.Count());
    }

    /// <summary>
    /// Verifies that dismiss state and its positive dismissed value are persisted.
    /// </summary>
    [TestMethod]
    public async Task Dismiss_PersistsDismissedValue()
    {
        using FinHubDbContext dbContext = CreateDbContext();
        RecordingInsightService service = new(
        [
            new Insight(
                InsightTypes.UnusualPurchase,
                InsightSeverities.Warning,
                null,
                TransactionId,
                "USD",
                null,
                300m,
                100m)
        ]);
        InsightsController controller = CreateController(dbContext, service);
        UpdateInsightStateRequest request = new()
        {
            InsightType = InsightTypes.UnusualPurchase,
            TransactionId = TransactionId,
            Currency = "USD"
        };

        ActionResult<InsightAlertResponse?> result =
            await controller.Dismiss(request, CancellationToken.None);

        Assert.IsInstanceOfType<OkObjectResult>(result.Result);
        InsightState state = dbContext.InsightStates.Single();
        Assert.AreEqual(TransactionId, state.TransactionId);
        Assert.IsNotNull(state.DismissedAt);
        Assert.IsNull(state.Currency);
        Assert.AreEqual(300m, state.DismissedValue);
        InsightAlertResponse? response =
            (result.Result as OkObjectResult)?.Value as InsightAlertResponse;
        Assert.AreEqual(300m, response?.Amount);
        Assert.AreEqual(100m, response?.Baseline);
        Assert.AreEqual(InsightSeverities.Warning, response?.Severity);
        Assert.IsFalse(response?.IsRead);
    }

    /// <summary>
    /// Verifies that marking a dismissed insight read clears the dismissal marker.
    /// </summary>
    [TestMethod]
    public async Task MarkRead_ClearsDismissalMarker()
    {
        using FinHubDbContext dbContext = CreateDbContext();
        dbContext.InsightStates.Add(new InsightState
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            InsightType = InsightTypes.UnusualPurchase,
            TransactionId = TransactionId,
            DismissedAt = new DateTime(2026, 11, 18, 11, 0, 0, DateTimeKind.Utc),
            DismissedValue = 300m
        });
        await dbContext.SaveChangesAsync();
        RecordingInsightService service = new(
        [
            new Insight(
                InsightTypes.UnusualPurchase,
                InsightSeverities.Warning,
                null,
                TransactionId,
                "USD",
                null,
                300m,
                100m)
        ]);
        InsightsController controller = CreateController(dbContext, service);
        UpdateInsightStateRequest request = new()
        {
            InsightType = InsightTypes.UnusualPurchase,
            TransactionId = TransactionId,
            Currency = "USD"
        };

        ActionResult<InsightAlertResponse?> result =
            await controller.MarkRead(request, CancellationToken.None);

        Assert.IsInstanceOfType<OkObjectResult>(result.Result);
        InsightState state = dbContext.InsightStates.Single();
        Assert.IsNotNull(state.ReadAt);
        Assert.IsNull(state.DismissedAt);
        Assert.IsNull(state.DismissedValue);
    }

    /// <summary>
    /// Verifies that dismissing a nonexistent insight is rejected.
    /// </summary>
    [TestMethod]
    public async Task Dismiss_RejectsMissingInsight()
    {
        using FinHubDbContext dbContext = CreateDbContext();
        InsightsController controller = CreateController(dbContext, new RecordingInsightService([]));
        UpdateInsightStateRequest request = new()
        {
            InsightType = InsightTypes.UnusualPurchase,
            TransactionId = TransactionId
        };

        ActionResult<InsightAlertResponse?> result =
            await controller.Dismiss(request, CancellationToken.None);

        Assert.IsInstanceOfType<NotFoundObjectResult>(result.Result);
        Assert.AreEqual(0, dbContext.InsightStates.Count());
    }

    /// <summary>
    /// Verifies that malformed insight subjects are rejected.
    /// </summary>
    [TestMethod]
    public async Task MarkRead_RejectsInvalidCategorySpikeSubject()
    {
        using FinHubDbContext dbContext = CreateDbContext();
        InsightsController controller = CreateController(dbContext, new RecordingInsightService([]));
        UpdateInsightStateRequest request = new()
        {
            InsightType = InsightTypes.CategorySpike,
            CategoryId = CategoryId
        };

        ActionResult<InsightAlertResponse?> result =
            await controller.MarkRead(request, CancellationToken.None);

        Assert.IsInstanceOfType<BadRequestObjectResult>(result.Result);
        Assert.AreEqual(0, dbContext.InsightStates.Count());
    }

    /// <summary>
    /// Verifies that category-spike period values must be the first day of a month.
    /// </summary>
    [TestMethod]
    public async Task MarkRead_RejectsInvalidPeriodStart()
    {
        using FinHubDbContext dbContext = CreateDbContext();
        InsightsController controller = CreateController(
            dbContext,
            new RecordingInsightService([]));
        UpdateInsightStateRequest request = new()
        {
            InsightType = InsightTypes.CategorySpike,
            CategoryId = CategoryId,
            Currency = "CAD",
            PeriodStart = new DateOnly(2026, 11, 2)
        };

        ActionResult<InsightAlertResponse?> result =
            await controller.MarkRead(request, CancellationToken.None);

        Assert.IsInstanceOfType<BadRequestObjectResult>(result.Result);
        Assert.AreEqual(0, dbContext.InsightStates.Count());
    }

    /// <summary>
    /// Creates an in-memory database context.
    /// </summary>
    /// <returns>An isolated FinHub database context.</returns>
    private static FinHubDbContext CreateDbContext()
    {
        DbContextOptions<FinHubDbContext> options =
            new DbContextOptionsBuilder<FinHubDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
        return new FinHubDbContext(options);
    }

    /// <summary>
    /// Creates an authenticated controller with a recording insight service.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="service">The recording insight service.</param>
    /// <returns>The configured insights controller.</returns>
    private static InsightsController CreateController(
        FinHubDbContext dbContext,
        RecordingInsightService service) =>
        new(
            dbContext,
            CreateAuthClient(),
            service,
            TimeProvider.System)
        {
            ControllerContext = CreateControllerContext()
        };

    /// <summary>
    /// Creates an authenticated preferences controller.
    /// </summary>
    private static PreferencesController CreatePreferencesController(
        FinHubDbContext dbContext) =>
        new(dbContext, CreateAuthClient())
        {
            ControllerContext = CreateControllerContext()
        };

    /// <summary>
    /// Creates a controller context carrying an access-token cookie.
    /// </summary>
    /// <returns>The authenticated controller context.</returns>
    private static ControllerContext CreateControllerContext()
    {
        DefaultHttpContext httpContext = new();
        httpContext.Request.Headers.Cookie =
            $"{AuthCookies.AccessToken}=test-access-token";
        return new ControllerContext { HttpContext = httpContext };
    }

    /// <summary>
    /// Creates an authentication client that resolves the test user.
    /// </summary>
    /// <returns>The configured Supabase authentication client.</returns>
    private static SupabaseAuthClient CreateAuthClient()
    {
        AuthHandler handler = new(UserId);
        HttpClient httpClient = new(handler);
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Supabase:Url"] = "https://supabase.test",
                ["Supabase:PublishableKey"] = "test-key"
            })
            .Build();
        return new SupabaseAuthClient(
            httpClient,
            configuration,
            NullLogger<SupabaseAuthClient>.Instance);
    }

    /// <summary>
    /// Supplies a successful Supabase user response for controller authentication.
    /// </summary>
    /// <param name="userId">The user ID returned by the auth handler.</param>
    private sealed class AuthHandler(Guid userId) : HttpMessageHandler
    {
        /// <inheritdoc />
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            HttpResponseMessage response = new(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    $$"""{"id":"{{userId}}"}""",
                    Encoding.UTF8,
                    "application/json")
            };
            return Task.FromResult(response);
        }
    }

    /// <summary>
    /// Records GetInsightsAsync calls while supplying test insights.
    /// </summary>
    /// <param name="insights">The insights returned by the service.</param>
    private sealed class RecordingInsightService(IReadOnlyList<Insight> insights)
        : IInsightService
    {
        /// <summary>
        /// Gets the number of GetInsightsAsync calls.
        /// </summary>
        public int GetInsightsCallCount { get; private set; }

        /// <summary>
        /// Gets the month count from the latest GetInsightsAsync call.
        /// </summary>
        public int? LastRequestedMonths { get; private set; }

        /// <inheritdoc />
        public Task<IReadOnlyList<Insight>> GetInsightsAsync(
            Guid userId,
            int months,
            CancellationToken cancellationToken)
        {
            GetInsightsCallCount++;
            LastRequestedMonths = months;
            return Task.FromResult(insights);
        }
    }
}

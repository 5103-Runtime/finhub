using FinHub.Api.Services;
using FinHub.Api.Data;
using FinHub.Api.Models;
using FinHub.Api.Constants;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace FinHub.Api.Tests.Services;

[TestClass]
public sealed class InsightServiceTests(TestContext testContext)
{
    private static readonly Guid UserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [TestMethod]
    public void Services_ResolveTheInsightService()
    {
        using WebApplicationFactory<Program> factory = new();
        using IServiceScope scope = factory.Services.CreateScope();

        IInsightService service = scope.ServiceProvider.GetRequiredService<IInsightService>();

        Assert.IsInstanceOfType<InsightService>(service);
    }

    [TestMethod]
    [DataRow(InsightService.MinimumMonths - 1)]
    [DataRow(InsightService.MaximumMonths + 1)]
    public async Task GetInsightsAsync_RejectsAMonthCountOutsideTheRange(int months)
    {
        using WebApplicationFactory<Program> factory = new();
        using IServiceScope scope = factory.Services.CreateScope();
        IInsightService service = scope.ServiceProvider.GetRequiredService<IInsightService>();

        await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(
            () => service.GetInsightsAsync(Guid.NewGuid(), months, testContext.CancellationToken));
    }

    /// <summary>
    /// Verifies that GetInsightsAsync excludes unread filtering and returns computed markers.
    /// </summary>
    [TestMethod]
    public async Task GetInsightsAsync_AppliesComputedInsightState()
    {
        using FinHubDbContext dbContext = CreateDbContext();
        TimeProvider timeProvider = new FixedTimeProvider(
            new DateTimeOffset(2026, 11, 18, 12, 0, 0, TimeSpan.Zero));
        InsightService service = new(
            dbContext,
            timeProvider,
            TimeZoneInfo.Utc);
        Guid categoryId = Guid.NewGuid();
        Guid transactionId = Guid.NewGuid();
        dbContext.InsightStates.AddRange(
            new InsightState
            {
                Id = Guid.NewGuid(),
                UserId = UserId,
                InsightType = InsightTypes.CategorySpike,
                CategoryId = categoryId,
                Currency = "CAD",
                PeriodStart = new DateOnly(2026, 11, 1),
                ReadAt = new DateTime(2026, 11, 18, 11, 0, 0, DateTimeKind.Utc)
            },
            new InsightState
            {
                Id = Guid.NewGuid(),
                UserId = UserId,
                InsightType = InsightTypes.UnusualPurchase,
                TransactionId = transactionId,
                DismissedAt = new DateTime(2026, 11, 18, 11, 0, 0, DateTimeKind.Utc),
                DismissedValue = 100m
            });
        await dbContext.SaveChangesAsync(testContext.CancellationToken);

        IReadOnlyList<Insight> insights =
            await service.GetInsightsAsync(UserId, 6, testContext.CancellationToken);

        Assert.AreEqual(0, insights.Count);
    }

    /// <summary>
    /// Verifies that GetInsightsAsync returns an empty computed list for a user with no transactions.
    /// </summary>
    [TestMethod]
    public async Task GetInsightsAsync_ReturnsComputedVisibleInsights()
    {
        using FinHubDbContext dbContext = CreateDbContext();
        TimeProvider timeProvider = new FixedTimeProvider(
            new DateTimeOffset(2026, 11, 18, 12, 0, 0, TimeSpan.Zero));
        InsightService service = new(
            dbContext,
            timeProvider,
            TimeZoneInfo.Utc);

        IReadOnlyList<Insight> history =
            await service.GetInsightsAsync(UserId, 6, testContext.CancellationToken);

        Assert.AreEqual(0, history.Count);
    }

    /// <summary>
    /// Creates an isolated in-memory database context.
    /// </summary>
    /// <returns>The database context.</returns>
    private static FinHubDbContext CreateDbContext()
    {
        DbContextOptions<FinHubDbContext> options =
            new DbContextOptionsBuilder<FinHubDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
        return new FinHubDbContext(options);
    }

    /// <summary>
    /// Supplies a fixed current time for insight service tests.
    /// </summary>
    /// <param name="utcNow">The fixed UTC time.</param>
    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        /// <inheritdoc />
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}

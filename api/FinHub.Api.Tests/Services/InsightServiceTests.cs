using FinHub.Api.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace FinHub.Api.Tests.Services;

[TestClass]
public sealed class InsightServiceTests(TestContext testContext)
{
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
}

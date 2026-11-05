using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace FinHub.Api.Tests;

[TestClass]
public sealed class AuthRateLimitTests(TestContext testContext)
{
    [TestMethod]
    public async Task Login_RejectsTheCallAfterTheConfiguredLimit()
    {
        using WebApplicationFactory<Program> factory = WithSettings(("PermitLimit", "1"), ("WindowSeconds", "7"));
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage first = await client.PostAsJsonAsync("/api/auth/login", new { }, testContext.CancellationToken);
        using HttpResponseMessage second = await client.PostAsJsonAsync("/api/auth/login", new { }, testContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.BadRequest, first.StatusCode);
        Assert.AreEqual(HttpStatusCode.TooManyRequests, second.StatusCode);
        Assert.AreEqual(TimeSpan.FromSeconds(7), second.Headers.RetryAfter?.Delta);
    }

    [TestMethod]
    public void AppSettings_ShipTenPerMinute()
    {
        IConfigurationSection auth = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"))
            .Build()
            .GetSection("RateLimiting:Auth");

        Assert.AreEqual(10, auth.GetValue<int>("PermitLimit"));
        Assert.AreEqual(60, auth.GetValue<int>("WindowSeconds"));
    }

    [TestMethod]
    [DataRow("PermitLimit", null)]
    [DataRow("PermitLimit", "0")]
    [DataRow("PermitLimit", "-1")]
    [DataRow("WindowSeconds", null)]
    [DataRow("WindowSeconds", "0")]
    [DataRow("WindowSeconds", "-1")]
    public void Startup_FailsOnAMissingOrNonPositiveValue(string key, string? value)
    {
        using WebApplicationFactory<Program> factory = WithSettings((key, value));

        InvalidOperationException ex = Assert.ThrowsExactly<InvalidOperationException>(factory.CreateClient);
        Assert.Contains($"RateLimiting:Auth:{key}", ex.Message);
    }

    private static WebApplicationFactory<Program> WithSettings(params (string Key, string? Value)[] settings) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            foreach ((string key, string? value) in settings)
            {
                builder.UseSetting($"RateLimiting:Auth:{key}", value);
            }
        });
}

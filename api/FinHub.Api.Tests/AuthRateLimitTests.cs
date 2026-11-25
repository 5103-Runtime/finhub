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
    public void AppSettings_ShipConfiguredRateLimits()
    {
        IConfigurationSection auth = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"))
            .Build()
            .GetSection("RateLimiting:Auth");

        Assert.AreEqual(10, auth.GetValue<int>("PermitLimit"));
        Assert.AreEqual(60, auth.GetValue<int>("WindowSeconds"));

        IConfigurationSection guestPreview = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"))
            .Build()
            .GetSection("RateLimiting:GuestPreview");

        Assert.AreEqual(1, guestPreview.GetValue<int>("PermitLimit"));
        Assert.AreEqual(60, guestPreview.GetValue<int>("WindowSeconds"));
    }

    [TestMethod]
    [DataRow("PermitLimit", null)]
    [DataRow("PermitLimit", "0")]
    [DataRow("PermitLimit", "-1")]
    [DataRow("WindowSeconds", null)]
    [DataRow("WindowSeconds", "0")]
    [DataRow("WindowSeconds", "-1")]
    [DataRow("GuestPreview:PermitLimit", null)]
    [DataRow("GuestPreview:PermitLimit", "0")]
    [DataRow("GuestPreview:PermitLimit", "-1")]
    [DataRow("GuestPreview:WindowSeconds", null)]
    [DataRow("GuestPreview:WindowSeconds", "0")]
    [DataRow("GuestPreview:WindowSeconds", "-1")]
    public void Startup_FailsOnAMissingOrNonPositiveValue(string key, string? value)
    {
        using WebApplicationFactory<Program> factory = WithSettings((key, value));

        InvalidOperationException ex = Assert.ThrowsExactly<InvalidOperationException>(factory.CreateClient);
        string settingName = key.StartsWith("GuestPreview:", StringComparison.Ordinal)
            ? $"RateLimiting:{key}"
            : $"RateLimiting:Auth:{key}";
        Assert.Contains(settingName, ex.Message);
    }

    private static WebApplicationFactory<Program> WithSettings(params (string Key, string? Value)[] settings) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            foreach ((string key, string? value) in settings)
            {
                string settingName = key.StartsWith("GuestPreview:", StringComparison.Ordinal)
                    ? $"RateLimiting:{key}"
                    : $"RateLimiting:Auth:{key}";
                builder.UseSetting(settingName, value);
            }
        });
}

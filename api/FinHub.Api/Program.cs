using System.Threading.RateLimiting;
using FinHub.Api.Data;
using FinHub.Api.Statements;
using FinHub.Api.Statements.Csv;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddHttpClient<FinHub.Api.Services.SupabaseAuthClient>();
builder.Services.AddDbContext<FinHubDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Supabase")));

builder.Services.AddSingleton(new StatementMappingCatalog()); // Built now so a bad mapping file fails startup.
builder.Services.AddSingleton<IStatementParser, CsvStatementParser>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    // Keyed on the TCP peer, which is the proxy once the project is hosted.
    // Before deploying, enable forwarded headers or every user will share one bucket
    // (ASPNETCORE_FORWARDEDHEADERS_ENABLED=true on Azure App Service,
    // or UseForwardedHeaders with the proxy in KnownProxies before UseRateLimiter).
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1)
        }));
    options.OnRejected = async (context, cancellationToken) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString();
        }
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "Too many attempts. Please try again in a minute.",
            errorCode = "rate_limited"
        }, cancellationToken);
    };
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseRateLimiter();

app.UseAuthorization();

app.MapControllers();

app.Run();

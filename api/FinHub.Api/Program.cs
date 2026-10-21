using System.Threading.RateLimiting;
using FinHub.Api.Data;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddHttpClient<FinHub.Api.Services.SupabaseAuthClient>();
builder.Services.AddDbContext<FinHubDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Supabase")));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1)
        }));
    options.OnRejected = async (context, cancellationToken) =>
    {
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

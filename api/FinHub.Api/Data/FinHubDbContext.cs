using Microsoft.EntityFrameworkCore;

namespace FinHub.Api.Data;

/// <summary>
/// Represents the Entity Framework Core database context for the FinHub application.
/// </summary>
/// <param name="options">The options for the database context.</param>
public sealed class FinHubDbContext(DbContextOptions<FinHubDbContext> options)
    : DbContext(options)
{
}

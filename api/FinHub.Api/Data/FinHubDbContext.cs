using Microsoft.EntityFrameworkCore;

namespace FinHub.Api.Data;

public sealed class FinHubDbContext(DbContextOptions<FinHubDbContext> options)
    : DbContext(options)
{
}

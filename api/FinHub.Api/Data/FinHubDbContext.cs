using Microsoft.EntityFrameworkCore;
using FinHub.Api.Models;

namespace FinHub.Api.Data;

/// <summary>
/// Represents the Entity Framework Core database context for the FinHub application.
/// </summary>
/// <param name="options">The options for the database context.</param>
public sealed class FinHubDbContext(DbContextOptions<FinHubDbContext> options)
    : DbContext(options)
{
    /// <summary>
    /// Gets the DbSet of user profiles in the database.
    /// </summary>
    public DbSet<Profile> Profiles => Set<Profile>();

    /// <summary>
    /// Configures the model for the FinHub database context, including entity mappings and relationships.
    /// </summary>
    /// <param name="modelBuilder">The ModelBuilder instance used to configure the model.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Profile>(entity =>
        {
            entity.ToTable("profiles", "public");
            entity.HasKey(profile => profile.Id);
            entity.Property(profile => profile.Id).HasColumnName("id");
            entity.Property(profile => profile.FirstName).HasColumnName("first_name").HasMaxLength(100);
            entity.Property(profile => profile.LastName).HasColumnName("last_name").HasMaxLength(100);
            entity.Property(profile => profile.PreferredName).HasColumnName("preferred_name").HasMaxLength(100);
            entity.Property(profile => profile.DateOfBirth).HasColumnName("date_of_birth");
            entity.Property(profile => profile.CreatedAt).HasColumnName("created_at");
            entity.Property(profile => profile.UpdatedAt).HasColumnName("updated_at");
        });
    }
}

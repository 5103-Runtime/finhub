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
    /// Gets the DbSet of financial accounts in the database.
    /// </summary>
    public DbSet<FinancialAccount> FinancialAccounts => Set<FinancialAccount>();

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

        modelBuilder.Entity<FinancialAccount>(entity =>
        {
            entity.ToTable("financial_accounts", "public");
            entity.HasKey(account => account.Id);
            entity.Property(account => account.Id).HasColumnName("id");
            entity.Property(account => account.UserId).HasColumnName("user_id");
            entity.Property(account => account.Name).HasColumnName("name").HasMaxLength(100);
            entity.Property(account => account.Institution).HasColumnName("institution").HasMaxLength(100);
            entity.Property(account => account.AccountType).HasColumnName("account_type");
            entity.Property(account => account.Currency).HasColumnName("currency");
            entity.Property(account => account.CreatedAt).HasColumnName("created_at");
            entity.Property(account => account.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(account => account.UserId);
        });
    }
}

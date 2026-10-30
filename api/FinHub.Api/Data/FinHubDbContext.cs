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
    /// Gets the DbSet of transactions in the database.
    /// </summary>
    public DbSet<Transaction> Transactions => Set<Transaction>();

    /// <summary>
    /// Gets the DbSet of system and user-owned categories.
    /// </summary>
    public DbSet<Category> Categories => Set<Category>();

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
            entity.Property(profile => profile.CreatedAt).HasColumnName("created_at").ValueGeneratedOnAdd();
            entity.Property(profile => profile.UpdatedAt).HasColumnName("updated_at").ValueGeneratedOnAddOrUpdate();
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
            entity.Property(account => account.CreatedAt).HasColumnName("created_at").ValueGeneratedOnAdd();
            entity.Property(account => account.UpdatedAt).HasColumnName("updated_at").ValueGeneratedOnAddOrUpdate();
            entity.HasIndex(account => account.UserId);
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.ToTable("transactions", "public");
            entity.HasKey(transaction => transaction.Id);
            entity.Property(transaction => transaction.Id).HasColumnName("id");
            entity.Property(transaction => transaction.AccountId).HasColumnName("account_id");
            entity.Property(transaction => transaction.CategoryId).HasColumnName("category_id");
            entity.Property(transaction => transaction.TransactionDate).HasColumnName("transaction_date");
            entity.Property(transaction => transaction.Description).HasColumnName("description").HasMaxLength(500);
            entity.Property(transaction => transaction.Amount).HasColumnName("amount").HasPrecision(14, 2);
            entity.Property(transaction => transaction.BalanceAfter).HasColumnName("balance_after").HasPrecision(14, 2);
            entity.Property(transaction => transaction.StatementLine).HasColumnName("statement_line");
            entity.Property(transaction => transaction.CreatedAt).HasColumnName("created_at").ValueGeneratedOnAdd();
            entity.Property(transaction => transaction.UpdatedAt).HasColumnName("updated_at").ValueGeneratedOnAddOrUpdate();
            entity.HasIndex(transaction => transaction.AccountId);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("categories", "public");
            entity.HasKey(category => category.Id);
            entity.Property(category => category.Id).HasColumnName("id");
            entity.Property(category => category.UserId).HasColumnName("user_id");
            entity.Property(category => category.Name).HasColumnName("name").HasMaxLength(50);
            entity.Property(category => category.IsSystem).HasColumnName("is_system");
            entity.Property(category => category.CreatedAt).HasColumnName("created_at").ValueGeneratedOnAdd();
            entity.Property(category => category.UpdatedAt).HasColumnName("updated_at").ValueGeneratedOnAddOrUpdate();
            entity.HasIndex(category => category.UserId);
        });
    }
}

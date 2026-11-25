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
    /// Gets the DbSet of internal account transfers.
    /// </summary>
    public DbSet<Transfer> Transfers => Set<Transfer>();

    /// <summary>
    /// Gets the DbSet of system and user-owned categories.
    /// </summary>
    public DbSet<Category> Categories => Set<Category>();

    /// <summary>
    /// Gets the categorization rules owned by users.
    /// </summary>
    public DbSet<CategorizationRule> CategorizationRules => Set<CategorizationRule>();

    /// <summary>
    /// Gets the per-user settings for each insight type.
    /// </summary>
    public DbSet<InsightPreference> InsightPreferences => Set<InsightPreference>();

    /// <summary>
    /// Gets the read and dismissed markers for insights.
    /// </summary>
    public DbSet<InsightState> InsightStates => Set<InsightState>();

    /// <summary>
    /// Gets the categories and accounts users have excluded from insights.
    /// </summary>
    public DbSet<InsightMute> InsightMutes => Set<InsightMute>();

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
            entity.Property(profile => profile.Role).HasColumnName("role").HasMaxLength(20);
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
            entity.Property(transaction => transaction.TransferId).HasColumnName("transfer_id");
            entity.Property(transaction => transaction.CategoryId).HasColumnName("category_id");
            entity.Property(transaction => transaction.TransactionDate).HasColumnName("transaction_date");
            entity.Property(transaction => transaction.Description).HasColumnName("description").HasMaxLength(500);
            entity.Property(transaction => transaction.Amount).HasColumnName("amount").HasPrecision(14, 2);
            entity.Property(transaction => transaction.BalanceAfter).HasColumnName("balance_after").HasPrecision(14, 2);
            entity.Property(transaction => transaction.StatementLine).HasColumnName("statement_line");
            entity.Property(transaction => transaction.CreatedAt).HasColumnName("created_at").ValueGeneratedOnAdd();
            entity.Property(transaction => transaction.UpdatedAt).HasColumnName("updated_at").ValueGeneratedOnAddOrUpdate();
            entity.HasIndex(transaction => transaction.AccountId);
            entity.HasIndex(transaction => transaction.TransferId);
            entity.HasOne<Transfer>()
                .WithMany()
                .HasForeignKey(transaction => transaction.TransferId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Transfer>(entity =>
        {
            entity.ToTable("transfers", "public");
            entity.HasKey(transfer => transfer.Id);
            entity.Property(transfer => transfer.Id).HasColumnName("id");
            entity.Property(transfer => transfer.UserId).HasColumnName("user_id");
            entity.Property(transfer => transfer.FromAccountId).HasColumnName("from_account_id");
            entity.Property(transfer => transfer.ToAccountId).HasColumnName("to_account_id");
            entity.Property(transfer => transfer.Amount).HasColumnName("amount").HasPrecision(14, 2);
            entity.Property(transfer => transfer.TransferDate).HasColumnName("transfer_date");
            entity.Property(transfer => transfer.Description).HasColumnName("description").HasMaxLength(500);
            entity.Property(transfer => transfer.CreatedAt).HasColumnName("created_at").ValueGeneratedOnAdd();
            entity.Property(transfer => transfer.UpdatedAt).HasColumnName("updated_at").ValueGeneratedOnAddOrUpdate();
            entity.HasIndex(transfer => transfer.FromAccountId);
            entity.HasIndex(transfer => transfer.ToAccountId);
            entity.HasIndex(transfer => transfer.UserId);
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

        modelBuilder.Entity<CategorizationRule>(entity =>
        {
            entity.ToTable("categorization_rules", "public");
            entity.HasKey(rule => rule.Id);
            entity.Property(rule => rule.Id).HasColumnName("id");
            entity.Property(rule => rule.UserId).HasColumnName("user_id");
            entity.Property(rule => rule.CategoryId).HasColumnName("category_id");
            entity.Property(rule => rule.AccountId).HasColumnName("account_id");
            entity.Property(rule => rule.DescriptionContains).HasColumnName("description_contains").HasMaxLength(500);
            entity.Property(rule => rule.AmountDirection).HasColumnName("amount_direction");
            entity.Property(rule => rule.Priority).HasColumnName("priority");
            entity.Property(rule => rule.IsEnabled).HasColumnName("is_enabled");
            entity.Property(rule => rule.CreatedAt).HasColumnName("created_at").ValueGeneratedOnAdd();
            entity.Property(rule => rule.UpdatedAt).HasColumnName("updated_at").ValueGeneratedOnAddOrUpdate();
            entity.HasIndex(rule => new { rule.UserId, rule.Priority });
            entity.HasIndex(rule => rule.CategoryId);
            entity.HasIndex(rule => rule.AccountId);
        });

        modelBuilder.Entity<InsightPreference>(entity =>
        {
            entity.ToTable("insight_preferences", "public");
            entity.HasKey(preference => preference.Id);
            entity.Property(preference => preference.Id).HasColumnName("id");
            entity.Property(preference => preference.UserId).HasColumnName("user_id");
            entity.Property(preference => preference.InsightType).HasColumnName("insight_type");
            entity.Property(preference => preference.IsEnabled).HasColumnName("is_enabled");
            entity.Property(preference => preference.Threshold).HasColumnName("threshold").HasPrecision(6, 2);
            entity.Property(preference => preference.CreatedAt).HasColumnName("created_at").ValueGeneratedOnAdd();
            entity.Property(preference => preference.UpdatedAt).HasColumnName("updated_at").ValueGeneratedOnAddOrUpdate();
            entity.HasIndex(preference => new { preference.UserId, preference.InsightType }).IsUnique();
        });

        modelBuilder.Entity<InsightState>(entity =>
        {
            entity.ToTable("insight_states", "public");
            entity.HasKey(state => state.Id);
            entity.Property(state => state.Id).HasColumnName("id");
            entity.Property(state => state.UserId).HasColumnName("user_id");
            entity.Property(state => state.InsightType).HasColumnName("insight_type");
            entity.Property(state => state.CategoryId).HasColumnName("category_id");
            entity.Property(state => state.TransactionId).HasColumnName("transaction_id");
            entity.Property(state => state.Currency).HasColumnName("currency");
            entity.Property(state => state.PeriodStart).HasColumnName("period_start");
            entity.Property(state => state.ReadAt).HasColumnName("read_at");
            entity.Property(state => state.DismissedAt).HasColumnName("dismissed_at");
            entity.Property(state => state.DismissedValue).HasColumnName("dismissed_value").HasPrecision(14, 2);
            entity.Property(state => state.CreatedAt).HasColumnName("created_at").ValueGeneratedOnAdd();
            entity.Property(state => state.UpdatedAt).HasColumnName("updated_at").ValueGeneratedOnAddOrUpdate();
            entity.HasIndex(state => new
            {
                state.UserId,
                state.InsightType,
                state.CategoryId,
                state.Currency,
                state.PeriodStart
            }).IsUnique();
            entity.HasIndex(state => new { state.UserId, state.InsightType, state.TransactionId }).IsUnique();
            entity.HasIndex(state => state.CategoryId);
            entity.HasIndex(state => state.TransactionId);
        });

        modelBuilder.Entity<InsightMute>(entity =>
        {
            entity.ToTable("insight_mutes", "public");
            entity.HasKey(mute => mute.Id);
            entity.Property(mute => mute.Id).HasColumnName("id");
            entity.Property(mute => mute.UserId).HasColumnName("user_id");
            entity.Property(mute => mute.CategoryId).HasColumnName("category_id");
            entity.Property(mute => mute.AccountId).HasColumnName("account_id");
            entity.Property(mute => mute.CreatedAt).HasColumnName("created_at").ValueGeneratedOnAdd();
            entity.HasIndex(mute => new { mute.UserId, mute.CategoryId }).IsUnique();
            entity.HasIndex(mute => new { mute.UserId, mute.AccountId }).IsUnique();
            entity.HasIndex(mute => mute.CategoryId);
        });
    }
}

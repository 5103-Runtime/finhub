using FinHub.Api.Data;
using FinHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FinHub.Api.Services;

/// <summary>
/// Evaluates user-owned categorization rules in deterministic priority order.
/// </summary>
/// <param name="dbContext">The database context.</param>
public sealed class CategorizationRuleService(FinHubDbContext dbContext)
    : ICategorizationRuleService
{
    /// <inheritdoc />
    public async Task<CategorizationMatch?> EvaluateMatchAsync(
        Guid userId,
        Guid accountId,
        string description,
        decimal amount,
        CancellationToken cancellationToken)
    {
        List<CategorizationRule> rules = await LoadRulesAsync(
            userId,
            cancellationToken);
        CategorizationRule? rule = MatchRule(
            rules,
            accountId,
            description,
            amount);
        return rule is null
            ? null
            : new CategorizationMatch(rule.CategoryId);
    }
    
    /// <inheritdoc />
    public async Task<Guid?> EvaluateAsync(
        Guid userId,
        Guid accountId,
        string description,
        decimal amount,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Guid?> categoryIds = await EvaluateAsync(
            userId,
            accountId,
            [new CategorizationCandidate(description, amount)],
            cancellationToken);
        return categoryIds.Count == 0 ? null : categoryIds[0];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid?>> EvaluateAsync(
        Guid userId,
        Guid accountId,
        IReadOnlyList<CategorizationCandidate> transactions,
        CancellationToken cancellationToken)
    {
        if (transactions.Count == 0)
        {
            return [];
        }

        List<CategorizationRule> rules = await LoadRulesAsync(
            userId,
            cancellationToken);

        Guid?[] categoryIds = new Guid?[transactions.Count];
        for (int index = 0; index < transactions.Count; index++)
        {
            CategorizationCandidate transaction = transactions[index];
            CategorizationRule? rule = MatchRule(
                rules,
                accountId,
                transaction.Description,
                transaction.Amount);
            categoryIds[index] = rule?.CategoryId;
        }

        return categoryIds;
    }

    /// <summary>
    /// Loads enabled rules owned by the user in deterministic priority order.
    /// </summary>
    private Task<List<CategorizationRule>> LoadRulesAsync(
        Guid userId,
        CancellationToken cancellationToken) =>
        dbContext.CategorizationRules
            .AsNoTracking()
            .Where(rule => rule.UserId == userId && rule.IsEnabled)
            .OrderBy(rule => rule.Priority)
            .ThenBy(rule => rule.CreatedAt)
            .ThenBy(rule => rule.Id)
            .ToListAsync(cancellationToken);

    /// <summary>
    /// Finds the first rule that matches all configured criteria.
    /// </summary>
    /// <param name="rules">Enabled rules ordered by priority.</param>
    /// <param name="accountId">The transaction account.</param>
    /// <param name="description">The transaction description.</param>
    /// <param name="amount">The signed transaction amount.</param>
    /// <returns>The first matching category ID, or null when no rule matches.</returns>
    internal static Guid? Evaluate(
        IReadOnlyList<CategorizationRule> rules,
        Guid accountId,
        string description,
        decimal amount)
    {
        return MatchRule(rules, accountId, description, amount)?.CategoryId;
    }

    /// <summary>
    /// Finds the first rule that matches all configured criteria.
    /// </summary>
    private static CategorizationRule? MatchRule(
        IReadOnlyList<CategorizationRule> rules,
        Guid accountId,
        string description,
        decimal amount)
    {
        foreach (CategorizationRule rule in rules)
        {
            if (rule.AccountId is not null && rule.AccountId != accountId)
            {
                continue;
            }

            if (rule.DescriptionContains is not null
                && !DescriptionMatches(description, rule.DescriptionContains))
            {
                continue;
            }

            if (rule.AmountDirection == "in" && amount <= 0m)
            {
                continue;
            }

            if (rule.AmountDirection == "out" && amount >= 0m)
            {
                continue;
            }

            return rule;
        }

        return null;
    }

    /// <summary>
    /// Determines whether the rule text occurs in the transaction description.
    /// </summary>
    /// <param name="description">The transaction description.</param>
    /// <param name="ruleText">The rule's description criterion.</param>
    /// <returns>True when the transaction description contains the rule text.</returns>
    internal static bool DescriptionMatches(string description, string ruleText)
    {
        string normalizedDescription = description.Trim();
        string normalizedRuleText = ruleText.Trim();
        if (normalizedDescription.Length == 0 || normalizedRuleText.Length == 0)
        {
            return false;
        }

        return normalizedDescription.Contains(
            normalizedRuleText,
            StringComparison.OrdinalIgnoreCase);
    }
}

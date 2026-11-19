namespace FinHub.Api.Services;

/// <summary>
/// Defines categorization rule evaluation operations.
/// </summary>
public interface ICategorizationRuleService
{
    /// <summary>
    /// Finds the highest-priority matching rule for one transaction.
    /// </summary>
    /// <param name="userId">The owner of the rules.</param>
    /// <param name="accountId">The transaction account.</param>
    /// <param name="description">The transaction description.</param>
    /// <param name="amount">The signed transaction amount.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The highest-priority match, or null when no rule matches.</returns>
    Task<CategorizationMatch?> EvaluateMatchAsync(
        Guid userId,
        Guid accountId,
        string description,
        decimal amount,
        CancellationToken cancellationToken);

    /// <summary>
    /// Finds the category assigned by the first matching enabled rule for one transaction.
    /// </summary>
    /// <param name="userId">The owner of the rules.</param>
    /// <param name="accountId">The transaction account.</param>
    /// <param name="description">The transaction description.</param>
    /// <param name="amount">The signed transaction amount.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The first matching category ID, or null when no rule matches.</returns>
    Task<Guid?> EvaluateAsync(
        Guid userId,
        Guid accountId,
        string description,
        decimal amount,
        CancellationToken cancellationToken);

    /// <summary>
    /// Evaluates multiple transactions using one ordered set of enabled rules.
    /// </summary>
    /// <param name="userId">The owner of the rules.</param>
    /// <param name="accountId">The transaction account.</param>
    /// <param name="transactions">The transaction details to evaluate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>One category ID for each transaction, or null when no rule matches.</returns>
    Task<IReadOnlyList<Guid?>> EvaluateAsync(
        Guid userId,
        Guid accountId,
        IReadOnlyList<CategorizationCandidate> transactions,
        CancellationToken cancellationToken);
}

/// <summary>
/// Represents transaction details used by rule evaluation.
/// </summary>
/// <param name="Description">The transaction description.</param>
/// <param name="Amount">The signed transaction amount.</param>
public sealed record CategorizationCandidate(
    string Description,
    decimal Amount);

/// <summary>
/// Represents a matched categorization rule.
/// </summary>
/// <param name="CategoryId">The category assigned by the matching rule.</param>
public sealed record CategorizationMatch(
    Guid CategoryId);

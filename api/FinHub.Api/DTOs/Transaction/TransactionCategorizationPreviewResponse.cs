namespace FinHub.Api.DTOs.Transaction;

/// <summary>
/// Represents the automatic categorization preview for a manual transaction.
/// </summary>
/// <param name="CategoryId">The category selected by the first matching enabled rule, or null.</param>
/// <param name="AutomaticCategoryApplied">Whether an enabled rule supplied the category.</param>
public sealed record TransactionCategorizationPreviewResponse(
    Guid? CategoryId,
    bool AutomaticCategoryApplied);

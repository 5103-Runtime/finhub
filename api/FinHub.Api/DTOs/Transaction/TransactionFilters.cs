namespace FinHub.Api.DTOs.Transaction;

/// <summary>
/// Optional filters for the authenticated user's transaction list.
/// </summary>
/// <param name="AccountId">Restricts results to one owned account.</param>
/// <param name="CategoryId">Restricts results to one available category.</param>
/// <param name="Uncategorized">Restricts results to transactions without a category.</param>
/// <param name="FromDate">Inclusive lower bound for the transaction date.</param>
/// <param name="ToDate">Inclusive upper bound for the transaction date.</param>
/// <param name="MinAmount">Inclusive lower bound for the signed amount.</param>
/// <param name="MaxAmount">Inclusive upper bound for the signed amount.</param>
/// <param name="Search">Case-insensitive text searched within the description.</param>
public sealed record TransactionFilters(
    Guid? AccountId,
    Guid? CategoryId,
    bool Uncategorized,
    DateOnly? FromDate,
    DateOnly? ToDate,
    decimal? MinAmount,
    decimal? MaxAmount,
    string? Search
);

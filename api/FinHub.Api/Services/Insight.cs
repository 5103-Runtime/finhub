namespace FinHub.Api.Services;

/// <summary>
/// Represents one insight computed from a user's transactions.
/// The subject fields match the columns of <c>insight_states</c> for the same insight type.
/// </summary>
/// <param name="InsightType">The insight type.</param>
/// <param name="Severity">The severity derived from the amount.</param>
/// <param name="CategoryId">The category of a category spike; null for other types.</param>
/// <param name="TransactionId">The transaction of an unusual purchase; null for other types.</param>
/// <param name="Currency">The currency of a category spike; null for other types.</param>
/// <param name="PeriodStart">The first day of the month a category spike covers; null for other types.</param>
/// <param name="Amount">The positive amount the insight is about.</param>
/// <param name="Baseline">The positive amount the insight was compared against.</param>
public sealed record Insight(
    string InsightType,
    string Severity,
    Guid? CategoryId,
    Guid? TransactionId,
    string? Currency,
    DateOnly? PeriodStart,
    decimal Amount,
    decimal Baseline);

/// <summary>
/// Represents the transaction fields required by the insight rules.
/// </summary>
/// <param name="Id">The transaction identifier.</param>
/// <param name="AccountId">The account containing the transaction.</param>
/// <param name="TransferId">The optional transfer that created the transaction.</param>
/// <param name="CategoryId">The optional transaction category.</param>
/// <param name="TransactionDate">The date of the transaction.</param>
/// <param name="Amount">The signed transaction amount.</param>
/// <param name="Currency">The currency of the owning account.</param>
internal sealed record InsightTransaction(
    Guid Id,
    Guid AccountId,
    Guid? TransferId,
    Guid? CategoryId,
    DateOnly TransactionDate,
    decimal Amount,
    string Currency);

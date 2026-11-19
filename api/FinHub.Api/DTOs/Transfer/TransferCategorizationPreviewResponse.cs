namespace FinHub.Api.DTOs.Transfer;

/// <summary>
/// Represents automatic categorization suggestions for the two new ledger lines of a transfer.
/// </summary>
/// <param name="OutgoingCategoryId">The category suggested for the source ledger line.</param>
/// <param name="IncomingCategoryId">The category suggested for the destination ledger line.</param>
/// <param name="AutomaticCategoryApplied">Whether both ledger suggestions are identical and were categorized automatically.</param>
public sealed record TransferCategorizationPreviewResponse(
    Guid? OutgoingCategoryId,
    Guid? IncomingCategoryId,
    bool AutomaticCategoryApplied);

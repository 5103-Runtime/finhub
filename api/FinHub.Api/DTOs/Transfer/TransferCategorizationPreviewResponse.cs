namespace FinHub.Api.DTOs.Transfer;

/// <summary>
/// Represents automatic categorization suggestions for the two new ledger lines of a transfer.
/// </summary>
/// <param name="OutgoingCategoryId">The category suggested for the source ledger line.</param>
/// <param name="IncomingCategoryId">The category suggested for the destination ledger line.</param>
/// <param name="SharedCategoryId">The shared category when both ledger lines resolve to the same category.</param>
/// <param name="AutomaticCategoryApplied">Whether a shared category can be applied to the transfer.</param>
public sealed record TransferCategorizationPreviewResponse(
    Guid? OutgoingCategoryId,
    Guid? IncomingCategoryId,
    Guid? SharedCategoryId,
    bool AutomaticCategoryApplied);

namespace FinHub.Api.Statements.Csv;

/// <summary>
/// Describes how one bank's CSV export maps onto ledger transactions.
/// Mappings are data: supporting a new export is a JSON file under Statements/Csv/Mappings, not code.
/// </summary>
public sealed record StatementMapping
{
    /// <summary>
    /// Gets the bank the export comes from, such as rbc.
    /// </summary>
    public required string Bank { get; init; }

    /// <summary>
    /// Gets the account type the export is for: chequing, savings, or credit_card.
    /// </summary>
    public required string AccountType { get; init; }

    /// <summary>
    /// Gets the header cells that must all be present for this mapping to apply.
    /// </summary>
    public required IReadOnlyList<string> Match { get; init; }

    /// <summary>
    /// Gets the column that tells apart exports sharing one header, or null when the header is enough.
    /// </summary>
    public string? AccountTypeColumn { get; init; }

    /// <summary>
    /// Gets the values of <see cref="AccountTypeColumn"/> this mapping applies to.
    /// </summary>
    public IReadOnlyList<string>? AccountTypeValues { get; init; }

    /// <summary>
    /// Gets the column identifying the account a row belongs to, or null. When set, every row must agree.
    /// </summary>
    public string? AccountColumn { get; init; }

    /// <summary>
    /// Gets the column holding the transaction date.
    /// </summary>
    public required string DateColumn { get; init; }

    /// <summary>
    /// Gets the .NET format string the date is parsed with, using the invariant culture.
    /// </summary>
    public required string DateFormat { get; init; }

    /// <summary>
    /// Gets the columns joined, in order, into the description.
    /// </summary>
    public required IReadOnlyList<string> DescriptionColumns { get; init; }

    /// <summary>
    /// Gets the column holding the signed amount as the bank writes it.
    /// </summary>
    public required string AmountColumn { get; init; }

    /// <summary>
    /// Gets a value indicating whether every sign is flipped, for exports that list charges as positive.
    /// </summary>
    public required bool Invert { get; init; }

    /// <summary>
    /// Gets the column holding the running balance, or null when the export has none.
    /// </summary>
    public string? BalanceColumn { get; init; }

    /// <summary>
    /// Gets the column holding a row status, or null when the export has none.
    /// </summary>
    public string? StatusColumn { get; init; }

    /// <summary>
    /// Gets the <see cref="StatusColumn"/> values whose rows are imported, such as posted. Rows with any other status are skipped; a row with no status is rejected.
    /// </summary>
    public IReadOnlyList<string>? ImportStatuses { get; init; }

    /// <summary>
    /// Gets every column the mapping reads, which its match list must therefore contain.
    /// </summary>
    internal IEnumerable<string> ReferencedColumns =>
        new[] { DateColumn, AmountColumn, BalanceColumn, StatusColumn, AccountTypeColumn, AccountColumn }
            .Concat(DescriptionColumns)
            .OfType<string>();

    /// <summary>
    /// Gets the mapping's name for messages, such as rbc/chequing.
    /// </summary>
    public string Name => $"{Bank}/{AccountType}";
}

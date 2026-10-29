namespace FinHub.Api.Statements.Csv;

/// <summary>
/// One CSV record and the file line it starts on, counting the first line as 1.
/// </summary>
/// <param name="Line">The line the record starts on; a quoted cell may carry it across several.</param>
/// <param name="Cells">The record's cells, as written.</param>
internal sealed record CsvRow(int Line, IReadOnlyList<string> Cells)
{
    /// <summary>
    /// Gets a value indicating whether every cell is empty or whitespace.
    /// </summary>
    public bool IsBlank => Cells.All(string.IsNullOrWhiteSpace);
}

namespace FinHub.Api.Statements.Csv;

/// <summary>
/// Raised when text is not well-formed CSV, pointing at the line where the problem was found.
/// </summary>
/// <param name="message">What is wrong with the text.</param>
/// <param name="line">The line where the problem was found, counting the first line as 1.</param>
internal sealed class CsvFormatException(string message, int line) : FormatException(message)
{
    /// <summary>
    /// Gets the line where the problem was found, counting the first line as 1.
    /// </summary>
    public int Line { get; } = line;
}

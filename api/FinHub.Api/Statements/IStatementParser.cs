namespace FinHub.Api.Statements;

/// <summary>
/// Turns an uploaded bank statement into candidate transactions. One implementation per file type.
/// </summary>
public interface IStatementParser
{
    /// <summary>
    /// Tells whether this parser handles a file, judged by its name.
    /// </summary>
    /// <param name="fileName">The uploaded file's name.</param>
    /// <returns>True when <see cref="ParseAsync"/> can read the file.</returns>
    bool CanParse(string fileName);

    /// <summary>
    /// Parses a statement. Nothing is saved; the caller decides what happens to the result.
    /// </summary>
    /// <param name="file">The file's content, read from the stream's current position.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The transactions the statement holds, in file order.</returns>
    /// <exception cref="StatementFormatException">Thrown when the file is not a statement this parser understands.</exception>
    Task<ParsedStatement> ParseAsync(Stream file, CancellationToken cancellationToken = default);
}

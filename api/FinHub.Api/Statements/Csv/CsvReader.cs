using System.Globalization;
using System.Text;

namespace FinHub.Api.Statements.Csv;

/// <summary>
/// Reads comma-separated text into records, following RFC 4180 quoting. Every line yields a record,
/// blank lines included, and each record carries the file line it starts on so errors can point at
/// the file. Cells come back as written, untrimmed, so callers decide what whitespace means.
/// </summary>
internal static class CsvReader
{
    /// <summary>
    /// UTF-8 that refuses bytes it cannot decode, so a file in another encoding is rejected instead of imported with replacement characters.
    /// The preamble lets StreamReader strip a byte order mark; mark detection stays off so no other decoder can take over.
    /// </summary>
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: true, throwOnInvalidBytes: true);

    /// <summary>
    /// Told to the user when the bytes are not UTF-8, including UTF-16 without a byte order mark, which decodes as text full of NULs.
    /// </summary>
    private const string NotUtf8Message = "The file is not UTF-8 text. Export it again from the bank, or save it as UTF-8.";

    /// <summary>
    /// Far more rows than any statement holds. Stops a file of bare newlines from allocating a record per byte.
    /// </summary>
    private const int MaxRecords = 100_000;

    /// <summary>
    /// Far more cells than any statement holds, counted across the file. Stops a row of bare commas from allocating a cell per byte.
    /// </summary>
    private const int MaxCells = 1_000_000;

    /// <summary>
    /// Reads every record of a UTF-8 stream. A byte order mark, if present, is dropped.
    /// </summary>
    /// <param name="stream">The stream to read; it is left open.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The records in file order.</returns>
    /// <exception cref="FormatException">Thrown when the bytes are not UTF-8, or the file has more rows or cells than a statement can.</exception>
    /// <exception cref="CsvFormatException">Thrown when a quoted cell is never closed, or text follows a closing quote; it names the line.</exception>
    public static async Task<IReadOnlyList<CsvRow>> ReadAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        using StreamReader reader = new(stream, StrictUtf8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        string text;
        try
        {
            text = await reader.ReadToEndAsync(cancellationToken);
        }
        catch (DecoderFallbackException exception)
        {
            throw new FormatException(NotUtf8Message, exception);
        }

        if (text.Contains('\0'))
        {
            throw new FormatException(NotUtf8Message);
        }

        return Parse(text);
    }

    /// <summary>
    /// Splits CSV text into records.
    /// </summary>
    /// <param name="text">The text to split.</param>
    /// <returns>The records in file order.</returns>
    /// <exception cref="FormatException">Thrown when the text has more rows or cells than a statement can.</exception>
    /// <exception cref="CsvFormatException">Thrown when a quoted cell is never closed, or text follows a closing quote; it names the line.</exception>
    public static IReadOnlyList<CsvRow> Parse(string text)
    {
        List<CsvRow> rows = [];
        List<string> row = [];
        StringBuilder cell = new();
        bool quoted = false;
        bool closedQuote = false;
        int line = 1;
        int rowLine = 1;
        int quoteLine = 1;
        int cells = 0;

        for (int index = 0; index < text.Length; index++)
        {
            char current = text[index];

            if (quoted)
            {
                if (current != '"')
                {
                    cell.Append(current);
                    if (current == '\n' || (current == '\r' && (index + 1 >= text.Length || text[index + 1] != '\n')))
                    {
                        line++;
                    }
                }
                else if (index + 1 < text.Length && text[index + 1] == '"')
                {
                    // A doubled quote inside a quoted cell is a literal quote.
                    cell.Append('"');
                    index++;
                }
                else
                {
                    quoted = false;
                    closedQuote = true;
                }

                continue;
            }

            if (closedQuote && current is not (',' or '\r' or '\n'))
            {
                throw new CsvFormatException("Text follows a closing quote; a quoted cell must end at a comma or a line end.", line);
            }

            closedQuote = false;

            switch (current)
            {
                case '"' when cell.Length == 0:
                    quoted = true;
                    quoteLine = line;
                    break;
                case ',':
                    EndCell();
                    break;
                case '\r':
                    if (index + 1 < text.Length && text[index + 1] == '\n')
                    {
                        index++;
                    }

                    EndRow();
                    break;
                case '\n':
                    EndRow();
                    break;
                default:
                    cell.Append(current);
                    break;
            }
        }

        if (quoted)
        {
            throw new CsvFormatException("A quoted cell opens on this line and is never closed.", quoteLine);
        }

        if (row.Count > 0 || cell.Length > 0 || closedQuote)
        {
            EndRow();
        }

        return rows;

        void EndRow()
        {
            if (rows.Count == MaxRecords)
            {
                throw new FormatException($"The file has more than {MaxRecords.ToString("N0", CultureInfo.InvariantCulture)} rows.");
            }

            EndCell();
            rows.Add(new CsvRow(rowLine, row));
            row = [];
            line++;
            rowLine = line;
        }

        void EndCell()
        {
            if (++cells > MaxCells)
            {
                throw new FormatException($"The file has more than {MaxCells.ToString("N0", CultureInfo.InvariantCulture)} cells.");
            }

            row.Add(cell.ToString());
            cell.Clear();
        }
    }
}

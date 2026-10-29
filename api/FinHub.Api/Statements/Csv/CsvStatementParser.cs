using System.Globalization;

namespace FinHub.Api.Statements.Csv;

/// <summary>
/// Parses a bank's CSV export into candidate transactions, using the mapping that matches its header.
/// </summary>
/// <param name="catalog">The mappings to choose from.</param>
public sealed class CsvStatementParser(StatementMappingCatalog catalog) : IStatementParser
{
    /// <summary>
    /// The longest description the transactions table accepts.
    /// </summary>
    private const int MaxDescriptionLength = 500;

    /// <summary>
    /// The most of a bad cell an error message repeats, so a huge cell cannot flood the response or the log.
    /// </summary>
    private const int MaxEchoLength = 40;

    /// <inheritdoc />
    public bool CanParse(string fileName) =>
        string.Equals(Path.GetExtension(fileName), ".csv", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public async Task<ParsedStatement> ParseAsync(Stream file, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CsvRow> rows;
        try
        {
            rows = await CsvReader.ReadAsync(file, cancellationToken);
        }
        catch (CsvFormatException exception)
        {
            throw new StatementFormatException(
                StatementFormatException.InvalidFile, $"Line {exception.Line}: {exception.Message}", exception.Line, exception);
        }
        catch (FormatException exception)
        {
            throw new StatementFormatException(
                StatementFormatException.InvalidFile, exception.Message, innerException: exception);
        }

        int headerIndex = rows.TakeWhile(row => row.IsBlank).Count();
        if (headerIndex == rows.Count)
        {
            throw new StatementFormatException(StatementFormatException.InvalidFile, "The file is empty.");
        }

        List<string> header = rows[headerIndex].Cells.Select(cell => cell.Trim()).ToList();
        if (!catalog.MatchesHeader(header, out string? discriminator))
        {
            throw new StatementFormatException(
                StatementFormatException.UnknownFormat, "The file's columns match no supported bank export.");
        }

        CsvRow? firstRow = rows.Skip(headerIndex + 1).FirstOrDefault(row => !row.IsBlank);
        if (firstRow is null)
        {
            throw new StatementFormatException(StatementFormatException.InvalidFile, "The file has no transactions.");
        }

        // The first row picks the mapping, so a blank account type there is a row error, not an unknown format.
        if (discriminator is not null)
        {
            _ = RequiredCell(firstRow.Cells, header.IndexOf(discriminator), firstRow.Line, "account type");
        }

        StatementMapping mapping = catalog.Find(header, firstRow.Cells)
            ?? throw new StatementFormatException(
                StatementFormatException.UnknownFormat,
                "The file's columns match a supported bank export, but its first row does not name a supported account type.");

        int Column(string name) => ColumnIndex(header, mapping, name);
        int? OptionalColumn(string? name) => name is null ? null : Column(name);

        int dateColumn = Column(mapping.DateColumn);
        int amountColumn = Column(mapping.AmountColumn);
        List<int> descriptionColumns = mapping.DescriptionColumns.Select(Column).ToList();
        int? balanceColumn = OptionalColumn(mapping.BalanceColumn);
        int? statusColumn = OptionalColumn(mapping.StatusColumn);
        int? accountTypeColumn = OptionalColumn(mapping.AccountTypeColumn);
        int? accountColumn = OptionalColumn(mapping.AccountColumn);

        string? account = null;
        List<ParsedTransaction> transactions = [];
        int skippedRows = 0;

        foreach (CsvRow row in rows.Skip(headerIndex + 1))
        {
            if (row.IsBlank)
            {
                continue;
            }

            IReadOnlyList<string> cells = row.Cells;
            int line = row.Line;

            if (cells.Count != header.Count)
            {
                throw InvalidRow(line, $"The row has {cells.Count} cells; the header has {header.Count}.");
            }

            if (accountTypeColumn is int typeIndex)
            {
                string accountType = RequiredCell(cells, typeIndex, line, "account type");
                if (!mapping.AccountTypeValues!.Contains(accountType, StringComparer.OrdinalIgnoreCase))
                {
                    throw MultipleAccounts(line);
                }
            }

            if (accountColumn is int accountIndex)
            {
                string rowAccount = RequiredCell(cells, accountIndex, line, "account number");
                account ??= rowAccount;
                if (!string.Equals(account, rowAccount, StringComparison.Ordinal))
                {
                    throw MultipleAccounts(line);
                }
            }

            if (statusColumn is int statusIndex
                && !mapping.ImportStatuses!.Contains(RequiredCell(cells, statusIndex, line, "status"), StringComparer.OrdinalIgnoreCase))
            {
                skippedRows++;
                continue;
            }

            DateOnly transactionDate = ParseDate(RequiredCell(cells, dateColumn, line, "date"), mapping.DateFormat, line);
            string description = BuildDescription(cells, descriptionColumns, line);
            decimal amount = ParseNumber(RequiredCell(cells, amountColumn, line, "amount"), line, "amount");

            decimal? balanceAfter = balanceColumn is int balanceIndex && Cell(cells, balanceIndex) is { Length: > 0 } text
                ? ParseNumber(text, line, "balance")
                : null;

            if (mapping.Invert)
            {
                amount = -amount;
                balanceAfter = -balanceAfter;
            }

            transactions.Add(new ParsedTransaction(line, transactionDate, description, amount, balanceAfter));
        }

        return new ParsedStatement(mapping.Bank, mapping.AccountType, transactions, skippedRows);
    }

    /// <summary>
    /// Returns a cell trimmed, or an empty string when the row is shorter than the header.
    /// </summary>
    private static string Cell(IReadOnlyList<string> row, int column) =>
        column < row.Count ? row[column].Trim() : "";

    /// <summary>
    /// Returns a cell that must hold a value. A blank one makes the row invalid, rather than skipped or read as another account.
    /// </summary>
    private static string RequiredCell(IReadOnlyList<string> row, int column, int line, string what)
    {
        string value = Cell(row, column);
        return value.Length > 0 ? value : throw InvalidRow(line, $"The row has no {what}.");
    }

    /// <summary>
    /// Finds a mapped column in the header. A miss cannot happen for a mapping the catalog validated, so it is
    /// reported as a mapping error, not a file error. A column listed twice is a file error, since either copy
    /// could be the one the bank meant.
    /// </summary>
    private static int ColumnIndex(List<string> header, StatementMapping mapping, string column)
    {
        int index = header.IndexOf(column);
        if (index < 0)
        {
            throw new InvalidOperationException(
                $"Mapping {mapping.Name} uses column \"{column}\", which is not in the header. Add it to the mapping's match list.");
        }

        if (header.LastIndexOf(column) != index)
        {
            throw new StatementFormatException(
                StatementFormatException.InvalidFile, $"The header lists \"{column}\" more than once.");
        }

        return index;
    }

    /// <summary>
    /// Parses a date in the mapping's format, or fails the row.
    /// </summary>
    private static DateOnly ParseDate(string value, string format, int line) =>
        DateOnly.TryParseExact(value, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date)
            ? date
            : throw InvalidRow(line, $"\"{Echo(value)}\" is not a date in the format {format}.");

    /// <summary>
    /// Parses a plain decimal with an optional sign, or fails the row. Thousands separators and symbols are rejected.
    /// </summary>
    private static decimal ParseNumber(string value, int line, string what) =>
        decimal.TryParse(value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal number)
            ? number
            : throw InvalidRow(line, $"\"{Echo(value)}\" is not a valid {what}.");

    /// <summary>
    /// Quotes a bad cell in an error message: cut short, with line breaks and control characters flattened so the message stays one clean line.
    /// </summary>
    private static string Echo(string value)
    {
        string flat = string.Concat(value.ReplaceLineEndings(" ").Select(character => char.IsControl(character) ? ' ' : character));
        return flat.Length <= MaxEchoLength ? flat : Truncate(flat, MaxEchoLength) + "\u2026";
    }

    /// <summary>
    /// Joins the description columns with one space, skipping empty ones, flattens line breaks, and caps the result.
    /// </summary>
    private static string BuildDescription(IReadOnlyList<string> row, IReadOnlyList<int> columns, int line)
    {
        string description = string.Join(
            ' ',
            columns.Select(column => Cell(row, column).ReplaceLineEndings(" ")).Where(part => part.Length > 0));

        if (description.Length == 0)
        {
            throw InvalidRow(line, "The row has no description.");
        }

        return Truncate(description, MaxDescriptionLength);
    }

    /// <summary>
    /// Cuts text to a length without splitting a surrogate pair, which the database would reject as a lone half.
    /// </summary>
    private static string Truncate(string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        int cut = char.IsHighSurrogate(value[maxLength - 1]) ? maxLength - 1 : maxLength;
        return value[..cut].TrimEnd();
    }

    private static StatementFormatException InvalidRow(int line, string message) =>
        new(StatementFormatException.InvalidRow, $"Line {line}: {message}", line);

    private static StatementFormatException MultipleAccounts(int line) =>
        new(StatementFormatException.MultipleAccounts,
            $"Line {line}: The file holds more than one account. Export one account at a time.", line);
}

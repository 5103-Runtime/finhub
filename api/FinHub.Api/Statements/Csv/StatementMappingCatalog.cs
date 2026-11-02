using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FinHub.Api.Statements.Csv;

/// <summary>
/// Holds the bank statement mappings and selects the one that describes an uploaded file.
/// </summary>
public sealed class StatementMappingCatalog
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectNullableAnnotations = true,
        AllowDuplicateProperties = false,
    };

    /// <summary>
    /// Initializes the catalog from the mapping files embedded in the assembly.
    /// </summary>
    public StatementMappingCatalog() : this(LoadEmbeddedMappings())
    {
        if (Mappings.Count == 0)
        {
            throw new InvalidOperationException("No statement mappings are embedded in the assembly.");
        }
    }

    /// <summary>
    /// Initializes the catalog from the given mappings. Internal so the container always picks the
    /// embedded set; the tests reach this one through InternalsVisibleTo.
    /// </summary>
    /// <param name="mappings">The mappings to select from.</param>
    internal StatementMappingCatalog(IEnumerable<StatementMapping> mappings)
    {
        Mappings = [.. mappings];
        foreach (StatementMapping mapping in Mappings)
        {
            Validate(mapping);
        }

        for (int first = 0; first < Mappings.Count; first++)
        {
            for (int second = first + 1; second < Mappings.Count; second++)
            {
                ValidateDistinct(Mappings[first], Mappings[second]);
            }
        }
    }

    /// <summary>
    /// Gets every mapping the catalog knows about.
    /// </summary>
    public IReadOnlyList<StatementMapping> Mappings { get; }

    /// <summary>
    /// Finds the mapping whose header signature and account type describe a file.
    /// </summary>
    /// <param name="header">The file's header cells.</param>
    /// <param name="firstRow">The file's first data row, or null when it has none.</param>
    /// <returns>The matching mapping, or null when no mapping describes the file.</returns>
    /// <exception cref="StatementFormatException">Thrown when more than one mapping matches, which happens when a file carries the columns of two exports at once.</exception>
    public StatementMapping? Find(IReadOnlyList<string> header, IReadOnlyList<string>? firstRow)
    {
        List<StatementMapping> matches = Mappings
            .Where(mapping => Matches(mapping, header, firstRow))
            .ToList();

        return matches.Count switch
        {
            0 => null,
            1 => matches[0],
            _ => throw new StatementFormatException(
                StatementFormatException.UnknownFormat, "The file's columns match more than one supported bank export."),
        };
    }

    /// <summary>
    /// Checks whether a header carries any mapping's signature, whatever its account type column says, and names that
    /// column. Lets the parser tell an unknown export from a known one with a blank or unsupported account type.
    /// </summary>
    /// <param name="header">The file's header cells.</param>
    /// <param name="accountTypeColumn">The account type column of the first mapping whose signature is present, or null.</param>
    /// <returns>True when every match cell of at least one mapping is present.</returns>
    public bool MatchesHeader(IReadOnlyList<string> header, out string? accountTypeColumn)
    {
        StatementMapping? match = Mappings.FirstOrDefault(mapping => SignaturePresent(mapping, header));
        accountTypeColumn = match?.AccountTypeColumn;
        return match is not null;
    }

    /// <summary>
    /// Rejects a mapping that could never parse a file correctly, so a bad JSON file fails at startup, not on an upload.
    /// </summary>
    private static void Validate(StatementMapping mapping)
    {
        if (string.IsNullOrWhiteSpace(mapping.Bank) || string.IsNullOrWhiteSpace(mapping.AccountType))
        {
            throw new InvalidOperationException($"Mapping {mapping.Name} has a blank bank or account type.");
        }

        if (!RoundTrips(mapping.DateFormat))
        {
            throw new InvalidOperationException($"Mapping {mapping.Name} has a date format that cannot read a full date: \"{mapping.DateFormat}\".");
        }

        IEnumerable<string?>[] lists = [mapping.Match, mapping.DescriptionColumns, mapping.AccountTypeValues ?? [], mapping.ImportStatuses ?? []];
        if (lists.Any(list => list.Any(string.IsNullOrWhiteSpace)))
        {
            throw new InvalidOperationException($"Mapping {mapping.Name} has a blank or null list entry.");
        }

        string[] missing = [.. mapping.ReferencedColumns.Except(mapping.Match, StringComparer.Ordinal)];
        if (missing.Length > 0)
        {
            throw new InvalidOperationException(
                $"Mapping {mapping.Name} uses {string.Join(", ", missing)}, which its match list lacks.");
        }

        if (mapping.DescriptionColumns.Count == 0)
        {
            throw new InvalidOperationException($"Mapping {mapping.Name} lists no description columns.");
        }

        if ((mapping.AccountTypeColumn is not null) != (mapping.AccountTypeValues is { Count: > 0 }))
        {
            throw new InvalidOperationException($"Mapping {mapping.Name} must set accountTypeColumn and accountTypeValues together.");
        }

        if ((mapping.StatusColumn is not null) != (mapping.ImportStatuses is { Count: > 0 }))
        {
            throw new InvalidOperationException($"Mapping {mapping.Name} must set statusColumn and importStatuses together.");
        }
    }

    /// <summary>
    /// Checks that a date format writes a date and reads it back whole, so a mapping typo fails at startup instead of
    /// failing every row, or throwing, on upload.
    /// </summary>
    private static bool RoundTrips(string format)
    {
        DateOnly sample = new(2001, 2, 3);
        try
        {
            return DateOnly.TryParseExact(
                sample.ToString(format, CultureInfo.InvariantCulture), format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly parsed)
                && parsed == sample;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    /// <summary>
    /// Rejects two mappings that could both describe one file: one match list inside the other, with no account
    /// type values to tell them apart. Such a pair would turn every upload of that bank into an error.
    /// </summary>
    private static void ValidateDistinct(StatementMapping mapping, StatementMapping other)
    {
        bool nested = SignaturePresent(mapping, other.Match) || SignaturePresent(other, mapping.Match);
        bool separable = mapping.AccountTypeColumn is not null
            && string.Equals(mapping.AccountTypeColumn, other.AccountTypeColumn, StringComparison.Ordinal)
            && !mapping.AccountTypeValues!.Intersect(other.AccountTypeValues!, StringComparer.OrdinalIgnoreCase).Any();

        if (nested && !separable)
        {
            throw new InvalidOperationException($"Mappings {mapping.Name} and {other.Name} can both describe one file.");
        }
    }

    /// <summary>
    /// Checks whether a mapping's header signature, and account type value when it has one, match a file.
    /// </summary>
    private static bool Matches(
        StatementMapping mapping,
        IReadOnlyList<string> header,
        IReadOnlyList<string>? firstRow)
    {
        if (!SignaturePresent(mapping, header))
        {
            return false;
        }

        if (mapping.AccountTypeColumn is null)
        {
            return true;
        }

        int column = IndexOf(header, mapping.AccountTypeColumn);
        if (firstRow is null || column < 0 || column >= firstRow.Count)
        {
            return false;
        }

        return mapping.AccountTypeValues!.Contains(firstRow[column].Trim(), StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks whether every cell in a mapping's match list is present in a header.
    /// </summary>
    private static bool SignaturePresent(StatementMapping mapping, IReadOnlyList<string> header) =>
        mapping.Match.All(cell => header.Contains(cell, StringComparer.Ordinal));

    /// <summary>
    /// Returns the position of a header cell, or -1 when it is absent.
    /// </summary>
    private static int IndexOf(IReadOnlyList<string> header, string cell)
    {
        for (int index = 0; index < header.Count; index++)
        {
            if (string.Equals(header[index], cell, StringComparison.Ordinal))
            {
                return index;
            }
        }

        return -1;
    }

    /// <summary>
    /// Reads every mapping file embedded under Statements/Csv/Mappings, in name order.
    /// </summary>
    private static IEnumerable<StatementMapping> LoadEmbeddedMappings()
    {
        Assembly assembly = typeof(StatementMappingCatalog).Assembly;

        foreach (string resourceName in assembly.GetManifestResourceNames().Order())
        {
            if (!resourceName.Contains(".Statements.Csv.Mappings.", StringComparison.Ordinal)
                || !resourceName.EndsWith(".json", StringComparison.Ordinal))
            {
                continue;
            }

            using Stream stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Embedded mapping {resourceName} could not be opened.");

            yield return ReadMapping(stream, resourceName);
        }
    }

    /// <summary>
    /// Reads one mapping file. Unknown keys and missing fields are errors, so a typo cannot change how a bank parses.
    /// </summary>
    internal static StatementMapping ReadMapping(Stream json, string name)
    {
        try
        {
            return JsonSerializer.Deserialize<StatementMapping>(json, SerializerOptions)
                ?? throw new InvalidOperationException($"Mapping {name} is empty.");
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException($"Mapping {name} is malformed: {exception.Message}", exception);
        }
    }
}

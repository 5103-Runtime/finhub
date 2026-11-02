namespace FinHub.Api.Statements;

/// <summary>
/// Raised when an uploaded file cannot be turned into transactions. The error code is stable so the
/// API can map it to a response; the line, when known, is where in the file the problem was found.
/// </summary>
public sealed class StatementFormatException : Exception
{
    /// <summary>
    /// The file is empty or has no rows after its header, is not UTF-8 or well-formed CSV, has more rows or
    /// cells than a statement can, or lists a mapped column twice.
    /// </summary>
    public const string InvalidFile = "invalid_statement_file";

    /// <summary>
    /// The header matches no mapping, or more than one, or the first row names no supported account type.
    /// </summary>
    public const string UnknownFormat = "unknown_statement_format";

    /// <summary>
    /// One row could not be parsed, or has a blank account type, account number, or status.
    /// </summary>
    public const string InvalidRow = "invalid_statement_row";

    /// <summary>
    /// The file holds rows from more than one account.
    /// </summary>
    public const string MultipleAccounts = "multiple_accounts";

    /// <summary>
    /// Initializes a new instance of the <see cref="StatementFormatException"/> class.
    /// </summary>
    /// <param name="errorCode">One of the constants on this class.</param>
    /// <param name="message">What went wrong, worded for the user.</param>
    /// <param name="line">The file line where the problem was found, counting the first line as 1, or null when it has none.</param>
    /// <param name="innerException">The underlying failure, if any.</param>
    public StatementFormatException(
        string errorCode,
        string message,
        int? line = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        Line = line;
    }

    /// <summary>
    /// Gets the stable error code.
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Gets the file line where the problem was found, or null when it has none.
    /// </summary>
    public int? Line { get; }
}

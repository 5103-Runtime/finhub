using System.Text;

namespace FinHub.Api.Tests.Statements.Csv;

[TestClass]
public sealed class CsvStatementParserTests
{
    private const string RbcHeader =
        "Account Type,Account Number,Transaction Date,Cheque Number,Description 1,Description 2,CAD$,USD$";

    private const string ScotiabankChequingHeader =
        "Filter,Date,Description,Sub-description,Type of Transaction,Amount,Balance";

    private const string ScotiabankCreditHeader =
        "Filter,Date,Description,Sub-description,Status,Type of Transaction,Amount,Associated card number";

    private static readonly StatementMappingCatalog Catalog = new();

    private static readonly CsvStatementParser Parser = new(Catalog);

    [TestMethod]
    [DataRow("rbc-chequing-statement.csv", "rbc", "chequing", 26, 0)]
    [DataRow("rbc-credit-statement.csv", "rbc", "credit_card", 56, 0)]
    [DataRow("rbc-mastercard-statement.csv", "rbc", "credit_card", 24, 0)]
    [DataRow("scotiabank-chequing-statement.csv", "scotiabank", "chequing", 20, 0)]
    [DataRow("scotiabank-credit-statement.csv", "scotiabank", "credit_card", 19, 4)]
    public void Parse_ReadsEverySampleStatement(string fileName, string bank, string accountType, int transactions, int skippedRows)
    {
        ParsedStatement statement = ParseFixture(fileName);

        Assert.AreEqual(bank, statement.Bank);
        Assert.AreEqual(accountType, statement.AccountType);
        Assert.AreEqual(transactions, statement.Transactions.Count);
        Assert.AreEqual(skippedRows, statement.SkippedRows);

        for (int index = 0; index < statement.Transactions.Count; index++)
        {
            ParsedTransaction transaction = statement.Transactions[index];
            Assert.AreEqual(transaction.Description.Trim(), transaction.Description);
            Assert.IsTrue(transaction.Description.Length is > 0 and <= 500);
            Assert.AreNotEqual(0m, transaction.Amount);
            if (index > 0)
            {
                Assert.IsTrue(transaction.StatementLine > statement.Transactions[index - 1].StatementLine);
            }
        }
    }

    [TestMethod]
    public void Parse_CountsLinesInsideQuotedCells()
    {
        StatementFormatException exception = Assert.ThrowsExactly<StatementFormatException>(
            () => Parse($"{RbcHeader}\nChequing,0,10/1/2026,,\"TWO\nLINES\",,1,\nChequing,0,bad,,X,,1,\n"));

        Assert.AreEqual(4, exception.Line);
    }

    [TestMethod]
    public void Parse_ReadsKnownRowsFromTheSamples()
    {
        Assert.AreEqual(
            new ParsedTransaction(2, new DateOnly(2026, 10, 1), "DEPOSIT", 209.77m, null),
            ParseFixture("rbc-chequing-statement.csv").Transactions[0]);
        Assert.AreEqual(
            new ParsedTransaction(6, new DateOnly(2026, 10, 23), "gas station", -46m, null),
            ParseFixture("scotiabank-credit-statement.csv").Transactions[0]);
    }

    [TestMethod]
    public void Parse_ScotiabankChequing_ReportsABalanceChainNewestFirst()
    {
        IReadOnlyList<ParsedTransaction> transactions = ParseFixture("scotiabank-chequing-statement.csv").Transactions;
        Assert.IsTrue(transactions.All(transaction => transaction.BalanceAfter is not null));

        for (int index = 0; index + 1 < transactions.Count; index++)
        {
            Assert.AreEqual(
                transactions[index + 1].BalanceAfter + transactions[index].Amount,
                transactions[index].BalanceAfter);
        }
    }

    [TestMethod]
    public void Parse_ScotiabankCredit_InvertsSigns_AndSkipsPendingRows()
    {
        ParsedStatement statement = ParseFixture("scotiabank-credit-statement.csv");

        Assert.AreEqual(6, statement.Transactions[0].StatementLine);
        foreach (ParsedTransaction transaction in statement.Transactions)
        {
            bool isPayment = transaction.Description.StartsWith("payment", StringComparison.Ordinal);
            Assert.AreEqual(isPayment, transaction.Amount > 0, transaction.Description);
        }
    }

    [TestMethod]
    public void Parse_RbcCredit_KeepsTheBankSigns()
    {
        IReadOnlyList<ParsedTransaction> transactions = ParseFixture("rbc-credit-statement.csv").Transactions;

        Assert.AreEqual(9, transactions.Count(transaction => transaction.Amount > 0));
        foreach (ParsedTransaction transaction in transactions.Where(transaction => transaction.Amount > 0))
        {
            Assert.IsTrue(transaction.Description.StartsWith("PAYMENT", StringComparison.Ordinal), transaction.Description);
        }
    }

    [TestMethod]
    public void Parse_JoinsDescriptionColumns_TrimsThem_AndCapsThemAt500()
    {
        string longDescription = new('A', 600);
        ParsedStatement statement = Parse(
            $"{ScotiabankChequingHeader}\n,10/22/2026,bill payment,Mb-Utility Co 00000001 ,Debit,-83.42,2039.13\n,10/21/2026,{longDescription},,Debit,-1,2122.55\n");

        Assert.AreEqual("bill payment Mb-Utility Co 00000001", statement.Transactions[0].Description);
        Assert.AreEqual(500, statement.Transactions[1].Description.Length);
    }

    [TestMethod]
    public void Parse_NeverCutsADescriptionInsideASurrogatePair()
    {
        string description = new string('A', 499) + "\U0001F600B";
        ParsedStatement statement = Parse($"{RbcHeader}\nChequing,0,10/1/2026,,{description},,1,\n");

        Assert.AreEqual(499, statement.Transactions[0].Description.Length);
    }

    [TestMethod]
    public void Parse_FlattensLineBreaksInsideADescription()
    {
        ParsedStatement statement = Parse($"{RbcHeader}\nChequing,0,10/1/2026,,\"TWO\nLINES\",,1,\n");

        Assert.AreEqual("TWO LINES", statement.Transactions[0].Description);
    }

    [TestMethod]
    public void Parse_ReturnsNoTransactions_WhenEveryRowIsPending()
    {
        ParsedStatement statement = Parse($"{ScotiabankCreditHeader}\n,10/28/2026,bns,,pending,Credit,-50,***\n");

        Assert.AreEqual(0, statement.Transactions.Count);
        Assert.AreEqual(1, statement.SkippedRows);
    }

    [TestMethod]
    public void Parse_ReadsQuotedCommas_AndZeroPaddedDates()
    {
        ParsedTransaction transaction = Parse(
            $"{RbcHeader}\nChequing,0,10/01/2026,,\"E-TRANSFER SENT, JOHN\",,1333.09,\n").Transactions[0];

        Assert.AreEqual("E-TRANSFER SENT, JOHN", transaction.Description);
        Assert.AreEqual(1333.09m, transaction.Amount);
        Assert.AreEqual(new DateOnly(2026, 10, 1), transaction.TransactionDate);
        Assert.IsNull(transaction.BalanceAfter);
    }

    [TestMethod]
    public void Parse_ImportsOnlyListedStatuses_CaseInsensitively_AndKeepsLineNumbers()
    {
        ParsedStatement statement = Parse(
            $"{ScotiabankCreditHeader}\n,10/28/2026,bns,,pending,Credit,-50,***\n,10/26/2026,gas,,PENDING,Debit,40,***\n,10/23/2026,gas,,posted,Debit,46,***\n,10/21/2026,payment - thank you,,Posted,Credit,-250,***\n,10/20/2026,shop,,declined,Debit,10,***\n");

        Assert.AreEqual(3, statement.SkippedRows);
        Assert.AreEqual(2, statement.Transactions.Count);
        Assert.AreEqual(4, statement.Transactions[0].StatementLine);
        Assert.AreEqual(-46m, statement.Transactions[0].Amount);
        Assert.AreEqual(250m, statement.Transactions[1].Amount);
    }

    [TestMethod]
    public void Parse_IgnoresWhitespaceRows_WithoutCountingThem()
    {
        ParsedStatement statement = Parse($"{RbcHeader}\nChequing,0,10/1/2026,,X,,1,\n,,,,,,,\n   \n");

        Assert.AreEqual(1, statement.Transactions.Count);
        Assert.AreEqual(0, statement.SkippedRows);
    }

    [TestMethod]
    public void Parse_InvertsTheBalanceWithTheAmount()
    {
        StatementMapping mapping = Catalog.Mappings.Single(candidate => candidate.Name == "scotiabank/chequing");
        CsvStatementParser parser = new(new StatementMappingCatalog([mapping with { Invert = true }]));

        ParsedTransaction transaction = parser
            .ParseAsync(ToStream($"{ScotiabankChequingHeader}\n,10/1/2026,x,,Debit,-5,100\n")).GetAwaiter().GetResult()
            .Transactions[0];

        Assert.AreEqual(5m, transaction.Amount);
        Assert.AreEqual(-100m, transaction.BalanceAfter);
    }

    [TestMethod]
    [DataRow("", StatementFormatException.InvalidFile, DisplayName = "empty file")]
    [DataRow(RbcHeader + "\n", StatementFormatException.InvalidFile, DisplayName = "header only")]
    [DataRow("a,b,c\n1,2,3\n", StatementFormatException.UnknownFormat, DisplayName = "unknown header")]
    [DataRow(RbcHeader + "\n\n   \n", StatementFormatException.InvalidFile, DisplayName = "header and blank lines")]
    [DataRow(RbcHeader + ",CAD$\nChequing,0,10/1/2026,,X,,1,,2\n", StatementFormatException.InvalidFile, DisplayName = "duplicate column")]
    [DataRow(ScotiabankCreditHeader + ",Balance\n,10/1/2026,x,,posted,Debit,5,***,100\n", StatementFormatException.UnknownFormat, DisplayName = "columns of two exports")]
    [DataRow("a,b,c\n", StatementFormatException.UnknownFormat, DisplayName = "unknown header, no rows")]
    [DataRow("\n \n", StatementFormatException.InvalidFile, DisplayName = "blank lines only")]
    public void Parse_RejectsTheWholeFile(string text, string errorCode)
    {
        StatementFormatException exception = Assert.ThrowsExactly<StatementFormatException>(() => Parse(text));

        Assert.AreEqual(errorCode, exception.ErrorCode);
        Assert.IsNull(exception.Line);
    }

    [TestMethod]
    public void Parse_NamesTheLine_WhenTheCsvItselfIsMalformed()
    {
        StatementFormatException exception = Assert.ThrowsExactly<StatementFormatException>(
            () => Parse($"{RbcHeader}\nChequing,0,10/1/2026,,OK,,1,\nChequing,0,10/2/2026,,\"unclosed,,1,\n"));

        Assert.AreEqual(StatementFormatException.InvalidFile, exception.ErrorCode);
        Assert.AreEqual(3, exception.Line);
        StringAssert.StartsWith(exception.Message, "Line 3: ");
    }

    [TestMethod]
    public void Parse_RejectsAFirstRow_WithABlankAccountType_AsARow()
    {
        StatementFormatException exception = Assert.ThrowsExactly<StatementFormatException>(
            () => Parse($"{RbcHeader}\n,0,10/1/2026,,X,,1,\n"));

        Assert.AreEqual(StatementFormatException.InvalidRow, exception.ErrorCode);
        Assert.AreEqual(2, exception.Line);
    }

    [TestMethod]
    public void Parse_SaysWhenOnlyTheAccountTypeIsUnsupported()
    {
        StatementFormatException exception = Assert.ThrowsExactly<StatementFormatException>(
            () => Parse($"{RbcHeader}\nLoan,0,10/1/2026,,X,,1,\n"));

        Assert.AreEqual(StatementFormatException.UnknownFormat, exception.ErrorCode);
        StringAssert.Contains(exception.Message, "account type");
    }

    [TestMethod]
    public void Parse_LabelsAnRbcSavingsExportAsSavings()
    {
        ParsedStatement statement = Parse($"{RbcHeader}\nSavings,0,10/1/2026,,X,,1,\n");

        Assert.AreEqual("savings", statement.AccountType);
    }

    [TestMethod]
    [DataRow("Chequing,0,2026-10-01,,X,,1,", DisplayName = "date in another format")]
    [DataRow("Chequing,0,1/1/26,,X,,1,", DisplayName = "two-digit year")]
    [DataRow("Chequing,0,10/1/2026,,X,,(12.50),", DisplayName = "parenthesised amount")]
    [DataRow("Chequing,0,10/1/2026,,X,,$12,", DisplayName = "currency symbol")]
    [DataRow("Chequing,0,10/1/2026,,X,,,", DisplayName = "empty amount")]
    [DataRow("Chequing,0,10/1/2026", DisplayName = "short row")]
    [DataRow("Chequing,0,10/1/2026,,,,1,", DisplayName = "empty description")]
    [DataRow(",0,10/1/2026,,X,,1,", DisplayName = "blank account type")]
    [DataRow("Chequing,,10/1/2026,,X,,1,", DisplayName = "blank account number")]
    [DataRow("Chequing,0,10/1/2026,,X,,\"1,333.09\",", DisplayName = "thousands separator")]
    [DataRow("Chequing,0,10/1/2026,,X,,\"12,50\",", DisplayName = "comma decimal")]
    [DataRow("Chequing,0,10/1/2026,,X,,1,,extra", DisplayName = "row longer than the header")]
    public void Parse_RejectsARow_AndReportsItsLine(string row)
    {
        StatementFormatException exception = Assert.ThrowsExactly<StatementFormatException>(
            () => Parse($"{RbcHeader}\nChequing,0,10/1/2026,,OK,,1,\n{row}\n"));

        Assert.AreEqual(StatementFormatException.InvalidRow, exception.ErrorCode);
        Assert.AreEqual(3, exception.Line);
    }

    [TestMethod]
    [DataRow("statement.csv", true)]
    [DataRow("STATEMENT.CSV", true)]
    [DataRow("statement.pdf", false)]
    [DataRow("csv", false)]
    public void CanParse_JudgesByExtension(string fileName, bool expected)
    {
        Assert.AreEqual(expected, Parser.CanParse(fileName));
    }

    [TestMethod]
    [DataRow(",10/22/2026,gas,,,Debit,40,***", DisplayName = "blank status")]
    [DataRow(",10/22/2026,gas", DisplayName = "truncated row")]
    public void Parse_RejectsARow_WithoutAStatus_InsteadOfSkippingIt(string row)
    {
        StatementFormatException exception = Assert.ThrowsExactly<StatementFormatException>(
            () => Parse($"{ScotiabankCreditHeader}\n,10/23/2026,gas,,posted,Debit,46,***\n{row}\n"));

        Assert.AreEqual(StatementFormatException.InvalidRow, exception.ErrorCode);
        Assert.AreEqual(3, exception.Line);
    }

    [TestMethod]
    [DataRow("Visa,0,10/2/2026,,X,,-1,", DisplayName = "another account type")]
    [DataRow("Chequing,1,10/2/2026,,X,,-1,", DisplayName = "another account number")]
    public void Parse_RejectsAFileHoldingMoreThanOneAccount(string row)
    {
        StatementFormatException exception = Assert.ThrowsExactly<StatementFormatException>(
            () => Parse($"{RbcHeader}\nChequing,0,10/1/2026,,OK,,1,\n{row}\n"));

        Assert.AreEqual(StatementFormatException.MultipleAccounts, exception.ErrorCode);
        Assert.AreEqual(3, exception.Line);
    }

    [TestMethod]
    public void Parse_NumbersLinesAsTheFileDoes_BlankLinesIncluded()
    {
        ParsedStatement statement = Parse($"{RbcHeader}\n\nChequing,0,10/1/2026,,X,,1,\n");

        Assert.AreEqual(3, statement.Transactions[0].StatementLine);
    }

    [TestMethod]
    public void Parse_SkipsBlankLinesBeforeTheHeader()
    {
        ParsedStatement statement = Parse($"\n{RbcHeader}\nChequing,0,10/1/2026,,X,,1,\n");

        Assert.AreEqual(1, statement.Transactions.Count);
        Assert.AreEqual(3, statement.Transactions[0].StatementLine);
    }

    [TestMethod]
    public void Parse_TrimsHeaderCells()
    {
        string paddedHeader = string.Join(",", RbcHeader.Split(',').Select(cell => $" {cell} "));
        ParsedStatement statement = Parse($"{paddedHeader}\nChequing,0,10/1/2026,,X,,1,\n");

        Assert.AreEqual(1, statement.Transactions.Count);
    }

    [TestMethod]
    public void Parse_KeepsTheErrorShort_WhenABadCellIsHuge()
    {
        StatementFormatException exception = Assert.ThrowsExactly<StatementFormatException>(
            () => Parse($"{RbcHeader}\nChequing,0,{new string('x', 1000)},,X,,1,\n"));

        Assert.AreEqual(StatementFormatException.InvalidRow, exception.ErrorCode);
        Assert.IsTrue(exception.Message.Length < 120, exception.Message);
        StringAssert.Contains(exception.Message, "\u2026");
    }

    [TestMethod]
    public void Parse_FlattensLineBreaksAndControlCharacters_WhenEchoingABadCell()
    {
        StatementFormatException exception = Assert.ThrowsExactly<StatementFormatException>(
            () => Parse($"{RbcHeader}\nChequing,0,\"bad\ndate\tx\",,X,,1,\n"));

        Assert.IsFalse(exception.Message.Any(char.IsControl), exception.Message);
    }

    [TestMethod]
    public void Parse_SkipsAnUnlistedStatusRow_WithoutValidatingIt()
    {
        ParsedStatement statement = Parse($"{ScotiabankCreditHeader}\n,not a date,x,,pending,Debit,abc,***\n,10/23/2026,gas,,posted,Debit,46,***\n");

        Assert.AreEqual(1, statement.SkippedRows);
        Assert.AreEqual(1, statement.Transactions.Count);
    }

    [TestMethod]
    public void Parse_KeepsAZeroAmount_WhenInverting()
    {
        ParsedTransaction transaction = Parse($"{ScotiabankCreditHeader}\n,10/23/2026,fee waived,,posted,Debit,0.00,***\n").Transactions[0];

        Assert.AreEqual(0m, transaction.Amount);
    }

    [TestMethod]
    public async Task ParseAsync_PropagatesCancellation_InsteadOfReportingABadFile()
    {
        using MemoryStream stream = ToStream($"{RbcHeader}\nChequing,0,10/1/2026,,X,,1,\n");

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => Parser.ParseAsync(stream, new CancellationToken(canceled: true)));
    }

    private static ParsedStatement Parse(string text) => Parser.ParseAsync(ToStream(text)).GetAwaiter().GetResult();

    private static MemoryStream ToStream(string text) => new(Encoding.UTF8.GetBytes(text));

    private static ParsedStatement ParseFixture(string fileName)
    {
        using FileStream file = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName));
        return Parser.ParseAsync(file).GetAwaiter().GetResult();
    }
}

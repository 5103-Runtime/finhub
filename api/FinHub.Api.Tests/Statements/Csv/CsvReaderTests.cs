using System.Text;

namespace FinHub.Api.Tests.Statements.Csv;

[TestClass]
public sealed class CsvReaderTests
{
    [TestMethod]
    [DataRow("a,b\r\nc,d\r\n", DisplayName = "CRLF")]
    [DataRow("a,b\nc,d\n", DisplayName = "LF")]
    [DataRow("a,b\rc,d\r", DisplayName = "CR")]
    [DataRow("a,b\nc,d", DisplayName = "no trailing newline")]
    [DataRow("\"a\",b\r\nc,d\r\n", DisplayName = "CRLF after a closing quote")]
    public void Parse_SplitsRecords_ForEveryLineEnding(string text)
    {
        IReadOnlyList<CsvRow> rows = CsvReader.Parse(text);

        Assert.AreEqual(2, rows.Count);
        Assert.AreEqual(2, rows[1].Line);
        CollectionAssert.AreEqual(new[] { "c", "d" }, rows[1].Cells.ToArray());
    }

    [TestMethod]
    public void Parse_ReturnsNoRecords_ForEmptyText()
    {
        Assert.AreEqual(0, CsvReader.Parse("").Count);
    }

    [TestMethod]
    public void Parse_HandlesQuotedCells()
    {
        IReadOnlyList<CsvRow> rows = CsvReader.Parse("\"x,y\",\"he said \"\"hi\"\"\",\"multi\r\nline\",\"\"\n");

        Assert.AreEqual(1, rows.Count);
        CollectionAssert.AreEqual(new[] { "x,y", "he said \"hi\"", "multi\r\nline", "" }, rows[0].Cells.ToArray());
    }

    [TestMethod]
    [DataRow("\"a\nb\",1\nc,2\n", DisplayName = "LF")]
    [DataRow("\"a\r\nb\",1\r\nc,2\r\n", DisplayName = "CRLF")]
    [DataRow("\"a\rb\",1\rc,2\r", DisplayName = "CR")]
    public void Parse_CountsLinesInsideQuotedCells(string text)
    {
        IReadOnlyList<CsvRow> rows = CsvReader.Parse(text);

        Assert.AreEqual(1, rows[0].Line);
        Assert.AreEqual(3, rows[1].Line);
    }

    [TestMethod]
    public void Parse_KeepsAQuoteAfterLeadingSpaceAsText()
    {
        CollectionAssert.AreEqual(new[] { "a", " \"b\"" }, CsvReader.Parse("a, \"b\"\n")[0].Cells.ToArray());
    }

    [TestMethod]
    public void Parse_Throws_WhenTheTextHasTooManyRows()
    {
        Assert.ThrowsExactly<FormatException>(() => CsvReader.Parse(new string('\n', 100_001)));
    }

    [TestMethod]
    public void Parse_Throws_WhenTheTextHasTooManyCells()
    {
        Assert.ThrowsExactly<FormatException>(() => CsvReader.Parse(new string(',', 1_000_001)));
    }

    [TestMethod]
    public void Parse_AcceptsExactlyTheRowCap()
    {
        Assert.AreEqual(100_000, CsvReader.Parse(new string('\n', 100_000)).Count);
    }

    [TestMethod]
    public void Parse_AcceptsExactlyTheCellCap()
    {
        Assert.AreEqual(1_000_000, CsvReader.Parse(new string(',', 999_999))[0].Cells.Count);
    }

    [TestMethod]
    public void Parse_KeepsAQuoteInsideAnUnquotedCellAsText()
    {
        IReadOnlyList<CsvRow> rows = CsvReader.Parse("12\" PIZZA,c\n");

        CollectionAssert.AreEqual(new[] { "12\" PIZZA", "c" }, rows[0].Cells.ToArray());
    }

    [TestMethod]
    public void Parse_KeepsBlankLinesAsEmptyRecords_ButAddsNoneAfterTheLastNewline()
    {
        IReadOnlyList<CsvRow> rows = CsvReader.Parse("a,b\n\nc,d\n");

        Assert.AreEqual(3, rows.Count);
        CollectionAssert.AreEqual(new[] { "" }, rows[1].Cells.ToArray());
        Assert.AreEqual(3, rows[2].Line);
    }

    [TestMethod]
    public void Parse_KeepsAQuotedEmptyCellAtTheEndOfTheText()
    {
        IReadOnlyList<CsvRow> rows = CsvReader.Parse("\"\"");

        Assert.AreEqual(1, rows.Count);
        CollectionAssert.AreEqual(new[] { "" }, rows[0].Cells.ToArray());
    }

    [TestMethod]
    [DataRow("a,b\nc,\"12.5\"0\n", 2, DisplayName = "single-line cell")]
    [DataRow("a,b\nc,\"two\nlines\"0\n", 3, DisplayName = "multi-line cell")]
    public void Parse_Throws_WhenTextFollowsAClosingQuote_AndNamesTheLine(string text, int line)
    {
        CsvFormatException exception = Assert.ThrowsExactly<CsvFormatException>(() => CsvReader.Parse(text));

        Assert.AreEqual(line, exception.Line);
    }

    [TestMethod]
    public void Parse_KeepsEmptyTrailingCells_AndDoesNotTrim()
    {
        IReadOnlyList<string> cells = CsvReader.Parse(" a , b ,\n")[0].Cells;

        CollectionAssert.AreEqual(new[] { " a ", " b ", "" }, cells.ToArray());
    }

    [TestMethod]
    public void Parse_Throws_WhenAQuotedCellIsNeverClosed_AndNamesTheLineItOpenedOn()
    {
        CsvFormatException exception = Assert.ThrowsExactly<CsvFormatException>(() => CsvReader.Parse("a,b\nc,\"two\nlines\nd,e\n"));

        Assert.AreEqual(2, exception.Line);
    }

    [TestMethod]
    public async Task ReadAsync_DropsTheByteOrderMark_AndLeavesTheStreamOpen()
    {
        using MemoryStream stream = new([0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes("a,b\n")]);

        IReadOnlyList<CsvRow> rows = await CsvReader.ReadAsync(stream);

        Assert.AreEqual("a", rows[0].Cells[0]);
        Assert.IsTrue(stream.CanRead);
    }

    [TestMethod]
    [DataRow(false, DisplayName = "no byte order mark")]
    [DataRow(true, DisplayName = "byte order mark")]
    public async Task ReadAsync_Throws_WhenTheBytesAreNotUtf8(bool withByteOrderMark)
    {
        byte[] prefix = withByteOrderMark ? [0xEF, 0xBB, 0xBF] : [];
        using MemoryStream stream = new([.. prefix, .. Encoding.UTF8.GetBytes("a,"), 0xE9, (byte)'\n']);

        await Assert.ThrowsExactlyAsync<FormatException>(() => CsvReader.ReadAsync(stream));
    }

    [TestMethod]
    [DataRow(true, DisplayName = "byte order mark")]
    [DataRow(false, DisplayName = "no byte order mark")]
    public async Task ReadAsync_Throws_WhenTheFileIsUtf16(bool withByteOrderMark)
    {
        byte[] prefix = withByteOrderMark ? Encoding.Unicode.GetPreamble() : [];
        using MemoryStream stream = new([.. prefix, .. Encoding.Unicode.GetBytes("a,b\n")]);

        await Assert.ThrowsExactlyAsync<FormatException>(() => CsvReader.ReadAsync(stream));
    }

    [TestMethod]
    public async Task ReadAsync_Throws_WhenTheTextContainsANul()
    {
        using MemoryStream stream = new([.. Encoding.UTF8.GetBytes("a,b"), 0x00, .. Encoding.UTF8.GetBytes("c\n")]);

        await Assert.ThrowsExactlyAsync<FormatException>(() => CsvReader.ReadAsync(stream));
    }
}

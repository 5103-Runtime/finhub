using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace FinHub.Api.Tests.Statements.Csv;

[TestClass]
public sealed class StatementMappingCatalogTests
{
    private static readonly string[] RbcHeader =
        ["Account Type", "Account Number", "Transaction Date", "Cheque Number", "Description 1", "Description 2", "CAD$", "USD$"];

    private static readonly string[] ScotiabankChequingHeader =
        ["Filter", "Date", "Description", "Sub-description", "Type of Transaction", "Amount", "Balance"];

    private static readonly StatementMappingCatalog Catalog = new();

    [TestMethod]
    public void Constructor_LoadsEveryEmbeddedMapping()
    {
        CollectionAssert.AreEquivalent(
            new[] { "rbc/chequing", "rbc/credit_card", "rbc/savings", "scotiabank/chequing", "scotiabank/credit_card" },
            Catalog.Mappings.Select(mapping => mapping.Name).ToArray());
    }

    [TestMethod]
    public void Constructor_RejectsAMappingNamingAColumnOutsideItsMatchList()
    {
        StatementMapping mapping = Catalog.Mappings.Single(candidate => candidate.Name == "scotiabank/chequing");

        Assert.ThrowsExactly<InvalidOperationException>(
            () => new StatementMappingCatalog([mapping with { DateColumn = "Nope" }]));
    }

    [TestMethod]
    public void Constructor_RejectsAStatusColumnWithoutImportStatuses()
    {
        StatementMapping mapping = Catalog.Mappings.Single(candidate => candidate.Name == "scotiabank/credit_card");

        Assert.ThrowsExactly<InvalidOperationException>(
            () => new StatementMappingCatalog([mapping with { ImportStatuses = null }]));
    }

    [TestMethod]
    public void Constructor_RejectsAnAccountTypeColumnWithoutValues()
    {
        StatementMapping mapping = Catalog.Mappings.Single(candidate => candidate.Name == "rbc/chequing");

        Assert.ThrowsExactly<InvalidOperationException>(
            () => new StatementMappingCatalog([mapping with { AccountTypeValues = null }]));
    }

    [TestMethod]
    public void Constructor_RejectsAMappingWithNoDescriptionColumns()
    {
        StatementMapping mapping = Catalog.Mappings.Single(candidate => candidate.Name == "scotiabank/chequing");

        Assert.ThrowsExactly<InvalidOperationException>(
            () => new StatementMappingCatalog([mapping with { DescriptionColumns = [] }]));
    }

    [TestMethod]
    [DataRow("", DisplayName = "empty")]
    [DataRow("Q", DisplayName = "not a format")]
    [DataRow("M/d", DisplayName = "no year")]
    public void Constructor_RejectsADateFormatThatCannotReadAFullDate(string format)
    {
        StatementMapping mapping = Catalog.Mappings.Single(candidate => candidate.Name == "scotiabank/chequing");

        Assert.ThrowsExactly<InvalidOperationException>(
            () => new StatementMappingCatalog([mapping with { DateFormat = format }]));
    }

    [TestMethod]
    public void Constructor_RejectsABlankBankOrAccountType()
    {
        StatementMapping mapping = Catalog.Mappings.Single(candidate => candidate.Name == "scotiabank/chequing");

        Assert.ThrowsExactly<InvalidOperationException>(() => new StatementMappingCatalog([mapping with { Bank = " " }]));
        Assert.ThrowsExactly<InvalidOperationException>(() => new StatementMappingCatalog([mapping with { AccountType = "" }]));
    }

    [TestMethod]
    public void Constructor_RejectsABlankOrNullListEntry()
    {
        StatementMapping mapping = Catalog.Mappings.Single(candidate => candidate.Name == "scotiabank/chequing");

        Assert.ThrowsExactly<InvalidOperationException>(
            () => new StatementMappingCatalog([mapping with { DescriptionColumns = [null!] }]));
        Assert.ThrowsExactly<InvalidOperationException>(
            () => new StatementMappingCatalog([mapping with { Match = [.. mapping.Match, " "] }]));
    }

    [TestMethod]
    [DataRow("\"dateFormat\": \"M/d/yyyy\", \"invert\": false, \"inverted\": true", DisplayName = "unknown key")]
    [DataRow("\"dateFormat\": \"M/d/yyyy\", \"invert\": false, \"invert\": true", DisplayName = "duplicate key")]
    [DataRow("\"dateFormat\": \"M/d/yyyy\"", DisplayName = "missing invert")]
    [DataRow("\"dateFormat\": null, \"invert\": false", DisplayName = "null required field")]
    public void ReadMapping_RejectsAMalformedFile(string tail)
    {
        string json = $$"""
            { "bank": "x", "accountType": "chequing", "match": ["A"], "dateColumn": "A",
              "descriptionColumns": ["A"], "amountColumn": "A", {{tail}} }
            """;
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(json));

        InvalidOperationException exception = Assert.ThrowsExactly<InvalidOperationException>(
            () => StatementMappingCatalog.ReadMapping(stream, "broken.json"));

        StringAssert.Contains(exception.Message, "broken.json");
    }

    [TestMethod]
    public void ReadMapping_RejectsANullDocument()
    {
        using MemoryStream stream = new(Encoding.UTF8.GetBytes("null"));

        InvalidOperationException exception = Assert.ThrowsExactly<InvalidOperationException>(
            () => StatementMappingCatalog.ReadMapping(stream, "empty.json"));

        StringAssert.Contains(exception.Message, "empty.json");
    }

    [TestMethod]
    [DataRow("Chequing", "rbc/chequing")]
    [DataRow("Savings", "rbc/savings")]
    [DataRow("chequing", "rbc/chequing")]
    [DataRow(" Visa ", "rbc/credit_card")]
    [DataRow("MasterCard", "rbc/credit_card")]
    [DataRow("Loan", null)]
    public void Find_TellsRbcExportsApartByAccountType(string accountType, string? expected)
    {
        string[] firstRow = [accountType, "0", "10/1/2026", "", "X", "", "1", ""];

        Assert.AreEqual(expected, Catalog.Find(RbcHeader, firstRow)?.Name);
    }

    [TestMethod]
    public void Find_NeedsAFirstRow_WhenBanksShareAHeader()
    {
        Assert.IsNull(Catalog.Find(RbcHeader, null));
    }

    [TestMethod]
    public void MatchesHeader_IgnoresTheAccountType_AndNamesItsColumn()
    {
        Assert.IsTrue(Catalog.MatchesHeader(RbcHeader, out string? column));
        Assert.AreEqual("Account Type", column);
        Assert.IsTrue(Catalog.MatchesHeader(ScotiabankChequingHeader, out column));
        Assert.IsNull(column);
        Assert.IsFalse(Catalog.MatchesHeader(["Date", "Amount"], out column));
        Assert.IsNull(column);
    }

    [TestMethod]
    public void Find_MatchesOnTheHeaderAlone_AndIgnoresExtraColumns()
    {
        Assert.AreEqual("scotiabank/chequing", Catalog.Find([.. ScotiabankChequingHeader, "Extra"], null)?.Name);
    }

    [TestMethod]
    public void Find_ReturnsNull_WhenAHeaderCellIsMissing()
    {
        Assert.IsNull(Catalog.Find(ScotiabankChequingHeader.Where(cell => cell != "Balance").ToList(), null));
    }

    [TestMethod]
    public void Constructor_RejectsTwoMappingsThatCanDescribeOneFile()
    {
        StatementMapping mapping = Catalog.Mappings.Single(candidate => candidate.Name == "scotiabank/chequing");
        StatementMapping generic = mapping with
        {
            Bank = "other",
            Match = ["Date", "Description", "Amount"],
            DescriptionColumns = ["Description"],
            BalanceColumn = null,
        };

        Assert.ThrowsExactly<InvalidOperationException>(() => new StatementMappingCatalog([mapping, mapping with { Bank = "other" }]));
        Assert.ThrowsExactly<InvalidOperationException>(() => new StatementMappingCatalog([mapping, generic]));
    }

    [TestMethod]
    public void Constructor_RejectsNestedMappings_WhoseAccountTypeColumnsDiffer()
    {
        StatementMapping mapping = Catalog.Mappings.Single(candidate => candidate.Name == "rbc/chequing");
        StatementMapping other = mapping with { Bank = "other", AccountTypeColumn = "Cheque Number", AccountTypeValues = ["x"] };

        Assert.ThrowsExactly<InvalidOperationException>(() => new StatementMappingCatalog([mapping, other]));
    }

    [TestMethod]
    public void Find_Throws_WhenAFileCarriesTheColumnsOfTwoExports()
    {
        StatementFormatException exception = Assert.ThrowsExactly<StatementFormatException>(
            () => Catalog.Find([.. ScotiabankChequingHeader, "Status", "Associated card number"], null));

        Assert.AreEqual(StatementFormatException.UnknownFormat, exception.ErrorCode);
    }

    [TestMethod]
    public void Container_BuildsTheCatalogFromTheEmbeddedMappings()
    {
        using ServiceProvider provider = new ServiceCollection()
            .AddSingleton<StatementMappingCatalog>()
            .BuildServiceProvider();

        Assert.AreEqual(5, provider.GetRequiredService<StatementMappingCatalog>().Mappings.Count);
    }
}

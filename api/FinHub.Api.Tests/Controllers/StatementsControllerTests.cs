using FinHub.Api.Controllers;
using FinHub.Api.DTOs.Statement;

namespace FinHub.Api.Tests.Controllers;

[TestClass]
public sealed class StatementsControllerTests
{
    private static readonly ParsedTransaction Coffee = new(2, new DateOnly(2026, 10, 20), "Coffee", -4.5m, null);

    [TestMethod]
    public void ToResponse_FlagsTheCandidatesTheAccountAlreadyHolds()
    {
        ParsedStatement statement = new("rbc", "chequing", [Coffee, Coffee with { StatementLine = 3, Description = "Tea" }], 1);
        HashSet<StatementsController.TransactionKey> existing = [new(Coffee.TransactionDate, "Coffee", -4.50m)];

        ParsedStatementResponse response = StatementsController.ToResponse(statement, existing);

        Assert.AreEqual("rbc", response.Bank);
        Assert.AreEqual("chequing", response.AccountType);
        Assert.AreEqual(1, response.SkippedRows);
        Assert.AreEqual(
            new ParsedTransactionResponse(2, Coffee.TransactionDate, "Coffee", -4.5m, null, true),
            response.Transactions[0]);
        Assert.AreEqual(
            new ParsedTransactionResponse(3, Coffee.TransactionDate, "Tea", -4.5m, null, false),
            response.Transactions[1]);
    }

    [TestMethod]
    public void ToResponse_MatchesTheDescriptionExactly()
    {
        ParsedStatement statement = new("rbc", "chequing", [Coffee], 0);
        HashSet<StatementsController.TransactionKey> existing = [new(Coffee.TransactionDate, "coffee", -4.5m)];

        Assert.IsFalse(StatementsController.ToResponse(statement, existing).Transactions[0].PotentialDuplicate);
    }

    [TestMethod]
    public void KeyOf_RoundsTheAmountTheWayItWouldBeSaved()
    {
        Assert.AreEqual(1.01m, StatementsController.KeyOf(Coffee with { Amount = 1.005m }).Amount);
        Assert.AreEqual(-4.5m, StatementsController.KeyOf(Coffee).Amount);
    }
}

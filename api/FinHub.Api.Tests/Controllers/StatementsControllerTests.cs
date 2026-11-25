using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FinHub.Api.Controllers;
using FinHub.Api.DTOs.Statement;
using FinHub.Api.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FinHub.Api.Tests.Controllers;

[TestClass]
public sealed class StatementsControllerTests
{
    private static readonly ParsedTransaction Coffee = new(2, new DateOnly(2026, 10, 20), "Coffee", -4.5m, null);

    private static readonly ImportTransactionRequest CoffeeLine = new(2, Coffee.TransactionDate, "Coffee", -4.5m, null, null);

    [TestMethod]
    public async Task Preview_AllowsGuestsAndReturnsOnlyParsedCandidates()
    {
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("RateLimiting:GuestPreview:PermitLimit", "1");
                builder.UseSetting("RateLimiting:GuestPreview:WindowSeconds", "60");
            });
        using HttpClient client = factory.CreateClient();
        using MultipartFormDataContent request = CreateGuestPreviewRequest();

        using HttpResponseMessage response = await client.PostAsync(
            "/api/statements/preview",
            request);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        JsonElement root = body.RootElement;
        Assert.AreEqual("rbc", root.GetProperty("bank").GetString());
        Assert.IsTrue(root.GetProperty("totalTransactions").GetInt32() > 0);
        Assert.IsTrue(root.GetProperty("transactions").GetArrayLength() <= 20);
        JsonElement transaction = root.GetProperty("transactions")[0];
        Assert.IsFalse(transaction.TryGetProperty("potentialDuplicate", out _));
        Assert.IsFalse(transaction.TryGetProperty("categoryId", out _));

        using MultipartFormDataContent rateLimitedRequest = CreateGuestPreviewRequest();
        using HttpResponseMessage rateLimitedResponse = await client.PostAsync(
            "/api/statements/preview",
            rateLimitedRequest);
        Assert.AreEqual(HttpStatusCode.TooManyRequests, rateLimitedResponse.StatusCode);
    }

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
    public void ToGuestPreviewResponse_ReportsAllCandidatesAndReturnsAtMostTwenty()
    {
        ParsedTransaction[] transactions = Enumerable.Range(1, 21)
            .Select(line => Coffee with { StatementLine = line })
            .ToArray();
        ParsedStatement statement = new("rbc", "chequing", transactions, 0);

        PreviewStatementResponse response = StatementsController.ToGuestPreviewResponse(statement);

        Assert.AreEqual(21, response.TotalTransactions);
        Assert.AreEqual(20, response.Transactions.Count);
        Assert.AreEqual(1, response.Transactions[0].StatementLine);
        Assert.AreEqual(20, response.Transactions[^1].StatementLine);
    }

    private static MultipartFormDataContent CreateGuestPreviewRequest()
    {
        string filePath = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "rbc-chequing-statement.csv");
        ByteArrayContent fileContent = new(File.ReadAllBytes(filePath));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");

        MultipartFormDataContent request = new();
        request.Add(fileContent, "file", "rbc-chequing-statement.csv");
        return request;
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

    [TestMethod]
    public void KeyOf_AgreesBetweenACandidateAndTheLineThatSavesIt()
    {
        ParsedTransaction candidate = Coffee with { Amount = 1.005m };
        ImportTransactionRequest line = CoffeeLine with { Description = " Coffee ", Amount = 1.005m };

        Assert.AreEqual(StatementsController.KeyOf(candidate), StatementsController.KeyOf(line));
    }

    [TestMethod]
    public void UnconfirmedDuplicates_ListsTheLinesTheAccountHoldsUnlessConfirmed()
    {
        ImportTransactionRequest confirmed = CoffeeLine with { StatementLine = 3, ConfirmPotentialDuplicate = true };
        ImportTransactionRequest fresh = CoffeeLine with { StatementLine = 4, Description = "Tea" };
        HashSet<StatementsController.TransactionKey> existing = [new(Coffee.TransactionDate, "Coffee", -4.50m)];

        List<int> unconfirmed = StatementsController.UnconfirmedDuplicates([CoffeeLine, confirmed, fresh], existing);

        CollectionAssert.AreEqual(new[] { 2 }, unconfirmed);
    }

    [TestMethod]
    public void ToTransaction_StoresTheLineTheWayTheManualEndpointWould()
    {
        Guid accountId = Guid.NewGuid();
        Guid categoryId = Guid.NewGuid();
        ImportTransactionRequest line = new(7, new DateOnly(2026, 10, 22), "  bill payment  ", -83.415m, 2039.125m, categoryId);

        Transaction transaction = StatementsController.ToTransaction(line, accountId);

        Assert.AreNotEqual(Guid.Empty, transaction.Id);
        Assert.AreEqual(accountId, transaction.AccountId);
        Assert.AreEqual(categoryId, transaction.CategoryId);
        Assert.AreEqual(new DateOnly(2026, 10, 22), transaction.TransactionDate);
        Assert.AreEqual("bill payment", transaction.Description);
        Assert.AreEqual(-83.42m, transaction.Amount);
        Assert.AreEqual(2039.13m, transaction.BalanceAfter);
        Assert.AreEqual(7, transaction.StatementLine);
    }

    [TestMethod]
    public void ToTransaction_KeepsAMissingBalance()
    {
        Assert.IsNull(StatementsController.ToTransaction(CoffeeLine, Guid.NewGuid()).BalanceAfter);
    }

    [TestMethod]
    public void ToTransaction_UsesAnEvaluatedCategoryWithoutReplacingAnExplicitCategory()
    {
        Guid accountId = Guid.NewGuid();
        Guid explicitCategoryId = Guid.NewGuid();
        Guid evaluatedCategoryId = Guid.NewGuid();

        Transaction explicitTransaction = StatementsController.ToTransaction(
            CoffeeLine with { CategoryId = explicitCategoryId },
            accountId,
            evaluatedCategoryId);
        Transaction uncategorized = StatementsController.ToTransaction(
            CoffeeLine,
            accountId,
            evaluatedCategoryId);

        Assert.AreEqual(explicitCategoryId, explicitTransaction.CategoryId);
        Assert.AreEqual(evaluatedCategoryId, uncategorized.CategoryId);
    }
}

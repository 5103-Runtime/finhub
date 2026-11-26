using System.Net;
using System.Text;
using FinHub.Api.Constants;
using FinHub.Api.Controllers;
using FinHub.Api.Data;
using FinHub.Api.DTOs.Statement;
using FinHub.Api.DTOs.Transaction;
using FinHub.Api.DTOs.Transfer;
using FinHub.Api.Models;
using FinHub.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace FinHub.Api.Tests.Controllers;

[TestClass]
public sealed class CategorizationExplicitNoneControllerTests
{
    private static readonly Guid UserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid FromAccountId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid ToAccountId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid RuleCategoryId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    [TestMethod]
    public async Task TransactionPreview_ExplicitNone_DoesNotEvaluateRules()
    {
        using FinHubDbContext dbContext = CreateDbContext();
        dbContext.FinancialAccounts.Add(Account(FromAccountId));
        await dbContext.SaveChangesAsync();
        RecordingCategorizationService service = new(RuleCategoryId);
        TransactionsController controller = CreateTransactionController(dbContext, service);
        CreateTransactionRequest request = new(
            FromAccountId,
            null,
            new DateOnly(2026, 11, 18),
            "Coffee",
            -5m,
            false,
            true);

        ActionResult<TransactionCategorizationPreviewResponse> result =
            await controller.PreviewCategorization(request, CancellationToken.None);

        Assert.IsInstanceOfType<OkObjectResult>(result.Result);
        Assert.AreEqual(0, service.SingleEvaluationCount);
        TransactionCategorizationPreviewResponse? response =
            (result.Result as OkObjectResult)?.Value as TransactionCategorizationPreviewResponse;
        Assert.IsNull(response?.CategoryId);
        Assert.IsFalse(response?.AutomaticCategoryApplied);
    }

    [TestMethod]
    public async Task TransactionPreview_ExplicitMissingCategory_ReturnsNotFound()
    {
        using FinHubDbContext dbContext = CreateDbContext();
        dbContext.FinancialAccounts.Add(Account(FromAccountId));
        await dbContext.SaveChangesAsync();
        RecordingCategorizationService service = new(RuleCategoryId);
        TransactionsController controller = CreateTransactionController(dbContext, service);
        CreateTransactionRequest request = new(
            FromAccountId,
            RuleCategoryId,
            new DateOnly(2026, 11, 18),
            "Coffee",
            -5m,
            false,
            true);

        ActionResult<TransactionCategorizationPreviewResponse> result =
            await controller.PreviewCategorization(request, CancellationToken.None);

        Assert.IsInstanceOfType<NotFoundObjectResult>(result.Result);
        Assert.AreEqual(0, service.SingleEvaluationCount);
    }

    [TestMethod]
    public async Task TransactionCreate_ExplicitNone_DoesNotEvaluateOrSaveARuleCategory()
    {
        using FinHubDbContext dbContext = CreateDbContext();
        dbContext.FinancialAccounts.Add(Account(FromAccountId));
        await dbContext.SaveChangesAsync();
        RecordingCategorizationService service = new(RuleCategoryId);
        TransactionsController controller = CreateTransactionController(dbContext, service);
        CreateTransactionRequest request = new(
            FromAccountId,
            null,
            new DateOnly(2026, 11, 18),
            "Coffee",
            -5m,
            false,
            true);

        await controller.Create(request, CancellationToken.None);

        Assert.AreEqual(0, service.SingleEvaluationCount);
        Assert.IsNull(dbContext.Transactions.Single().CategoryId);
    }

    [TestMethod]
    public async Task StatementImport_ExplicitNone_DoesNotEvaluateOrSaveARuleCategory()
    {
        using FinHubDbContext dbContext = CreateDbContext();
        dbContext.FinancialAccounts.Add(Account(FromAccountId));
        await dbContext.SaveChangesAsync();
        RecordingCategorizationService service = new(RuleCategoryId);
        StatementsController controller = CreateStatementController(dbContext, service);
        ImportStatementRequest request = new(
            FromAccountId,
            [
                new ImportTransactionRequest(
                    2,
                    new DateOnly(2026, 11, 18),
                    "Coffee",
                    -5m,
                    null,
                    null,
                    false,
                    true)
            ]);

        await controller.Import(request, CancellationToken.None);

        Assert.AreEqual(0, service.BatchEvaluationCount);
        Assert.IsNull(dbContext.Transactions.Single().CategoryId);
    }

    [TestMethod]
    public async Task TransferPreview_ExplicitNone_DoesNotEvaluateRules()
    {
        using FinHubDbContext dbContext = CreateDbContext();
        dbContext.FinancialAccounts.AddRange(
            Account(FromAccountId),
            Account(ToAccountId));
        await dbContext.SaveChangesAsync();
        RecordingCategorizationService service = new(RuleCategoryId);
        TransfersController controller = CreateTransferController(dbContext, service);
        CreateTransferRequest request = new(
            FromAccountId,
            ToAccountId,
            new DateOnly(2026, 11, 18),
            "Payment",
            25m,
            SourceCategoryWasExplicitlySet: true,
            DestinationCategoryWasExplicitlySet: true);

        ActionResult<TransferCategorizationPreviewResponse> result =
            await controller.PreviewCategorization(request, CancellationToken.None);

        Assert.IsInstanceOfType<OkObjectResult>(result.Result);
        Assert.AreEqual(0, service.MatchEvaluationCount);
        TransferCategorizationPreviewResponse? response =
            (result.Result as OkObjectResult)?.Value as TransferCategorizationPreviewResponse;
        Assert.IsNull(response?.OutgoingCategoryId);
        Assert.IsNull(response?.IncomingCategoryId);
        Assert.IsFalse(response?.AutomaticCategoryApplied);
    }

    [TestMethod]
    public async Task TransferCreate_ExplicitNone_DoesNotEvaluateOrSaveRuleCategories()
    {
        using FinHubDbContext dbContext = CreateDbContext();
        dbContext.FinancialAccounts.AddRange(
            Account(FromAccountId),
            Account(ToAccountId));
        await dbContext.SaveChangesAsync();
        RecordingCategorizationService service = new(RuleCategoryId);
        TransfersController controller = CreateTransferController(dbContext, service);
        CreateTransferRequest request = new(
            FromAccountId,
            ToAccountId,
            new DateOnly(2026, 11, 18),
            "Payment",
            25m,
            SourceCategoryWasExplicitlySet: true,
            DestinationCategoryWasExplicitlySet: true);

        await controller.Create(request, CancellationToken.None);

        Assert.AreEqual(0, service.MatchEvaluationCount);
        Assert.AreEqual(2, dbContext.Transactions.Count());
        Assert.IsTrue(dbContext.Transactions.ToList().All(transaction => transaction.CategoryId is null));
    }

    private static FinHubDbContext CreateDbContext()
    {
        DbContextOptions<FinHubDbContext> options = new DbContextOptionsBuilder<FinHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new FinHubDbContext(options);
    }

    private static FinancialAccount Account(Guid id) => new()
    {
        Id = id,
        UserId = UserId,
        Name = id == FromAccountId ? "Chequing" : "Credit",
        AccountType = id == FromAccountId ? "chequing" : "credit_card",
        Currency = "CAD"
    };

    private static TransactionsController CreateTransactionController(
        FinHubDbContext dbContext,
        ICategorizationRuleService service) =>
        new(dbContext, CreateAuthClient(), service)
        {
            ControllerContext = CreateControllerContext()
        };

    private static StatementsController CreateStatementController(
        FinHubDbContext dbContext,
        ICategorizationRuleService service) =>
        new(
            dbContext,
            CreateAuthClient(),
            [],
            service,
            new GuestPreviewRateLimiter(1, TimeSpan.FromMinutes(1), TimeProvider.System))
        {
            ControllerContext = CreateControllerContext()
        };

    private static TransfersController CreateTransferController(
        FinHubDbContext dbContext,
        ICategorizationRuleService service) =>
        new(dbContext, CreateAuthClient(), service)
        {
            ControllerContext = CreateControllerContext()
        };

    private static ControllerContext CreateControllerContext()
    {
        DefaultHttpContext httpContext = new();
        httpContext.Request.Headers.Cookie =
            $"{AuthCookies.AccessToken}=test-access-token";
        return new ControllerContext { HttpContext = httpContext };
    }

    private static SupabaseAuthClient CreateAuthClient()
    {
        AuthHandler handler = new(UserId);
        HttpClient httpClient = new(handler);
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Supabase:Url"] = "https://supabase.test",
                ["Supabase:PublishableKey"] = "test-key"
            })
            .Build();
        return new SupabaseAuthClient(
            httpClient,
            configuration,
            NullLogger<SupabaseAuthClient>.Instance);
    }

    private sealed class AuthHandler(Guid userId) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            HttpResponseMessage response = new(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    $$"""{"id":"{{userId}}"}""",
                    Encoding.UTF8,
                    "application/json")
            };
            return Task.FromResult(response);
        }
    }

    private sealed class RecordingCategorizationService(Guid categoryId)
        : ICategorizationRuleService
    {
        public int SingleEvaluationCount { get; private set; }
        public int BatchEvaluationCount { get; private set; }
        public int MatchEvaluationCount { get; private set; }

        public Task<CategorizationMatch?> EvaluateMatchAsync(
            Guid userId,
            Guid accountId,
            string description,
            decimal amount,
            CancellationToken cancellationToken)
        {
            MatchEvaluationCount++;
            return Task.FromResult<CategorizationMatch?>(
                new CategorizationMatch(categoryId));
        }

        public Task<Guid?> EvaluateAsync(
            Guid userId,
            Guid accountId,
            string description,
            decimal amount,
            CancellationToken cancellationToken)
        {
            SingleEvaluationCount++;
            return Task.FromResult<Guid?>(categoryId);
        }

        public Task<IReadOnlyList<Guid?>> EvaluateAsync(
            Guid userId,
            Guid accountId,
            IReadOnlyList<CategorizationCandidate> transactions,
            CancellationToken cancellationToken)
        {
            BatchEvaluationCount++;
            return Task.FromResult<IReadOnlyList<Guid?>>(
                transactions.Select(_ => (Guid?)categoryId).ToList());
        }
    }
}

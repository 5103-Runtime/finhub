using System.Net;
using System.Text;
using System.Text.Json;
using FinHub.Api.Constants;
using FinHub.Api.Controllers;
using FinHub.Api.Data;
using FinHub.Api.DTOs.Admin;
using FinHub.Api.Models;
using FinHub.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace FinHub.Api.Tests.Controllers;

[TestClass]
public sealed class UserManagementControllerTests
{
    private static readonly Guid AdministratorId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid UserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [TestMethod]
    public async Task List_ReturnsAllUsers_ForAdministrator()
    {
        await using FinHubDbContext dbContext = CreateDbContext();
        dbContext.Profiles.AddRange(
            CreateProfile(AdministratorId, "administrator"),
            CreateProfile(UserId, "user"));
        dbContext.AuthUsers.AddRange(
            CreateAuthUser(AdministratorId, "admin@example.com"),
            CreateAuthUser(UserId, "user@example.com"));
        await dbContext.SaveChangesAsync();

        UserManagementController controller = CreateController(dbContext, AdministratorId);

        ActionResult<IReadOnlyList<UserManagementResponse>> result =
            await controller.List(null, null, CancellationToken.None);

        Assert.IsInstanceOfType<OkObjectResult>(result.Result);
        IReadOnlyList<UserManagementResponse> users =
            ((OkObjectResult)result.Result!).Value as IReadOnlyList<UserManagementResponse>
            ?? throw new AssertFailedException("The response did not contain user responses.");
        Assert.AreEqual(2, users.Count);
        Assert.AreEqual("admin@example.com", users.Single(user => user.Id == AdministratorId).Email);
        Assert.AreEqual("user@example.com", users.Single(user => user.Id == UserId).Email);
    }

    [TestMethod]
    [DataRow("asc", "a-user@example.com")]
    [DataRow("desc", "z-admin@example.com")]
    [DataRow(null, "z-admin@example.com")]
    public async Task List_SortsByEmail(string? sortOrder, string firstEmail)
    {
        await using FinHubDbContext dbContext = CreateDbContext();
        Profile administrator = CreateProfile(AdministratorId, "administrator");
        administrator.CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        Profile user = CreateProfile(UserId, "user");
        user.CreatedAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        dbContext.Profiles.AddRange(administrator, user);
        dbContext.AuthUsers.AddRange(
            CreateAuthUser(AdministratorId, "z-admin@example.com"),
            CreateAuthUser(UserId, "a-user@example.com"));
        await dbContext.SaveChangesAsync();

        UserManagementController controller = CreateController(dbContext, AdministratorId);

        ActionResult<IReadOnlyList<UserManagementResponse>> result =
            await controller.List("email", sortOrder, CancellationToken.None);

        Assert.IsInstanceOfType<OkObjectResult>(result.Result);
        IReadOnlyList<UserManagementResponse> users =
            ((OkObjectResult)result.Result!).Value as IReadOnlyList<UserManagementResponse>
            ?? throw new AssertFailedException("The response did not contain user responses.");
        Assert.AreEqual(firstEmail, users[0].Email);
    }

    [TestMethod]
    [DataRow(null, "asc", "missing_sort_field")]
    [DataRow("emailAddress", "asc", "invalid_sort_field")]
    [DataRow("email", "sideways", "invalid_sort_order")]
    public async Task List_RejectsInvalidSorting(
        string? sortBy,
        string? sortOrder,
        string expectedErrorCode)
    {
        await using FinHubDbContext dbContext = CreateDbContext();
        dbContext.Profiles.Add(CreateProfile(AdministratorId, "administrator"));
        await dbContext.SaveChangesAsync();

        UserManagementController controller = CreateController(dbContext, AdministratorId);

        ActionResult<IReadOnlyList<UserManagementResponse>> result =
            await controller.List(sortBy, sortOrder, CancellationToken.None);

        Assert.IsInstanceOfType<BadRequestObjectResult>(result.Result);
        string body = JsonSerializer.Serialize(((BadRequestObjectResult)result.Result!).Value);
        StringAssert.Contains(body, expectedErrorCode);
    }

    [TestMethod]
    public async Task List_ReturnsForbidden_ForNormalUser()
    {
        await using FinHubDbContext dbContext = CreateDbContext();
        dbContext.Profiles.Add(CreateProfile(UserId, "user"));
        await dbContext.SaveChangesAsync();

        UserManagementController controller = CreateController(dbContext, UserId);

        ActionResult<IReadOnlyList<UserManagementResponse>> result =
            await controller.List(null, null, CancellationToken.None);

        ObjectResult? response = result.Result as ObjectResult;
        Assert.IsNotNull(response);
        Assert.AreEqual(StatusCodes.Status403Forbidden, response.StatusCode);
        StringAssert.Contains(response.Value?.ToString(), "administrator_required");
    }

    [TestMethod]
    public async Task Get_ReturnsOneUser_ForAdministrator()
    {
        await using FinHubDbContext dbContext = CreateDbContext();
        dbContext.Profiles.AddRange(
            CreateProfile(AdministratorId, "administrator"),
            CreateProfile(UserId, "user"));
        dbContext.AuthUsers.AddRange(
            CreateAuthUser(AdministratorId, "admin@example.com"),
            CreateAuthUser(UserId, "user@example.com"));
        await dbContext.SaveChangesAsync();

        UserManagementController controller = CreateController(dbContext, AdministratorId);

        ActionResult<UserManagementResponse> result =
            await controller.Get(UserId, CancellationToken.None);

        Assert.IsInstanceOfType<OkObjectResult>(result.Result);
        UserManagementResponse user =
            ((OkObjectResult)result.Result!).Value as UserManagementResponse
            ?? throw new AssertFailedException("The response did not contain a user.");
        Assert.AreEqual(UserId, user.Id);
        Assert.AreEqual("user@example.com", user.Email);
        Assert.AreEqual("user", user.Role);
    }

    [TestMethod]
    public async Task Get_ReturnsNotFound_WhenUserDoesNotExist()
    {
        await using FinHubDbContext dbContext = CreateDbContext();
        dbContext.Profiles.Add(CreateProfile(AdministratorId, "administrator"));
        await dbContext.SaveChangesAsync();

        UserManagementController controller = CreateController(dbContext, AdministratorId);

        ActionResult<UserManagementResponse> result =
            await controller.Get(Guid.NewGuid(), CancellationToken.None);

        Assert.IsInstanceOfType<NotFoundObjectResult>(result.Result);
    }

    private static FinHubDbContext CreateDbContext()
    {
        DbContextOptions<FinHubDbContext> options = new DbContextOptionsBuilder<FinHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new FinHubDbContext(options);
    }

    private static Profile CreateProfile(Guid id, string role) => new()
    {
        Id = id,
        FirstName = "Test",
        LastName = "User",
        PreferredName = null,
        DateOfBirth = new DateOnly(1990, 1, 1),
        Role = role,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static AuthUser CreateAuthUser(Guid id, string email) => new()
    {
        Id = id,
        Email = email
    };

    private static UserManagementController CreateController(
        FinHubDbContext dbContext,
        Guid authenticatedUserId)
    {
        AuthHandler handler = new(authenticatedUserId);
        HttpClient httpClient = new(handler);
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Supabase:Url"] = "https://supabase.test",
                ["Supabase:PublishableKey"] = "test-key"
            })
            .Build();
        SupabaseAuthClient authClient = new(
            httpClient,
            configuration,
            NullLogger<SupabaseAuthClient>.Instance);

        DefaultHttpContext httpContext = new();
        httpContext.Request.Headers.Cookie =
            $"{AuthCookies.AccessToken}=test-access-token";

        return new UserManagementController(dbContext, authClient)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
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
}

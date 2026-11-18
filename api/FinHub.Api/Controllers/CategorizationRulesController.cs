using FinHub.Api.Data;
using FinHub.Api.DTOs.CategorizationRule;
using FinHub.Api.Models;
using FinHub.Api.Services;
using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FinHub.Api.Controllers;

/// <summary>
/// Controller responsible for managing the authenticated user's categorization rules.
/// </summary>
/// <param name="dbContext">The database context.</param>
/// <param name="authClient">The authentication client.</param>
[ApiController]
[Route("api/categorization-rules")]
public sealed class CategorizationRulesController(
    FinHubDbContext dbContext,
    SupabaseAuthClient authClient) : AuthenticatedControllerBase(authClient)
{
    /// <summary>
    /// Lists the authenticated user's categorization rules in priority order.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The user's categorization rules.</returns>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategorizationRuleResponse>>> List(
        CancellationToken cancellationToken)
    {
        (Guid? userId, ActionResult? failure) = await ResolveCurrentUserAsync(cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        List<CategorizationRuleResponse> rules = await dbContext.CategorizationRules
            .AsNoTracking()
            .Where(rule => rule.UserId == userId!.Value)
            .OrderBy(rule => rule.Priority)
            .ThenBy(rule => rule.CreatedAt)
            .Select(rule => ToResponse(rule))
            .ToListAsync(cancellationToken);

        return Ok(rules);
    }

    /// <summary>
    /// Reorders all categorization rules owned by the authenticated user.
    /// </summary>
    /// <param name="request">The complete rule order, from highest to lowest priority.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The rules in their new priority order.</returns>
    [HttpPut("reorder")]
    public async Task<ActionResult<IReadOnlyList<CategorizationRuleResponse>>> Reorder(
        [FromBody] ReorderCategorizationRulesRequest request,
        CancellationToken cancellationToken)
    {
        (Guid? userId, ActionResult? failure) = await ResolveCurrentUserAsync(cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        if (request.RuleIds is null)
        {
            return InvalidRuleOrder();
        }

        Guid currentUserId = userId!.Value;
        HashSet<Guid> requestedIds = request.RuleIds.ToHashSet();
        if (requestedIds.Count != request.RuleIds.Count)
        {
            return InvalidRuleOrder();
        }

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);
        List<CategorizationRule> rules = await dbContext.CategorizationRules
            .Where(rule => rule.UserId == currentUserId)
            .ToListAsync(cancellationToken);
        if (rules.Count != requestedIds.Count
            || rules.Any(rule => !requestedIds.Contains(rule.Id)))
        {
            return InvalidRuleOrder();
        }

        Dictionary<Guid, CategorizationRule> rulesById =
            rules.ToDictionary(rule => rule.Id);
        try
        {
            RenumberRules(request.RuleIds
                .Select(id => rulesById[id])
                .ToList());

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            return PriorityConflict();
        }
        catch (DbUpdateException exception) when (IsSerializationFailure(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            return ReorderConflict();
        }
        catch (PostgresException exception) when (
            exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            await transaction.RollbackAsync(cancellationToken);
            return PriorityConflict();
        }
        catch (PostgresException exception) when (exception.SqlState == "40001")
        {
            await transaction.RollbackAsync(cancellationToken);
            return ReorderConflict();
        }

        List<CategorizationRuleResponse> reordered = request.RuleIds
            .Select(id => rulesById[id])
            .Select(ToResponse)
            .ToList();
        return Ok(reordered);
    }

    /// <summary>
    /// Retrieves one categorization rule owned by the authenticated user.
    /// </summary>
    /// <param name="id">The rule identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The categorization rule.</returns>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CategorizationRuleResponse>> Get(
        Guid id,
        CancellationToken cancellationToken)
    {
        (Guid? userId, ActionResult? failure) = await ResolveCurrentUserAsync(cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        CategorizationRule? rule = await FindRuleAsync(id, userId!.Value, track: false, cancellationToken);
        return rule is null ? RuleNotFound() : Ok(ToResponse(rule));
    }

    /// <summary>
    /// Creates a categorization rule for the authenticated user.
    /// </summary>
    /// <param name="request">The rule request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created categorization rule.</returns>
    [HttpPost]
    public async Task<ActionResult<CategorizationRuleResponse>> Create(
        [FromBody] CreateCategorizationRuleRequest request,
        CancellationToken cancellationToken)
    {
        (Guid? userId, ActionResult? failure) = await ResolveCurrentUserAsync(cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        Guid currentUserId = userId!.Value;
        ActionResult? referenceFailure = await ValidateReferencesAsync(
            request.CategoryId!.Value,
            request.AccountId,
            currentUserId,
            cancellationToken);
        if (referenceFailure is not null)
        {
            return referenceFailure;
        }

        CategorizationRule rule = new()
        {
            Id = Guid.NewGuid(),
            UserId = currentUserId,
            CategoryId = request.CategoryId.Value,
            AccountId = request.AccountId,
            DescriptionContains = NormalizeDescription(request.DescriptionContains),
            AmountDirection = request.AmountDirection,
            Priority = request.Priority!.Value,
            IsEnabled = true
        };

        if (await HasPriorityConflictAsync(currentUserId, rule.Priority, null, cancellationToken))
        {
            return PriorityConflict();
        }

        dbContext.CategorizationRules.Add(rule);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            return PriorityConflict();
        }

        return CreatedAtAction(nameof(Get), new { id = rule.Id }, ToResponse(rule));
    }

    /// <summary>
    /// Updates one categorization rule owned by the authenticated user.
    /// </summary>
    /// <param name="id">The rule identifier.</param>
    /// <param name="request">The rule request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated categorization rule.</returns>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CategorizationRuleResponse>> Update(
        Guid id,
        [FromBody] UpdateCategorizationRuleRequest request,
        CancellationToken cancellationToken)
    {
        (Guid? userId, ActionResult? failure) = await ResolveCurrentUserAsync(cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        Guid currentUserId = userId!.Value;
        CategorizationRule? rule = await FindRuleAsync(id, currentUserId, track: true, cancellationToken);
        if (rule is null)
        {
            return RuleNotFound();
        }

        ActionResult? referenceFailure = await ValidateReferencesAsync(
            request.CategoryId!.Value,
            request.AccountId,
            currentUserId,
            cancellationToken);
        if (referenceFailure is not null)
        {
            return referenceFailure;
        }

        if (await HasPriorityConflictAsync(currentUserId, request.Priority!.Value, id, cancellationToken))
        {
            return PriorityConflict();
        }

        rule.CategoryId = request.CategoryId.Value;
        rule.AccountId = request.AccountId;
        rule.DescriptionContains = NormalizeDescription(request.DescriptionContains);
        rule.AmountDirection = request.AmountDirection;
        rule.Priority = request.Priority.Value;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            return PriorityConflict();
        }

        return Ok(ToResponse(rule));
    }

    /// <summary>
    /// Deletes one categorization rule owned by the authenticated user.
    /// </summary>
    /// <param name="id">The rule identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>No content when the rule was deleted.</returns>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        (Guid? userId, ActionResult? failure) = await ResolveCurrentUserAsync(cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        CategorizationRule? rule = await FindRuleAsync(id, userId!.Value, track: true, cancellationToken);
        if (rule is null)
        {
            return RuleNotFound();
        }

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);
        try
        {
            dbContext.CategorizationRules.Remove(rule);

            List<CategorizationRule> remainingRules = await dbContext.CategorizationRules
                .Where(remainingRule => remainingRule.UserId == userId.Value
                    && remainingRule.Id != rule.Id)
                .OrderBy(remainingRule => remainingRule.Priority)
                .ThenBy(remainingRule => remainingRule.CreatedAt)
                .ThenBy(remainingRule => remainingRule.Id)
                .ToListAsync(cancellationToken);
            RenumberRules(remainingRules);

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            return PriorityConflict();
        }
        catch (DbUpdateException exception) when (IsSerializationFailure(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            return ReorderConflict();
        }
        catch (PostgresException exception) when (exception.SqlState == "40001")
        {
            await transaction.RollbackAsync(cancellationToken);
            return ReorderConflict();
        }

        return NoContent();
    }

    /// <summary>
    /// Enables one categorization rule owned by the authenticated user.
    /// </summary>
    /// <param name="id">The rule identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The enabled categorization rule.</returns>
    [HttpPost("{id:guid}/enable")]
    public Task<ActionResult<CategorizationRuleResponse>> Enable(
        Guid id,
        CancellationToken cancellationToken) =>
        SetEnabledAsync(id, true, cancellationToken);

    /// <summary>
    /// Disables one categorization rule owned by the authenticated user.
    /// </summary>
    /// <param name="id">The rule identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The disabled categorization rule.</returns>
    [HttpPost("{id:guid}/disable")]
    public Task<ActionResult<CategorizationRuleResponse>> Disable(
        Guid id,
        CancellationToken cancellationToken) =>
        SetEnabledAsync(id, false, cancellationToken);

    /// <summary>
    /// Changes the enabled state of a user's rule.
    /// </summary>
    private async Task<ActionResult<CategorizationRuleResponse>> SetEnabledAsync(
        Guid id,
        bool isEnabled,
        CancellationToken cancellationToken)
    {
        (Guid? userId, ActionResult? failure) = await ResolveCurrentUserAsync(cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        CategorizationRule? rule = await FindRuleAsync(id, userId!.Value, track: true, cancellationToken);
        if (rule is null)
        {
            return RuleNotFound();
        }

        rule.IsEnabled = isEnabled;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(rule));
    }

    /// <summary>
    /// Checks that a category is visible and an account belongs to the user.
    /// </summary>
    private async Task<ActionResult?> ValidateReferencesAsync(
        Guid categoryId,
        Guid? accountId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        bool categoryExists = await dbContext.Categories
            .AsNoTracking()
            .AnyAsync(category => category.Id == categoryId
                && (category.IsSystem || category.UserId == userId), cancellationToken);
        if (!categoryExists)
        {
            return CategoryNotFound();
        }

        if (accountId is not null
            && !await dbContext.FinancialAccounts
                .AsNoTracking()
                .AnyAsync(account => account.Id == accountId && account.UserId == userId, cancellationToken))
        {
            return AccountNotFound();
        }

        return null;
    }

    /// <summary>
    /// Checks whether another rule already uses the requested user priority.
    /// </summary>
    private Task<bool> HasPriorityConflictAsync(
        Guid userId,
        int priority,
        Guid? excludedRuleId,
        CancellationToken cancellationToken) =>
        dbContext.CategorizationRules
            .AsNoTracking()
            .AnyAsync(
                rule => rule.UserId == userId
                    && rule.Priority == priority
                    && (excludedRuleId == null || rule.Id != excludedRuleId.Value),
                cancellationToken);

    /// <summary>
    /// Finds a rule owned by the user.
    /// </summary>
    private Task<CategorizationRule?> FindRuleAsync(
        Guid id,
        Guid userId,
        bool track,
        CancellationToken cancellationToken)
    {
        IQueryable<CategorizationRule> rules = track
            ? dbContext.CategorizationRules
            : dbContext.CategorizationRules.AsNoTracking();

        return rules.SingleOrDefaultAsync(
            rule => rule.Id == id && rule.UserId == userId,
            cancellationToken);
    }

    /// <summary>
    /// Trims a description criterion and clears it when it contains only whitespace.
    /// </summary>
    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    /// <summary>
    /// Assigns consecutive priorities in the supplied order.
    /// </summary>
    /// <param name="rules">Rules ordered from highest to lowest priority.</param>
    internal static void RenumberRules(IReadOnlyList<CategorizationRule> rules)
    {
        for (int index = 0; index < rules.Count; index++)
        {
            rules[index].Priority = index + 1;
        }
    }

    /// <summary>
    /// Converts a rule to its response representation.
    /// </summary>
    private static CategorizationRuleResponse ToResponse(CategorizationRule rule) => new(
        rule.Id,
        rule.CategoryId,
        rule.AccountId,
        rule.DescriptionContains,
        rule.AmountDirection,
        rule.Priority,
        rule.IsEnabled,
        rule.CreatedAt,
        rule.UpdatedAt);

    /// <summary>
    /// Creates the response returned when no user-owned rule matches the ID.
    /// </summary>
    private NotFoundObjectResult RuleNotFound() => NotFound(new
    {
        error = "The categorization rule could not be found.",
        errorCode = "categorization_rule_not_found"
    });

    /// <summary>
    /// Creates the response returned when a referenced category is not visible.
    /// </summary>
    private NotFoundObjectResult CategoryNotFound() => NotFound(new
    {
        error = "The selected category could not be found.",
        errorCode = "category_not_found"
    });

    /// <summary>
    /// Creates the response returned when a referenced account is not owned by the user.
    /// </summary>
    private NotFoundObjectResult AccountNotFound() => NotFound(new
    {
        error = "The selected account could not be found.",
        errorCode = "account_not_found"
    });

    /// <summary>
    /// Creates the response returned when a priority is already in use.
    /// </summary>
    private ConflictObjectResult PriorityConflict() => Conflict(new
    {
        error = "A categorization rule already uses this priority.",
        errorCode = "categorization_rule_priority_conflict"
    });

    /// <summary>
    /// Creates the response returned when a reorder does not contain the complete valid rule set.
    /// </summary>
    private BadRequestObjectResult InvalidRuleOrder() => BadRequest(new
    {
        error = "The rule order must contain each of the authenticated user's rules exactly once.",
        errorCode = "invalid_categorization_rule_order"
    });

    /// <summary>
    /// Creates the response returned when concurrent rule changes prevent a reorder.
    /// </summary>
    private ConflictObjectResult ReorderConflict() => Conflict(new
    {
        error = "The rules changed while the reorder was being saved. Reload the rules and try again.",
        errorCode = "categorization_rule_reorder_conflict"
    });

    /// <summary>
    /// Determines whether the exception is a unique constraint violation.
    /// </summary>
    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation
        };

    /// <summary>
    /// Determines whether the exception is a PostgreSQL serialization failure.
    /// </summary>
    private static bool IsSerializationFailure(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: "40001"
        };
}

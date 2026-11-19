using FinHub.Api.Models;
using FinHub.Api.Services;

namespace FinHub.Api.Tests.Services;

[TestClass]
public sealed class CategorizationRuleServiceTests
{
    [TestMethod]
    public void Evaluate_ReturnsFirstMatchingRuleInPriorityOrder()
    {
        Guid accountId = Guid.NewGuid();
        Guid firstCategoryId = Guid.NewGuid();
        Guid secondCategoryId = Guid.NewGuid();
        List<CategorizationRule> rules =
        [
            Rule(firstCategoryId, priority: 1, descriptionContains: "GROCERY"),
            Rule(secondCategoryId, priority: 2, descriptionContains: "GROCERY")
        ];

        Guid? result = CategorizationRuleService.Evaluate(
            rules,
            accountId,
            "Weekly GROCERY Store",
            -45m);

        Assert.AreEqual(firstCategoryId, result);
    }

    [TestMethod]
    public void Evaluate_RequiresAllConfiguredCriteria()
    {
        Guid accountId = Guid.NewGuid();
        Guid otherAccountId = Guid.NewGuid();
        Guid categoryId = Guid.NewGuid();
        List<CategorizationRule> rules =
        [
            Rule(
                categoryId,
                priority: 1,
                accountId: otherAccountId,
                descriptionContains: "GROCERY",
                amountDirection: "out")
        ];

        Assert.IsNull(CategorizationRuleService.Evaluate(
            rules,
            accountId,
            "GROCERY Store",
            -45m));
    }

    [TestMethod]
    public void Evaluate_MatchesDescriptionCaseInsensitively()
    {
        Guid accountId = Guid.NewGuid();
        Guid categoryId = Guid.NewGuid();

        Guid? result = CategorizationRuleService.Evaluate(
            [Rule(categoryId, priority: 1, descriptionContains: "coffee")],
            accountId,
            "MORNING COFFEE",
            -5m);

        Assert.AreEqual(categoryId, result);
    }

    [TestMethod]
    public void Evaluate_RequiresTheRuleTextInsideTheDescription()
    {
        Guid accountId = Guid.NewGuid();
        Guid categoryId = Guid.NewGuid();

        Guid? partial = CategorizationRuleService.Evaluate(
            [Rule(categoryId, priority: 1, descriptionContains: "GROCERY")],
            accountId,
            "Grocer",
            -100m);
        Guid? full = CategorizationRuleService.Evaluate(
            [Rule(categoryId, priority: 1, descriptionContains: "Grocer")],
            accountId,
            "Grocery Store",
            -100m);

        Assert.IsNull(partial);
        Assert.AreEqual(categoryId, full);
    }

    [TestMethod]
    public void Evaluate_MatchesIncomingAndOutgoingAmounts()
    {
        Guid accountId = Guid.NewGuid();
        Guid incomeCategoryId = Guid.NewGuid();
        Guid expenseCategoryId = Guid.NewGuid();
        List<CategorizationRule> rules =
        [
            Rule(incomeCategoryId, priority: 1, amountDirection: "in"),
            Rule(expenseCategoryId, priority: 2, amountDirection: "out")
        ];

        Assert.AreEqual(incomeCategoryId, CategorizationRuleService.Evaluate(
            rules,
            accountId,
            "Deposit",
            100m));
        Assert.AreEqual(expenseCategoryId, CategorizationRuleService.Evaluate(
            rules,
            accountId,
            "Purchase",
            -100m));
    }

    [TestMethod]
    public void Evaluate_DoesNotMatchZeroAmountForDirectionRules()
    {
        Guid accountId = Guid.NewGuid();
        Guid categoryId = Guid.NewGuid();
        List<CategorizationRule> rules =
        [
            Rule(categoryId, priority: 1, amountDirection: "in"),
            Rule(categoryId, priority: 2, amountDirection: "out")
        ];

        Assert.IsNull(CategorizationRuleService.Evaluate(
            rules,
            accountId,
            "Adjustment",
            0m));
    }

    [TestMethod]
    public void Evaluate_AllowsAccountIndependentRules()
    {
        Guid accountId = Guid.NewGuid();
        Guid categoryId = Guid.NewGuid();

        Guid? result = CategorizationRuleService.Evaluate(
            [Rule(categoryId, priority: 1)],
            accountId,
            "Any transaction",
            -10m);

        Assert.AreEqual(categoryId, result);
    }

    [TestMethod]
    public void ResolveTransferCategories_DoesNotShareAOneSidedMatch()
    {
        Guid incomingCategoryId = Guid.NewGuid();

        TransferCategoryResolution result =
            CategorizationRuleService.ResolveTransferCategories(
                null,
                new CategorizationMatch(incomingCategoryId),
                null,
                false);

        Assert.IsNull(result.OutgoingCategoryId);
        Assert.AreEqual(incomingCategoryId, result.IncomingCategoryId);
        Assert.IsNull(result.SharedCategoryId);
        Assert.IsFalse(result.AutomaticCategoryApplied);
    }

    private static CategorizationRule Rule(
        Guid categoryId,
        int priority,
        Guid? accountId = null,
        string? descriptionContains = null,
        string? amountDirection = null) => new()
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            CategoryId = categoryId,
            AccountId = accountId,
            DescriptionContains = descriptionContains,
            AmountDirection = amountDirection,
            Priority = priority,
            IsEnabled = true
        };
}

using System.ComponentModel.DataAnnotations;
using FinHub.Api.Controllers;
using FinHub.Api.DTOs.CategorizationRule;
using FinHub.Api.Models;

namespace FinHub.Api.Tests.Controllers;

[TestClass]
public sealed class CategorizationRuleRequestTests
{
    [TestMethod]
    public void ValidCreateRequest_PassesValidation()
    {
        List<ValidationResult> errors = Validate(new CreateCategorizationRuleRequest
        {
            CategoryId = Guid.NewGuid(),
            DescriptionContains = "Coffee",
            Priority = 1
        });

        Assert.AreEqual(0, errors.Count);
    }

    [TestMethod]
    public void ValidUpdateRequest_PassesValidation()
    {
        List<ValidationResult> errors = Validate(new UpdateCategorizationRuleRequest
        {
            CategoryId = Guid.NewGuid(),
            AccountId = Guid.NewGuid(),
            Priority = 2
        });

        Assert.AreEqual(0, errors.Count);
    }

    [TestMethod]
    public void RequestWithoutCriteria_FailsValidation()
    {
        CreateCategorizationRuleRequest request = new()
        {
            CategoryId = Guid.NewGuid(),
            Priority = 1
        };

        Assert.IsTrue(Validate(request).Any(error =>
            error.ErrorMessage == "At least one rule criterion is required."));
    }

    [TestMethod]
    public void WhitespaceOnlyDescription_FailsValidation()
    {
        UpdateCategorizationRuleRequest request = new()
        {
            CategoryId = Guid.NewGuid(),
            DescriptionContains = " ",
            Priority = 1
        };

        Assert.IsTrue(Validate(request).Any(error =>
            error.ErrorMessage == "At least one rule criterion is required."));
    }

    [TestMethod]
    public void InvalidAmountDirection_FailsValidation()
    {
        CreateCategorizationRuleRequest request = new()
        {
            CategoryId = Guid.NewGuid(),
            AmountDirection = "sideways",
            Priority = 1
        };

        Assert.IsTrue(Validate(request).Any(error =>
            error.MemberNames.Contains(nameof(CreateCategorizationRuleRequest.AmountDirection))));
    }

    [TestMethod]
    public void NegativePriority_FailsValidation()
    {
        UpdateCategorizationRuleRequest request = new()
        {
            CategoryId = Guid.NewGuid(),
            AccountId = Guid.NewGuid(),
            Priority = -1
        };

        Assert.IsTrue(Validate(request).Any(error =>
            error.MemberNames.Contains(nameof(UpdateCategorizationRuleRequest.Priority))));
    }

    [TestMethod]
    public void DescriptionOver500Characters_FailsValidation()
    {
        CreateCategorizationRuleRequest request = new()
        {
            CategoryId = Guid.NewGuid(),
            DescriptionContains = new string('x', 501),
            Priority = 1
        };

        Assert.IsTrue(Validate(request).Any(error =>
            error.MemberNames.Contains(nameof(CreateCategorizationRuleRequest.DescriptionContains))));
    }

    [TestMethod]
    public void ValidRuleOrder_PassesValidation()
    {
        ReorderCategorizationRulesRequest request = new(
            [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()]);

        Assert.AreEqual(0, Validate(request).Count);
    }

    [TestMethod]
    public void DuplicateRuleIds_FailValidation()
    {
        Guid ruleId = Guid.NewGuid();
        ReorderCategorizationRulesRequest request = new(
            [ruleId, ruleId]);

        Assert.IsTrue(Validate(request).Any(error =>
            error.MemberNames.Contains(nameof(ReorderCategorizationRulesRequest.RuleIds))));
    }

    [TestMethod]
    public void EmptyRuleOrder_FailsValidation()
    {
        ReorderCategorizationRulesRequest request = new([]);

        Assert.IsTrue(Validate(request).Any(error =>
            error.MemberNames.Contains(nameof(ReorderCategorizationRulesRequest.RuleIds))));
    }

    [TestMethod]
    public void RenumberRules_AssignsConsecutivePriorities()
    {
        List<CategorizationRule> rules =
        [
            new() { Id = Guid.NewGuid(), Priority = 1 },
            new() { Id = Guid.NewGuid(), Priority = 4 },
            new() { Id = Guid.NewGuid(), Priority = 9 }
        ];

        CategorizationRulesController.RenumberRules(rules);

        CollectionAssert.AreEqual(
            new[] { 1, 2, 3 },
            rules.Select(rule => rule.Priority).ToArray());
    }

    [TestMethod]
    public void MissingCategoryAndPriority_FailsValidation()
    {
        UpdateCategorizationRuleRequest request = new()
        {
            AccountId = Guid.NewGuid()
        };

        List<ValidationResult> errors = Validate(request);

        Assert.IsTrue(errors.Any(error =>
            error.MemberNames.Contains(nameof(UpdateCategorizationRuleRequest.CategoryId))));
        Assert.IsTrue(errors.Any(error =>
            error.MemberNames.Contains(nameof(UpdateCategorizationRuleRequest.Priority))));
    }

    private static List<ValidationResult> Validate(object request)
    {
        List<ValidationResult> errors = [];
        Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            errors,
            validateAllProperties: true);
        return errors;
    }
}

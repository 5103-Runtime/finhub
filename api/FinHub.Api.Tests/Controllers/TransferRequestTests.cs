using System.ComponentModel.DataAnnotations;
using System.Reflection;
using FinHub.Api.DTOs.Transfer;

namespace FinHub.Api.Tests.Controllers;

[TestClass]
public sealed class TransferRequestTests
{
    [TestMethod]
    public void ValidTransferRequest_PassesValidation()
    {
        Assert.AreEqual(0, ValidationErrors(ValidRequest(), nameof(CreateTransferRequest.Description)).Count);
        Assert.AreEqual(0, ValidationErrors(ValidRequest(), nameof(CreateTransferRequest.Amount)).Count);
    }

    [TestMethod]
    public void MissingDescription_FailsRequiredValidation()
    {
        CreateTransferRequest request = ValidRequest() with { Description = " " };

        Assert.IsTrue(ValidationErrors(request, nameof(CreateTransferRequest.Description)).Count > 0);
    }

    [TestMethod]
    public void DescriptionOver500Characters_FailsLengthValidation()
    {
        CreateTransferRequest request = ValidRequest() with
        {
            Description = new string('x', 501)
        };

        Assert.IsTrue(ValidationErrors(request, nameof(CreateTransferRequest.Description)).Count > 0);
    }

    [TestMethod]
    public void NonPositiveAmount_FailsRangeValidation()
    {
        CreateTransferRequest zeroRequest = ValidRequest() with { Amount = 0m };
        CreateTransferRequest negativeRequest = ValidRequest() with { Amount = -1m };

        Assert.IsTrue(ValidationErrors(zeroRequest, nameof(CreateTransferRequest.Amount)).Count > 0);
        Assert.IsTrue(ValidationErrors(negativeRequest, nameof(CreateTransferRequest.Amount)).Count > 0);
    }

    [TestMethod]
    public void ExistingTransactionIds_AreOptional()
    {
        CreateTransferRequest request = ValidRequest();

        Assert.IsNull(request.FromTransactionId);
        Assert.IsNull(request.ToTransactionId);
    }

    private static CreateTransferRequest ValidRequest() =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2026, 11, 13),
            "Internal transfer",
            125.50m);

    private static List<ValidationResult> ValidationErrors(
        CreateTransferRequest request,
        string parameterName)
    {
        List<ValidationResult> errors = [];
        ParameterInfo parameter = typeof(CreateTransferRequest)
            .GetConstructors()
            .Single()
            .GetParameters()
            .Single(candidate => candidate.Name == parameterName);
        object? value = typeof(CreateTransferRequest)
            .GetProperty(parameterName)!
            .GetValue(request);
        Validator.TryValidateValue(
            value,
            new ValidationContext(request) { MemberName = parameterName },
            errors,
            parameter.GetCustomAttributes<ValidationAttribute>());
        return errors;
    }
}

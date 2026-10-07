using System.ComponentModel.DataAnnotations;

namespace FinHub.Api.Validations;

/// <summary>
/// Validates that a date of birth is neither in the future nor implausibly old.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class DateOfBirthAttribute : ValidationAttribute
{
    /// <summary>
    /// The maximum age in years that is considered valid for a date of birth.
    /// </summary>
    private const int MaximumAgeYears = 120;

    /// <inheritdoc />
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not DateOnly dateOfBirth)
        {
            return new ValidationResult("Date of birth must be a valid date.");
        }

        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (dateOfBirth > today)
        {
            return new ValidationResult("Date of birth cannot be in the future.");
        }

        if (dateOfBirth < today.AddYears(-MaximumAgeYears))
        {
            return new ValidationResult(
                $"Date of birth cannot be more than {MaximumAgeYears} years ago.");
        }

        return ValidationResult.Success;
    }
}

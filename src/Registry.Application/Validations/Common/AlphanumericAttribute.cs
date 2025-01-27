using System.ComponentModel.DataAnnotations;

namespace Application.Validations.Common
{
    public class AlphanumericAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            string? valueToValidate = value!.ToString();

            if (!valueToValidate!.All(char.IsLetterOrDigit))
            {
                return new ValidationResult($"{validationContext.DisplayName} must be alphanumeric (letters and numbers only).");
            }

            return ValidationResult.Success;
        }
    }
}

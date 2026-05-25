using System;
using System.ComponentModel.DataAnnotations;
using Application.Consts;
using Application.Helpers.Extensions;
using Application.Interfaces;

namespace Application.Validations
{
    public class ValidateAccountTypeAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var accountType = value as string;

            if (string.IsNullOrWhiteSpace(accountType))
            {
                return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
            }

            if (string.Equals(accountType, AccountTypes.Client, StringComparison.OrdinalIgnoreCase))
            {
                return ValidationResult.Success;
            }

            if (accountType.IsProspectAccount())
            {
                var featureFlagService = validationContext.GetService(typeof(IFeatureFlagService)) as IFeatureFlagService;
                if (featureFlagService?.IsEnabled(FeatureFlagKeys.IsProspectConsumptionEnabled) == true)
                {
                    return ValidationResult.Success;
                }
            }

            return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
        }

        public override string FormatErrorMessage(string name)
        {
            return $"Account Type {name} must be {AccountTypes.Client} or {AccountTypes.Prospect} when the prospect feature flag is enabled.";
        }
    }
}

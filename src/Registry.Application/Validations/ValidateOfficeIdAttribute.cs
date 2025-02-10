using Application.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Validations
{
    public class ValidateOfficeIdAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var instance = validationContext.ObjectInstance;
            var isCustomerProperty = instance.GetType().GetProperty(nameof(RefContactCsv.IsCustomer));
            var officeCodeProperty = instance.GetType().GetProperty(nameof(RefContactCsv.OfficeId));

            if (isCustomerProperty == null || officeCodeProperty == null)
            {
                return ValidationResult.Success;
            }

            bool? isCustomer = (bool?)isCustomerProperty?.GetValue(instance);
            string? officeCode = officeCodeProperty?.GetValue(instance) as string;

            if ((isCustomer == null || isCustomer == false) && string.IsNullOrEmpty(officeCode))
                return new ValidationResult("Office Code is required when contact type is collaborator");

            return ValidationResult.Success;
        }
    }
}

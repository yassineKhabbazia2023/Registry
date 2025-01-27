using Application.Models;
using Domain.Constants.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Validations
{
    public class ValidateOperationAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var instance = validationContext.ObjectInstance;
            var OperationTypeProperty = instance.GetType().GetProperty(nameof(RefRoleCsv.Operation));

            if (OperationTypeProperty == null)
            {
                return ValidationResult.Success;
            }

            string? OperationType = OperationTypeProperty?.GetValue(instance) as string;

            if(string.IsNullOrEmpty(OperationType) )
            {
                return new ValidationResult("Operation type can not be empty!");
            }

            string[] acceptedOperationTypes = { OperationStatusEnum.INSERT.ToString(), OperationStatusEnum.DELETE.ToString(), OperationStatusEnum.UPDATE.ToString(), };

            bool isAcceptedOperation = acceptedOperationTypes.Any(x => x.Equals(OperationType, StringComparison.OrdinalIgnoreCase));
            
            if (!isAcceptedOperation) 
            {
                return new ValidationResult("Operation type not known!");
            }

            return ValidationResult.Success;
        }
    }
}

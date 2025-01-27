using Application.Models;
using Application.Validations;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Registry.Application.Tests.Validations
{
    public class ValidateOfficeCodeAttributeTests
    {
        [Fact]
        public void NullableOfficeCode_WhenIsCustomerFalse_ShouldReturnError()
        {
            var model = new { IsCustomer = false, OfficeCode = string.Empty };
            var validateOfficeCodeAttr = new ValidateOfficeCodeAttribute();
            var validationResults = validateOfficeCodeAttr.GetValidationResult(model, new ValidationContext(model));
            Assert.NotNull(validationResults);
            validationResults?.ErrorMessage?.Equals("Office Code is required when contact type is collaborator");
        }

        [Fact]
        public void NullableOfficeCode_WhenIsCustomerIsTrue_ShouldReturnSuccess()
        {
            var model = new { IsCustomer = true, OfficeCode = string.Empty };
            var validateOfficeCodeAttr = new ValidateOfficeCodeAttribute();
            var validationResults = validateOfficeCodeAttr.GetValidationResult(model, new ValidationContext(model));
            Assert.Null(validationResults);
        }
    }
}

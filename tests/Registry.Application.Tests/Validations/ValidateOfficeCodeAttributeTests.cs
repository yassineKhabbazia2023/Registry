//// <copyright file="ValidateOfficeCodeAttributeTests.cs" company="Pulse">
//// Copyright (c) Pulse. All rights reserved.
//// </copyright>

using Application.Validations;
using System.ComponentModel.DataAnnotations;

namespace Registry.Application.Tests.Validations
{
    public class ValidateOfficeCodeAttributeTests
    {
        [Fact]
        public void NullableOfficeCode_WhenIsCustomerFalse_ShouldReturnError()
        {
            var model = new { IsCustomer = false, OfficeId = string.Empty };
            var validateOfficeCodeAttr = new ValidateOfficeIdAttribute();
            var validationResults = validateOfficeCodeAttr.GetValidationResult(model, new ValidationContext(model));
            Assert.NotNull(validationResults);
            validationResults?.ErrorMessage?.Equals("Office Code is required when contact type is collaborator");
        }

        [Fact]
        public void NullableOfficeCode_WhenIsCustomerIsTrue_ShouldReturnSuccess()
        {
            var model = new { IsCustomer = true, OfficeId = string.Empty };
            var validateOfficeCodeAttr = new ValidateOfficeIdAttribute();
            var validationResults = validateOfficeCodeAttr.GetValidationResult(model, new ValidationContext(model));
            Assert.Null(validationResults);
        }
    }
}

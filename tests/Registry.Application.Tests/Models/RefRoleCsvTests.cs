//// <copyright file="RefRoleCsvTests.cs" company="Pulse">
//// Copyright (c) Pulse. All rights reserved.
//// </copyright>

using Application.Models;
using Domain.Constants.Enums;
using System.ComponentModel.DataAnnotations;

namespace Registry.Application.Tests.Models
{
    public class RefRoleCsvTests
    {
        [Fact]
        public void RefRoleCsvTests_ValidateAll()
        {
            // Arrange
            var model = new RefRoleCsv
            {
                AccountNumber = "abc12",
                ContactEmail = "test@abc.com",
                Operation = OperationStatusEnum.INSERT.ToString(),
                RoleFlagStatus = 0
            };

            // Act
            var result = ValidateModel(model);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void RefRoleCsvTests_AccountNumberNull()
        {
            // Arrange
            var model = new RefRoleCsv
            {
                AccountNumber = null!,
                ContactEmail = "test@abc.com",
                Operation = OperationStatusEnum.INSERT.ToString(),
                RoleFlagStatus = 0
            };

            // Act
            var result = ValidateModel(model);

            // Assert
            Assert.NotEmpty(result);
            Assert.Equal("AccountNumber is required", result[0].ErrorMessage);
        }

        [Fact]
        public void RefRoleCsvTests_AccountNumberInvalid()
        {
            // Arrange
            var model = new RefRoleCsv
            {
                AccountNumber = "asdf(",
                ContactEmail = "test@abc.com",
                Operation = OperationStatusEnum.INSERT.ToString(),
                RoleFlagStatus = 0
            };

            // Act
            var result = ValidateModel(model);

            // Assert
            Assert.NotEmpty(result);
            Assert.Equal("AccountNumber must be alphanumeric (letters and numbers only).", result[0].ErrorMessage);
        }

        [Fact]
        public void RefRoleCsvTests_ContactEmailInvalid()
        {
            // Arrange
            var model = new RefRoleCsv
            {
                AccountNumber = "asdf(",
                ContactEmail = "testab@sd sd",
                Operation = OperationStatusEnum.INSERT.ToString(),
                RoleFlagStatus = 0
            };

            // Act
            var result = ValidateModel(model);

            // Assert
            Assert.NotEmpty(result);
            Assert.Equal("ContactEmail must be a valid email address", result[0].ErrorMessage);
        }

        [Fact]
        public void RefRoleCsvTests_ContactEmailNull()
        {
            // Arrange
            var model = new RefRoleCsv
            {
                AccountNumber = "asdf(",
                ContactEmail = null!,
                Operation = OperationStatusEnum.INSERT.ToString(),
                RoleFlagStatus = 0
            };

            // Act
            var result = ValidateModel(model);

            // Assert
            Assert.NotEmpty(result);
            Assert.Equal("ContactEmail is required", result[0].ErrorMessage);
        }

        [Fact]
        public void RefRoleCsvTests_RoleFlagStatusInvalid()
        {
            // Arrange
            var model = new RefRoleCsv
            {
                AccountNumber = "asdfff",
                ContactEmail = "test@ac.com",
                Operation = OperationStatusEnum.INSERT.ToString(),
                RoleFlagStatus = 2
            };

            // Act
            var result = ValidateModel(model);

            // Assert
            Assert.NotEmpty(result);
            Assert.Equal("RoleFlagStatus must be either 0 or 1.", result[0].ErrorMessage);
        }
        private List<ValidationResult> ValidateModel(object model)
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(model, null, null);
            Validator.TryValidateObject(model, validationContext, validationResults, true);
            return validationResults;
        }
    }
}

//// <copyright file="RefAccountCsvTest.cs" company="Pulse">
//// Copyright (c) Pulse. All rights reserved.
//// </copyright>

using Application.Consts;
using Application.Interfaces;
using Application.Models;
using Domain.Constants.Enums;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.ComponentModel.DataAnnotations;

namespace Registry.Application.Tests.Models
{
    public class RefAccountCsvTest
    {
        [Fact]
        public void RefAccountCsvTest_ShouldPassAllValidations()
        {
            // Arrange
            var model = new RefAccountCsv
            {
                AccountFlagStatus = 1,
                LegalName = "Pulse Corp",
                AccountNumber = "ABC123",
                Operation = OperationAction.Insert,
                AccountType = "CLIENT"
            };

            // Act
            var results = ValidateModel(model);

            // Assert
            Assert.Empty(results);
        }

        [Fact]
        public void RefAccountCsvTest_AccountFlagStatus_Wrong_Range_ShouldFailValidation()
        {
            // Arrange
            var model = new RefAccountCsv
            {
                AccountFlagStatus = 50,
                LegalName = "Pulse Corp",
                AccountNumber = "ABC123",
                Operation = OperationAction.Insert,
                AccountType = "CLIENT"
            };

            // Act
            var results = ValidateModel(model);

            // Assert
            Assert.NotEmpty(results);
            results.Count.Should().Be(1);
            results[0].ErrorMessage.Should().Be("AccountFlagStatus must be either 0 or 1.");
        }

        [Fact]
        public void RefAccountCsvTest_LegalName_Null_ShouldFailValidation()
        {
            // Arrange
            var model = new RefAccountCsv
            {
                AccountFlagStatus = 1,
                LegalName = null,
                AccountNumber = "ABC123",
                Operation = OperationAction.Insert,
                AccountType = "CLIENT"
            };

            // Act
            var results = ValidateModel(model);

            // Assert
            Assert.NotEmpty(results);
            results.Count.Should().Be(1);
            results[0].ErrorMessage.Should().Be("Account LegalName is required");
        }

        [Fact]
        public void RefAccountCsvTest_LegalName_Empty_ShouldFailValidation()
        {
            // Arrange
            var model = new RefAccountCsv
            {
                AccountFlagStatus = 1,
                LegalName = " ",
                AccountNumber = "ABC123",
                Operation = OperationAction.Insert,
                AccountType = "CLIENT"
            };

            // Act
            var results = ValidateModel(model);

            // Assert
            Assert.NotEmpty(results);
            results.Count.Should().Be(1);
            results[0].ErrorMessage.Should().Be("Account LegalName is required");
        }

        [Fact]
        public void RefAccountCsvTest_AccountNumber_Empty_ShouldFailValidation()
        {
            // Arrange
            var model = new RefAccountCsv
            {
                AccountFlagStatus = 1,
                LegalName = "Test",
                AccountNumber = " ",
                Operation = OperationAction.Insert,
                AccountType = "CLIENT"
            };

            // Act
            var results = ValidateModel(model);

            // Assert
            Assert.NotEmpty(results);
            results.Count.Should().Be(1);
            results[0].ErrorMessage.Should().Be("AccountNumber is required");
        }

        [Fact]
        public void RefAccountCsvTest_AccountNumber_Null_ShouldFailValidation()
        {
            // Arrange
            var model = new RefAccountCsv
            {
                AccountFlagStatus = 1,
                LegalName = "Test",
                AccountNumber = null,
                Operation = OperationAction.Insert,
                AccountType = "CLIENT"
            };

            // Act
            var results = ValidateModel(model);

            // Assert
            Assert.NotEmpty(results);
            results.Count.Should().Be(1);
            results[0].ErrorMessage.Should().Be("AccountNumber is required");
        }

        [Fact]
        public void RefAccountCsvTest_AccountNumber_HasSpecialChars_ShouldFailValidation()
        {
            // Arrange
            var model = new RefAccountCsv
            {
                AccountFlagStatus = 1,
                LegalName = "Test1",
                AccountNumber = "Test@1",
                Operation = OperationAction.Insert,
                AccountType = "CLIENT"
            };

            // Act
            var results = ValidateModel(model);

            // Assert
            Assert.NotEmpty(results);
            results.Count.Should().Be(1);
            results[0].ErrorMessage.Should().Be("AccountNumber must be alphanumeric (letters and numbers only).");
        }

        [Fact]
        public void RefAccountCsvTest_Operation_BadèRange_ShouldFailValidation()
        {
            // Arrange
            var model = new RefAccountCsv
            {
                AccountFlagStatus = 1,
                LegalName = "Test1",
                AccountNumber = "Test1",
                Operation = "Create",
                AccountType = "CLIENT"
            };

            // Act
            var results = ValidateModel(model);

            // Assert
            Assert.NotEmpty(results);
            results.Count.Should().Be(1);
            results[0].ErrorMessage.Should().Be("Operation type not known!");
        }

        [Fact]
        public void RefAccountCsvTest_AccountType_Client_ShouldPassValidation()
        {
            var model = new RefAccountCsv
            {
                AccountFlagStatus = 1,
                LegalName = "Pulse Corp",
                AccountNumber = "ABC123",
                Operation = OperationAction.Insert,
                AccountType = "CLIENT"
            };

            var results = ValidateModel(model, CreateServiceProvider(false));

            Assert.Empty(results);
        }

        [Fact]
        public void RefAccountCsvTest_AccountType_Prospect_WhenFeatureFlagEnabled_ShouldPassValidation()
        {
            var model = new RefAccountCsv
            {
                AccountFlagStatus = 1,
                LegalName = "Pulse Prospect",
                AccountNumber = "ABC124",
                Operation = OperationAction.Insert,
                AccountType = "PROSPECT"
            };

            var results = ValidateModel(model, CreateServiceProvider(true));

            Assert.Empty(results);
        }

        [Fact]
        public void RefAccountCsvTest_AccountType_Prospect_WhenFeatureFlagDisabled_ShouldFailValidation()
        {
            var model = new RefAccountCsv
            {
                AccountFlagStatus = 1,
                LegalName = "Pulse Prospect",
                AccountNumber = "ABC125",
                Operation = OperationAction.Insert,
                AccountType = "PROSPECT"
            };

            var results = ValidateModel(model, CreateServiceProvider(false));

            Assert.NotEmpty(results);
            results.Count.Should().Be(1);
            results[0].ErrorMessage.Should().Be("Account Type AccountType must be CLIENT or PROSPECT when the prospect feature flag is enabled.");
        }


        private static IServiceProvider CreateServiceProvider(bool enableProspectConsumption)
        {
            var services = new ServiceCollection();
            var featureFlagService = new Mock<IFeatureFlagService>();
            featureFlagService
                .Setup(service => service.IsEnabled(FeatureFlagKeys.IsProspectConsumptionEnabled))
                .Returns(enableProspectConsumption);
            services.AddSingleton(featureFlagService.Object);

            return services.BuildServiceProvider();
        }

        private List<ValidationResult> ValidateModel(object model, IServiceProvider? serviceProvider = null)
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(model, serviceProvider, null);
            Validator.TryValidateObject(model, validationContext, validationResults, true);
            return validationResults;
        }

    }
}

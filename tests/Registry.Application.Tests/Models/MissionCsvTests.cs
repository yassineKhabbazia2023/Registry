//// <copyright file="MissionCsvTests.cs" company="Pulse">
//// Copyright (c) Pulse. All rights reserved.
//// </copyright>

using Application.Helpers;
using Application.Models;
using CsvHelper;
using FluentAssertions;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Registry.Application.Tests.Models
{
    public class MissionCsvTests
    {
        [Fact]
        public void ReadStreamAsync_WithAkuiteoHeaders_MapsColumnsByName()
        {
            // Arrange
            var csvContent = new StringBuilder();
            csvContent.AppendLine("AccountNumber;EngagementCode;OfferCode;ProductCode;StartDate;EndDate;Operations");
            csvContent.AppendLine("123456;E1;PennylaneOfferCode;PennylaneProProductCode;12/01/2025;12/01/2027;INSERT");

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent.ToString()));

            // Act
            var records = CsvFileReader.ReadStreamAsync<MissionCsv>(stream).ToList();

            // Assert
            records.Should().HaveCount(1);
            var mission = records[0].Item1;
            mission.AccountNumber.Should().Be("123456");
            mission.EngagementCode.Should().Be("E1");
            mission.OfferCode.Should().Be("PennylaneOfferCode");
            mission.ProductCode.Should().Be("PennylaneProProductCode");
            mission.StartDate.Should().Be("12/01/2025");
            mission.EndDate.Should().Be("12/01/2027");
            mission.Operation.Should().Be("INSERT");
        }

        [Fact]
        public void ReadStreamAsync_WhenColumnMissing_ThrowsHeaderValidationException()
        {
            // Arrange
            var csvContent = new StringBuilder();
            csvContent.AppendLine("AccountNumber;EngagementCode;OfferCode;ProductCode;StartDate;EndDate");
            csvContent.AppendLine("123456;E1;PennylaneOfferCode;PennylaneProProductCode;12/01/2025;12/01/2027");

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent.ToString()));

            // Act
            var act = () => CsvFileReader.ReadStreamAsync<MissionCsv>(stream).ToList();

            // Assert
            act.Should().Throw<HeaderValidationException>();
        }

        [Fact]
        public void Validate_WithValidModel_ShouldPassAllValidations()
        {
            // Arrange
            var model = CreateValidModel();

            // Act
            var results = ValidateModel(model);

            // Assert
            results.Should().BeEmpty();
        }

        [Fact]
        public void Validate_WithoutProductCode_ShouldPassAllValidations()
        {
            // Arrange
            var model = CreateValidModel();
            model.ProductCode = null;

            // Act
            var results = ValidateModel(model);

            // Assert
            results.Should().BeEmpty();
        }

        [Theory]
        [InlineData(nameof(MissionCsv.AccountNumber))]
        [InlineData(nameof(MissionCsv.EngagementCode))]
        [InlineData(nameof(MissionCsv.OfferCode))]
        [InlineData(nameof(MissionCsv.StartDate))]
        [InlineData(nameof(MissionCsv.EndDate))]
        [InlineData(nameof(MissionCsv.Operation))]
        public void Validate_WithMissingRequiredField_ShouldFailValidation(string propertyName)
        {
            // Arrange
            var model = CreateValidModel();
            typeof(MissionCsv).GetProperty(propertyName)!.SetValue(model, null);

            // Act
            var results = ValidateModel(model);

            // Assert
            results.Should().NotBeEmpty();
        }

        [Theory]
        [InlineData(nameof(MissionCsv.StartDate))]
        [InlineData(nameof(MissionCsv.EndDate))]
        public void Validate_WithDateOutOfFormat_ShouldFailValidation(string propertyName)
        {
            // Arrange
            var model = CreateValidModel();
            typeof(MissionCsv).GetProperty(propertyName)!.SetValue(model, "2025-01-12");

            // Act
            var results = ValidateModel(model);

            // Assert
            results.Should().HaveCount(1);
            results[0].ErrorMessage.Should().Be("Invalid date format (dd/MM/yyyy)");
        }

        [Theory]
        [InlineData("12/01/2025", "11/01/2025")]
        [InlineData("12/01/2025", "12/01/2025")]
        public void Validate_WithEndDateNotLaterThanStartDate_ShouldFailValidation(string startDate, string endDate)
        {
            // Arrange
            var model = CreateValidModel();
            model.StartDate = startDate;
            model.EndDate = endDate;

            // Act
            var results = ValidateModel(model);

            // Assert
            results.Should().HaveCount(1);
            results[0].ErrorMessage.Should().Be("EndDate must be later than StartDate");
        }

        [Theory]
        [InlineData("INSERT")]
        [InlineData("DELETE")]
        [InlineData("insert")]
        public void Validate_WithAcceptedOperation_ShouldPassValidation(string operation)
        {
            // Arrange
            var model = CreateValidModel();
            model.Operation = operation;

            // Act
            var results = ValidateModel(model);

            // Assert
            results.Should().BeEmpty();
        }

        [Theory]
        [InlineData("UPDATE")]
        [InlineData("MERGE")]
        public void Validate_WithUnknownOperation_ShouldFailValidation(string operation)
        {
            // Arrange
            var model = CreateValidModel();
            model.Operation = operation;

            // Act
            var results = ValidateModel(model);

            // Assert
            results.Should().HaveCount(1);
            results[0].ErrorMessage.Should().Be("Operation type not known!");
        }

        private static MissionCsv CreateValidModel()
        {
            return new MissionCsv
            {
                AccountNumber = "123456",
                EngagementCode = "E1",
                OfferCode = "PennylaneOfferCode",
                ProductCode = "PennylaneProProductCode",
                StartDate = "12/01/2025",
                EndDate = "12/01/2027",
                Operation = "INSERT",
            };
        }

        private static List<ValidationResult> ValidateModel(object model)
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(model, null, null);
            Validator.TryValidateObject(model, validationContext, validationResults, true);
            return validationResults;
        }
    }
}

//// <copyright file="RefInvoiceCsvTests.cs" company="Pulse">
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
    public class RefInvoiceCsvTests
    {
        [Fact]
        public void ReadStreamAsync_WithSnakeCaseHeadersInAnyOrderAndExtraColumns_MapsExploitedColumnsByName()
        {
            // Arrange
            var csvContent = new StringBuilder();
            csvContent.AppendLine("item_type;item_amount_initial_inc_tax;client_code;item_number;item_date_issue;item_field_perso1;item_field_perso15;operation");
            csvContent.AppendLine("FAC;1200.00;C000123;FA-2024-0001;15/01/2024;ignored;https://docs.pulse.fr/FA-2024-0001.pdf;INSERT");

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent.ToString()));

            // Act
            var records = CsvFileReader.ReadStreamAsync<RefInvoiceCsv>(stream).ToList();

            // Assert
            records.Should().HaveCount(1);
            var invoice = records[0].Item1;
            invoice.AccountNumber.Should().Be("C000123");
            invoice.InvoiceNumber.Should().Be("FA-2024-0001");
            invoice.InvoiceDate.Should().Be("15/01/2024");
            invoice.DocumentPath.Should().Be("https://docs.pulse.fr/FA-2024-0001.pdf");
            invoice.Operation.Should().Be("INSERT");
        }

        [Fact]
        public void ReadStreamAsync_WhenExploitedColumnMissing_ThrowsHeaderValidationException()
        {
            // Arrange
            var csvContent = new StringBuilder();
            csvContent.AppendLine("item_type;client_code;item_number;item_date_issue;item_field_perso15");
            csvContent.AppendLine("FAC;C000123;FA-2024-0001;15/01/2024;https://docs.pulse.fr/FA-2024-0001.pdf");

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent.ToString()));

            // Act
            var act = () => CsvFileReader.ReadStreamAsync<RefInvoiceCsv>(stream).ToList();

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

        [Theory]
        [InlineData(nameof(RefInvoiceCsv.AccountNumber))]
        [InlineData(nameof(RefInvoiceCsv.InvoiceNumber))]
        [InlineData(nameof(RefInvoiceCsv.InvoiceDate))]
        [InlineData(nameof(RefInvoiceCsv.DocumentPath))]
        [InlineData(nameof(RefInvoiceCsv.Operation))]
        public void Validate_WithMissingRequiredField_ShouldFailValidation(string propertyName)
        {
            // Arrange
            var model = CreateValidModel();
            typeof(RefInvoiceCsv).GetProperty(propertyName)!.SetValue(model, null);

            // Act
            var results = ValidateModel(model);

            // Assert
            results.Should().NotBeEmpty();
        }

        [Fact]
        public void Validate_WithDateOutOfFormat_ShouldFailValidation()
        {
            // Arrange
            var model = CreateValidModel();
            model.InvoiceDate = "2024-01-15";

            // Act
            var results = ValidateModel(model);

            // Assert
            results.Should().HaveCount(1);
            results[0].ErrorMessage.Should().Be("Invalid date format (dd/MM/yyyy)");
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

        private static RefInvoiceCsv CreateValidModel()
        {
            return new RefInvoiceCsv
            {
                AccountNumber = "C000123",
                InvoiceNumber = "FA-2024-0001",
                InvoiceDate = "15/01/2024",
                DocumentPath = "https://docs.pulse.fr/FA-2024-0001.pdf",
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

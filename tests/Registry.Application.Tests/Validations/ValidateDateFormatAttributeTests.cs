// <copyright file="ValidateDateFormatAttributeTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Validations;
using FluentAssertions;

namespace Registry.Application.Tests.Validations
{
    public class ValidateDateFormatAttributeTests
    {
        private readonly ValidateDateFormatAttribute _attribute = new("dd/MM/yyyy");

        [Theory]
        [InlineData("15/01/2024")]
        [InlineData("01/12/2026")]
        [InlineData("29/02/2024")]
        public void IsValid_WithDateMatchingFormat_ReturnsTrue(string value)
        {
            _attribute.IsValid(value).Should().BeTrue();
        }

        [Theory]
        [InlineData("2024-01-15")]
        [InlineData("01/15/2024")]
        [InlineData("15/1/2024")]
        [InlineData("31/02/2024")]
        [InlineData("not a date")]
        public void IsValid_WithDateNotMatchingFormat_ReturnsFalse(string value)
        {
            _attribute.IsValid(value).Should().BeFalse();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void IsValid_WithNullOrEmptyValue_ReturnsTrue(string? value)
        {
            _attribute.IsValid(value).Should().BeTrue();
        }

        [Fact]
        public void FormatErrorMessage_ContainsExpectedFormat()
        {
            _attribute.FormatErrorMessage("InvoiceDate").Should().Be("Invalid date format (dd/MM/yyyy)");
        }
    }
}

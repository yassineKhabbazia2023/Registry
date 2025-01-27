// <copyright file="CsvConfigTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Helpers;
using Application.Models;

namespace ContactRegistry.Application.Tests.Helpers;

public class CsvConfigTests
{
    [Fact]
    public void IsDataCsvFormat_WithValidData_ShouldReturnTrue()
    {
        var result = CsvConfig.IsValidCsvFormat("ContactFlagStatus;Email;FirstName;LastName;IsCustomer;LandPhone;MobilePhone;JobDescription;OfficeCode;Operation\n1;test@gmail.com;Baris;Doe;;1234567890;0987654321;Developer;LaDefense;INSERT\n2;jane.doe@example.com;Jane;Fifi;true;;1111111111; Manager;LaDefense;UPDATE", typeof(RefContactCsv),out var message);

        Assert.True(result);
        Assert.Empty(message);
    }

    [Theory]
    [MemberData(nameof(InputData))]
    public void IsDataCsvFormat_WithInvalidData_ShouldReturnFalse(string data, string expectedMessage)
    {
        var result = CsvConfig.IsValidCsvFormat(data, typeof(RefAccountCsv), out var messageError);

        Assert.False(result);
        Assert.Contains(expectedMessage, messageError);
    }

    public static TheoryData<string, string> InputData =>
        new()
        {
            { null!, "The input data cannot be null or empty." },
            { string.Empty, "The input data cannot be null or empty."},
            { "ContactId,FirstName,LastName\n123,'Jean','Pierre'", "Missing columns in header"},
            { "ContactId;FirstName;LastName\n123;'10008384'", "Missing columns in header"},
        };

    // Test d'un fichier CSV valide
    [Fact]
    public void IsValidCsvFormat_ValidCsv_ReturnsTrue()
    {
        // Arrange
        var validCsv = "ContactFlagStatus;Email;FirstName;LastName;IsCustomer;LandPhone;MobilePhone;JobDescription;OfficeCode;Operation\n" +
                       "1;john.doe@example.com;John;Doe;true;1234567890;0987654321;Developer;La Defense;DELETE";

        // Act
        var result = CsvConfig.IsValidCsvFormat(validCsv, typeof(RefContactCsv), out var messageError);

        // Assert
        Assert.True(result);
        Assert.Empty(messageError);
    }

    // Test lorsque le CSV a une colonne manquante dans l'en-tête
    [Fact]
    public void IsValidCsvFormat_HeaderColumnCountMismatch_ReturnsFalse()
    {
        // Arrange
        var invalidCsv = "ContactFlagStatus;Email;FirstName;LastName;IsCustomer;LandPhone;MobilePhone;JobDescription;Operation\n" +
                         "1;john.doe@example.com;John;Doe;true;1234567890;0987654321;Developer;La Defense;DELETE";

        // Act
        var result = CsvConfig.IsValidCsvFormat(invalidCsv, typeof(RefContactCsv), out var messageError);

        // Assert
        Assert.False(result);
        Assert.Contains("Header column count mismatch", messageError);
    }

    // Test lorsque le CSV a des colonnes manquantes dans les lignes de données
    [Fact]
    public void IsValidCsvFormat_LineColumnCountMismatch_ReturnsFalse()
    {
        // Arrange
        var invalidCsv = "ContactFlagStatus;Email;FirstName;LastName;IsCustomer;LandPhone;MobilePhone;JobDescription;OfficeCode;Operation\n" +
                         "1;john.doe@example.com;John;Doe;true;1234567890;0987654321;Developer;La Defense\n" +  // Missing Operation
                         "2;jane.doe@example.com;Jane;Doe;true;;1111111111;Manager;La Defense;UPDATE";

        // Act
        var result = CsvConfig.IsValidCsvFormat(invalidCsv, typeof(RefContactCsv), out var messageError);

        // Assert
        Assert.False(result);
        Assert.Contains("Column count mismatch", messageError);
    }

    // Test pour un fichier CSV avec une colonne vide
    [Fact]
    public void IsValidCsvFormat_EmptyColumn_ReturnsFalse()
    {
        // Arrange
        var invalidCsv = "ContactFlagStatus;Email;FirstName;LastName;IsCustomer;LandPhone;MobilePhone;JobDescription;OfficeCode;Operation\n" +
                         ";john.doe@example.com;;;;;;;;DELETE";  // Empty FirstName

        // Act
        var result = CsvConfig.IsValidCsvFormat(invalidCsv, typeof(RefContactCsv), out var messageError);

        // Assert
        Assert.True(result);
        Assert.Empty(messageError);
    }

    // Test lorsque le CSV contient un nom de colonne incorrect
    [Fact]
    public void IsValidCsvFormat_IncorrectColumnName_ReturnsFalse()
    {
        // Arrange
        var invalidCsv = "ContactFlagStatus;Email;FirstName;LastName;IsCustomer;LandPhone;MobilePhone;JobDescription;OfficeCode;Operation\n" +  // OfficeId au lieu de OfficeCode
                         "1;john.doe@example.com;John;Doe;true;1234567890;0987654321;Developer;La Defense;DELETE";

        // Act
        var result = CsvConfig.IsValidCsvFormat(invalidCsv, typeof(RefContactCsv), out var messageError);

        // Assert
        Assert.True(result);
    }

    // Test pour un fichier CSV vide
    [Fact]
    public void IsValidCsvFormat_EmptyCsv_ReturnsFalse()
    {
        // Arrange
        var emptyCsv = string.Empty;

        // Act
        var result = CsvConfig.IsValidCsvFormat(emptyCsv, typeof(RefContactCsv), out var messageError);

        // Assert
        Assert.False(result);
        Assert.Contains("The input data cannot be null or empty.", messageError);
    }
}

// <copyright file="CsvConfigTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Helpers;
using Application.Models;
using CsvHelper;
using System.IO;

namespace Registry.Application.Tests.Helpers;

public class CsvConfigTests
{
    /// <summary>
    /// This tests for cases where the CSV file is valid.
    /// ----- Accounts
    /// 1 line with adress that contains a return to line
    /// 2 line with impair number of quotes
    /// 3 line with pair number of quotes
    /// 4 line with valid DATA
    /// 5 line with EMPTY COLUMN in the middle
    /// 5 line with EMPTY COLUMN in the start
    /// 6 line with SEMICOLON in quotes "personne ;moral"
    /// ----- Contacts
    /// 1 line with valid DATA without office
    /// 1 line with valid DATA with office
    /// </summary>
    [Theory]
    [InlineData("accounts_success_cases.csv", typeof(RefAccountCsv))]
    [InlineData("contacts_success_cases.csv", typeof(RefContactCsv))]
    public void IsDataCsvFormat_WithValidData_ShouldReturnTrue(string filename, Type type)
    {
        if (type == typeof(RefAccountCsv))
        {
            RunTest<RefAccountCsv>(filename);
        }
        else if (type == typeof(RefContactCsv))
        {
            RunTest<RefContactCsv>(filename);
        }
        else
        {
            throw new NotSupportedException($"Type non géré : {type}");
        }
    }
    private void RunTest<T>(string filename)
    {
        // Arrange
        var stream = LoadFileToStream(filename);

        // Act
        var responses = CsvFileReader.ReadStreamAsync<T>(stream).ToList();
        var result = CsvConfig.IsValidCsvFormat(responses, typeof(T), out var message);

        // Assert
        Assert.Empty(message);
        Assert.True(result);
    }
    [Theory]
    [InlineData("accounts_errors_case_empty_file.csv", "The input data does not contain any lines")]
    [InlineData("accounts_errors_case_missing_column_line.csv", "Column count mismatch. Expected 44, but got 43")]
    [InlineData("accounts_errors_case_missing_column_line.csv", "Column count mismatch. Expected 44, but got 43")]
    public void IsDataCsvFormat_WithInvalidData_ShouldReturnFalse(string filename, string errorMessage)
    {
        // Arrange
        var stream = LoadFileToStream(filename);

        // Act
        var responses = CsvFileReader.ReadStreamAsync<RefAccountCsv>(stream).ToList();
        var result = CsvConfig.IsValidCsvFormat(responses, typeof(RefAccountCsv), out var message);

        // Assert
        Assert.Contains(errorMessage, message);
        Assert.False(result);
    }

    [Theory]
    [InlineData("accounts_errors_case_missing_column_header.csv")]
    public void IsDataCsvFormat_WithInvalidHeader_ShouldThrow(string filename)
    {
        // Arrange
        var stream = LoadFileToStream(filename);

        // Act
        var act = () => CsvFileReader.ReadStreamAsync<RefAccountCsv>(stream).ToList();

        // Assert
        Assert.Throws<HeaderValidationException>(act);
    }

    private static Stream LoadFileToStream(string fileName)
    {
        var memoryStream = new MemoryStream();
        using (var fileStream = new FileStream($@"MockData/{fileName}", FileMode.Open, FileAccess.Read))
        {
            fileStream.CopyTo(memoryStream);
        }
        memoryStream.Position = 0; // Reset the stream position to the beginning
        return memoryStream;
    }
}

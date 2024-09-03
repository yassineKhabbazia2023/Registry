// <copyright file="CsvConfigTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Helpers;

namespace ContactRegistry.Application.Tests.Helpers;

public class CsvConfigTests
{
    [Fact]
    public void IsDataCsvFormat_WithValidData_ShouldReturnTrue()
    {
        var result = CsvConfig.IsValidCsvFormat("Entete1;Entete2;Entete3\ndata1;data2;data3", out var message);

        Assert.True(result);
        Assert.Equal(".", message);
    }

    [Theory]
    [MemberData(nameof(InputData))]
    public void IsDataCsvFormat_WithInvalidData_ShouldReturnFalse(string data, string expectedMessage)
    {
        var result = CsvConfig.IsValidCsvFormat(data, out var messageError);

        Assert.False(result);
        Assert.Equal(expectedMessage, messageError);
    }

    public static TheoryData<string, string> InputData =>
        new()
        {
            { null!, ": message cannot be null or empty." },
            { string.Empty, ": message cannot be null or empty."},
            { "ContactId,FirstName,LastName\n123,'Jean','Pierre'", ": errors on lines 1, 2"},
            { "ContactId;FirstName;LastName\n123;'10008384'", ": errors on lines 2"},
        };
}

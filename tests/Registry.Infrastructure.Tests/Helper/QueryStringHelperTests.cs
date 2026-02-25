// <copyright file="QueryStringHelperTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Infrastructure.Helper;

namespace Registry.Infrastructure.Tests.Helper;

public class QueryStringHelperTests
{
    [Fact]
    public void GetDecodedParameter_WithValidParameter_ReturnsDecodedValue()
    {
        // Arrange
        var rawQueryString = "?contactId=123&page=1&pageSize=10&search=test";

        // Act
        var result = QueryStringHelper.GetDecodedParameter(rawQueryString, "search");

        // Assert
        Assert.Equal("test", result);
    }

    [Fact]
    public void GetDecodedParameter_WithPlusSign_PreservesPlusSign()
    {
        // Arrange
        var rawQueryString = "?contactId=123&page=1&pageSize=10&search=stest+test@domain.com";

        // Act
        var result = QueryStringHelper.GetDecodedParameter(rawQueryString, "search");

        // Assert
        Assert.Equal("stest+test@domain.com", result);
    }

    [Fact]
    public void GetDecodedParameter_WithEncodedPlusSign_DecodesToPlusSign()
    {
        // Arrange
        var rawQueryString = "?contactId=123&page=1&pageSize=10&search=stest%2Btest@domain.com";

        // Act
        var result = QueryStringHelper.GetDecodedParameter(rawQueryString, "search");

        // Assert
        Assert.Equal("stest+test@domain.com", result);
    }

    [Fact]
    public void GetDecodedParameter_WithEncodedAtSign_DecodesToAtSign()
    {
        // Arrange
        var rawQueryString = "?contactId=123&search=test%40domain.com";

        // Act
        var result = QueryStringHelper.GetDecodedParameter(rawQueryString, "search");

        // Assert
        Assert.Equal("test@domain.com", result);
    }

    [Fact]
    public void GetDecodedParameter_WithMissingParameter_ReturnsNull()
    {
        // Arrange
        var rawQueryString = "?contactId=123&page=1&pageSize=10";

        // Act
        var result = QueryStringHelper.GetDecodedParameter(rawQueryString, "search");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetDecodedParameter_WithEmptyParameterValue_ReturnsNull()
    {
        // Arrange
        var rawQueryString = "?contactId=123&search=&page=1";

        // Act
        var result = QueryStringHelper.GetDecodedParameter(rawQueryString, "search");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetDecodedParameter_WithNullQueryString_ReturnsNull()
    {
        // Act
        var result = QueryStringHelper.GetDecodedParameter(null, "search");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetDecodedParameter_WithEmptyQueryString_ReturnsNull()
    {
        // Act
        var result = QueryStringHelper.GetDecodedParameter(string.Empty, "search");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetDecodedParameter_WithNullParameterName_ReturnsNull()
    {
        // Arrange
        var rawQueryString = "?search=test";

        // Act
        var result = QueryStringHelper.GetDecodedParameter(rawQueryString, null!);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetDecodedParameter_WithParameterAtEnd_ReturnsValue()
    {
        // Arrange
        var rawQueryString = "?contactId=123&page=1&search=stest+";

        // Act
        var result = QueryStringHelper.GetDecodedParameter(rawQueryString, "search");

        // Assert
        Assert.Equal("stest+", result);
    }

    [Fact]
    public void GetDecodedParameter_CaseInsensitiveParameterName_ReturnsValue()
    {
        // Arrange
        var rawQueryString = "?SEARCH=test";

        // Act
        var result = QueryStringHelper.GetDecodedParameter(rawQueryString, "search");

        // Assert
        Assert.Equal("test", result);
    }

    [Fact]
    public void GetDecodedParameter_WithSpecialCharactersInEmail_ReturnsDecodedEmail()
    {
        // Arrange
        var rawQueryString = "?search=user.name+tag@sub.domain.com";

        // Act
        var result = QueryStringHelper.GetDecodedParameter(rawQueryString, "search");

        // Assert
        Assert.Equal("user.name+tag@sub.domain.com", result);
    }

    [Fact]
    public void GetDecodedParameter_WithInvalidUrlEncoding_ReturnsValueAsIs()
    {
        // Arrange - Uri.UnescapeDataString returns invalid sequences as-is
        var rawQueryString = "?search=test%ZZ";

        // Act
        var result = QueryStringHelper.GetDecodedParameter(rawQueryString, "search");

        // Assert
        Assert.Equal("test%ZZ", result);
    }
}

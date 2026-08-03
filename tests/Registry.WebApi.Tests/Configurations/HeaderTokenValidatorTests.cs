// <copyright file="HeaderTokenValidatorTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using FluentAssertions;
using Microsoft.Extensions.Options;
using WebApi.Configurations;
using WebApi.Configurations.Models;

namespace Registry.WebApi.Tests.Configurations;

public class HeaderTokenValidatorTests
{
    private const string HeaderSecret = "header-secret";
    private const string QuerySecret = "legacy-query-secret";

    private readonly HeaderTokenValidator _validator;

    public HeaderTokenValidatorTests()
    {
        var tokenModel = new TokenModel
        {
            Token = QuerySecret,
            HeaderToken = HeaderSecret,
        };

        _validator = new HeaderTokenValidator(Options.Create(tokenModel));
    }

    [Theory]
    [InlineData(HeaderSecret, null, true)]
    [InlineData(HeaderSecret, "anything", true)]
    [InlineData("wrong-header", QuerySecret, false)]
    [InlineData("", QuerySecret, false)]
    [InlineData(null, QuerySecret, true)]
    [InlineData(null, "wrong-query", false)]
    [InlineData(null, "   ", false)]
    [InlineData(null, null, false)]
    public void IsAuthorized_WithHeaderAndQueryTokenCombinations_AppliesHeaderFirstWithoutFallback(string? headerToken, string? queryToken, bool expected)
    {
        // Act
        var result = _validator.IsAuthorized(headerToken, queryToken);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void IsAuthorized_WhenQueryTokenMatchesHeaderSecret_ReturnsFalse()
    {
        // Act
        var result = _validator.IsAuthorized(null, HeaderSecret);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void IsAuthorized_WhenHeaderSecretNotConfigured_ReturnsFalse(string? configuredHeaderSecret)
    {
        // Arrange
        var validator = new HeaderTokenValidator(Options.Create(new TokenModel
        {
            Token = QuerySecret,
            HeaderToken = configuredHeaderSecret,
        }));

        // Act
        var result = validator.IsAuthorized(configuredHeaderSecret, null);

        // Assert
        result.Should().BeFalse();
    }
}

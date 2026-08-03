// <copyright file="HeaderTokenValidator.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using WebApi.Configurations.Models;

namespace WebApi.Configurations;

/// <summary>
/// Csv flows authentication through the X-Registry-Token header or the query string token.
/// </summary>
public class HeaderTokenValidator : IHeaderTokenValidator
{
    private readonly TokenModel _tokenModel;

    /// <summary>
    /// HeaderTokenValidator.
    /// </summary>
    /// <param name="tokenModel">The token options.</param>
    public HeaderTokenValidator(IOptions<TokenModel> tokenModel)
    {
        _tokenModel = tokenModel!.Value;
    }

    /// <inheritdoc />
    public bool IsAuthorized(string? headerToken, string? queryToken)
    {
        if (headerToken != null)
        {
            return SecretEquals(headerToken, _tokenModel.HeaderToken);
        }

        return queryToken != null && SecretEquals(queryToken, _tokenModel.Token);
    }

    private static bool SecretEquals(string providedToken, string? configuredSecret)
    {
        if (string.IsNullOrWhiteSpace(providedToken) || string.IsNullOrWhiteSpace(configuredSecret))
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(providedToken),
            Encoding.UTF8.GetBytes(configuredSecret));
    }
}

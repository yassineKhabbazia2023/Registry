// <copyright file="IHeaderTokenValidator.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace WebApi.Configurations;

/// <summary>
/// Csv flows authentication through the X-Registry-Token header.
/// </summary>
public interface IHeaderTokenValidator
{
    /// <summary>
    /// Checks the header first, without falling back to the query string when it is present.
    /// </summary>
    /// <param name="headerToken">X-Registry-Token header value, null when absent.</param>
    /// <param name="queryToken">Query string token value, null when absent.</param>
    /// <returns>True when the caller is authorized.</returns>
    bool IsAuthorized(string? headerToken, string? queryToken);
}

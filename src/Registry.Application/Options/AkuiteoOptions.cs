// <copyright file="AkuiteoOptions.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Options;

/// <summary>
/// Stores the Akuiteo integration configuration.
/// </summary>
public class AkuiteoOptions
{
    /// <summary>
    /// Gets or sets the Akuiteo API base URL.
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// Gets or sets the rate-limiting client identifier.
    /// </summary>
    public string? ClientIdRateLimiting { get; set; }

    /// <summary>
    /// Gets or sets the rate-limiting client secret.
    /// </summary>
    public string? ClientSecretRateLimiting { get; set; }

    /// <summary>
    /// Gets or sets the Microsoft login base URL.
    /// </summary>
    public string? TokenBaseUrl { get; set; }

    /// <summary>
    /// Gets or sets the Azure AD tenant identifier.
    /// </summary>
    public string? Tenant { get; set; }

    /// <summary>
    /// Gets or sets the OAuth grant type.
    /// </summary>
    public string? TokenGrantType { get; set; }

    /// <summary>
    /// Gets or sets the OAuth client identifier.
    /// </summary>
    public string? TokenClientId { get; set; }

    /// <summary>
    /// Gets or sets the OAuth client secret.
    /// </summary>
    public string? TokenClientSecret { get; set; }

    /// <summary>
    /// Gets or sets the OAuth scope.
    /// </summary>
    public string? TokenScope { get; set; }

    /// <summary>
    /// Builds the Akuiteo API base URL with a trailing slash for relative endpoint composition.
    /// </summary>
    /// <returns>The Akuiteo API base URL.</returns>
    public Uri BuildApiBaseUrl()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(BaseUrl);

        return new Uri($"{BaseUrl.TrimEnd('/')}/", UriKind.Absolute);
    }

    /// <summary>
    /// Builds the Microsoft OAuth v2 token base URL for the configured tenant.
    /// </summary>
    /// <returns>The Microsoft OAuth v2 token base URL.</returns>
    public Uri BuildTokenBaseUrl()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(TokenBaseUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(Tenant);

        return new Uri($"{TokenBaseUrl.TrimEnd('/')}/{Tenant}/oauth2/v2.0/", UriKind.Absolute);
    }
}

// <copyright file="AkuiteoBearerTokenHandler.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using System.Net.Http.Headers;
using Application.Exceptions;
using Azure.Core;
using Microsoft.Extensions.Logging;

namespace Application.Providers;

/// <summary>
/// Adds an Azure AD bearer token to outgoing Akuiteo API requests.
/// </summary>
public class AkuiteoBearerTokenHandler : DelegatingHandler
{
    private readonly TokenCredential credential;
    private readonly string[] scopes;
    private readonly ILogger<AkuiteoBearerTokenHandler> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AkuiteoBearerTokenHandler"/> class.
    /// </summary>
    /// <param name="credential">The Azure AD token credential.</param>
    /// <param name="scopes">The scopes requested for the token.</param>
    /// <param name="logger">The logger.</param>
    public AkuiteoBearerTokenHandler(
        TokenCredential credential,
        string[] scopes,
        ILogger<AkuiteoBearerTokenHandler> logger)
    {
        this.credential = credential;
        this.scopes = scopes;
        this.logger = logger;
    }

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        AccessToken accessToken;
        try
        {
            accessToken = await credential.GetTokenAsync(new TokenRequestContext(scopes), cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Akuiteo request stopped because the Azure AD token could not be retrieved.");
            throw new AkuiteoAuthenticationTechnicalException("Unable to retrieve the Azure AD token for Akuiteo.", exception);
        }

        if (string.IsNullOrWhiteSpace(accessToken.Token))
        {
            logger.LogWarning("Akuiteo request stopped because the Azure AD token was empty.");
            throw new AkuiteoAuthenticationTechnicalException("Unable to retrieve the Azure AD token for Akuiteo.");
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Token);
        return await base.SendAsync(request, cancellationToken);
    }
}

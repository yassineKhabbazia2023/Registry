// <copyright file="AkuiteoEligibilityProvider.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using System.Net.Http.Json;
using System.Text.Json;
using Application.Exceptions;
using Application.Interfaces;
using Application.Models;
using Application.Models.Results;
using Microsoft.Extensions.Logging;

namespace Application.Providers;

/// <summary>
/// Calls the real Akuiteo account-search API for onboarding eligibility checks.
/// </summary>
public class AkuiteoEligibilityProvider : IAccountOnboardingEligibilityProvider
{
    private readonly HttpClient httpClient;
    private readonly ILogger<AkuiteoEligibilityProvider> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AkuiteoEligibilityProvider"/> class.
    /// </summary>
    /// <param name="httpClient">The typed HTTP client.</param>
    /// <param name="logger">The logger.</param>
    public AkuiteoEligibilityProvider(
        HttpClient httpClient,
        ILogger<AkuiteoEligibilityProvider> logger)
    {
        this.httpClient = httpClient;
        this.logger = logger;
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsAsync(string siret)
    {
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "akuiteo/account/search")
            {
                Content = JsonContent.Create(CreateSearchRequest(siret), options: AkuiteoResponseHelper.JsonSerializerOptions)
            };

            using var response = await httpClient.SendAsync(httpRequest);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = AkuiteoResponseHelper.BuildDownstreamErrorMessage(responseBody, response.ReasonPhrase);
                logger.LogWarning(
                    "Akuiteo account search failed. Siret: {Siret}, StatusCode: {StatusCode}, Error: {Error}",
                    siret,
                    (int)response.StatusCode,
                    errorMessage);
                throw new AkuiteoAccountSearchTechnicalException(
                    string.IsNullOrWhiteSpace(errorMessage)
                        ? "Akuiteo account search failed."
                        : errorMessage);
            }

            var apiResponse = AkuiteoResponseHelper.Deserialize<AkuiteoApiResponse<IEnumerable<JsonElement>>>(responseBody);
            if (!AkuiteoResponseHelper.IsSucceededStatus(apiResponse?.Meta?.Status))
            {
                var errorMessage = AkuiteoResponseHelper.BuildMetaStatusErrorMessage(apiResponse?.Meta, responseBody);
                logger.LogWarning(
                    "Akuiteo account search returned an invalid status. Siret: {Siret}, Error: {Error}",
                    siret,
                    errorMessage);
                throw new AkuiteoAccountSearchTechnicalException(errorMessage);
            }

            return apiResponse?.Data?.Any() == true;
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(
                exception,
                "Akuiteo account search failed because the external service was unreachable. Siret: {Siret}",
                siret);
            throw new AkuiteoAccountSearchTechnicalException("Akuiteo is unavailable.", exception);
        }
        catch (AkuiteoAuthenticationTechnicalException exception)
        {
            logger.LogError(
                exception,
                "Akuiteo account search failed because the Microsoft token could not be retrieved. Siret: {Siret}",
                siret);
            throw new AkuiteoAccountSearchTechnicalException(exception.Message, exception);
        }
        catch (TaskCanceledException exception)
        {
            logger.LogError(
                exception,
                "Akuiteo account search timed out. Siret: {Siret}",
                siret);
            throw new AkuiteoAccountSearchTechnicalException("Akuiteo timed out.", exception);
        }
    }

    /// <summary>
    /// Creates the Akuiteo account-search payload from a normalized SIRET.
    /// </summary>
    /// <param name="siret">The normalized SIRET.</param>
    /// <returns>The Akuiteo account-search request.</returns>
    private static AkuiteoAccountSearchRequest CreateSearchRequest(string siret)
    {
        return new AkuiteoAccountSearchRequest
        {
            Siret = new AkuiteoAccountSearchFilterRequest
            {
                Operator = "IS",
                Value = siret,
                Wildcards = "*"
            }
        };
    }
}

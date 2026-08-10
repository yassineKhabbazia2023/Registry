// <copyright file="RegistryApiAkuiteoContactService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Exceptions;
using Application.Interfaces;
using Application.Models.Results;
using Application.Requests;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Application.Providers;

/// <summary>
/// Creates Akuiteo contacts through the Registry API from hosts that do not own the Akuiteo credentials.
/// </summary>
public class RegistryApiAkuiteoContactService : IAkuiteoContactService
{
    private const string ClientName = "RegistryApi";
    private const string ContactRoute = "api/akuiteo/contacts";
    private readonly IHttpClientFactory httpClientFactory;
    private readonly ILogger<RegistryApiAkuiteoContactService> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RegistryApiAkuiteoContactService"/> class.
    /// </summary>
    /// <param name="httpClientFactory">The Registry HTTP client factory.</param>
    /// <param name="logger">The logger.</param>
    public RegistryApiAkuiteoContactService(
        IHttpClientFactory httpClientFactory,
        ILogger<RegistryApiAkuiteoContactService> logger)
    {
        this.httpClientFactory = httpClientFactory;
        this.logger = logger;
    }

    /// <inheritdoc/>
    public async Task<AkuiteoContactCreationResponse> CreateContactAsync(AkuiteoContactCreationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            var httpClient = httpClientFactory.CreateClient(ClientName);
            using var response = await httpClient.PostAsJsonAsync(ContactRoute, request);

            if (response.StatusCode != HttpStatusCode.Created)
            {
                var errorMessage = await ReadErrorMessageAsync(response);
                logger.LogError(
                    "Registry API rejected the Akuiteo contact creation. AccountNumber: {AccountNumber}, StatusCode: {StatusCode}, Error: {Error}",
                    request.AccountNumber,
                    (int)response.StatusCode,
                    errorMessage);
                throw new AkuiteoContactCreationTechnicalException(errorMessage);
            }

            var creationResponse = await response.Content.ReadFromJsonAsync<AkuiteoContactCreationResponse>();
            if (creationResponse is null || string.IsNullOrWhiteSpace(creationResponse.ContactId))
            {
                throw new AkuiteoContactCreationTechnicalException(
                    "Registry API returned an empty Akuiteo contact identifier.");
            }

            return creationResponse;
        }
        catch (AkuiteoContactCreationTechnicalException)
        {
            throw;
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(
                exception,
                "Registry API was unreachable while creating an Akuiteo contact. AccountNumber: {AccountNumber}",
                request.AccountNumber);
            throw new AkuiteoContactCreationTechnicalException("Registry API is unavailable.", exception);
        }
        catch (TaskCanceledException exception)
        {
            logger.LogError(
                exception,
                "Registry API timed out while creating an Akuiteo contact. AccountNumber: {AccountNumber}",
                request.AccountNumber);
            throw new AkuiteoContactCreationTechnicalException("Registry API timed out.", exception);
        }
        catch (JsonException exception)
        {
            logger.LogError(
                exception,
                "Registry API returned an invalid Akuiteo contact creation response. AccountNumber: {AccountNumber}",
                request.AccountNumber);
            throw new AkuiteoContactCreationTechnicalException(
                "Registry API returned an invalid Akuiteo contact creation response.",
                exception);
        }
    }

    /// <inheritdoc/>
    public Task<IReadOnlyCollection<AkuiteoContactSearchDataResponse>> SearchContactsAsync(string email)
    {
        throw new NotSupportedException(
            "Akuiteo contact search is not supported from Registry Azure Functions.");
    }

    /// <summary>
    /// Extracts the problem title returned by the Registry API, with a status-based fallback.
    /// </summary>
    /// <param name="response">The Registry API response.</param>
    /// <returns>The normalized error message.</returns>
    private static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response)
    {
        var fallbackMessage = $"Registry API returned status code {(int)response.StatusCode} while creating an Akuiteo contact.";
        var responseContent = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(responseContent))
        {
            return fallbackMessage;
        }

        try
        {
            using var jsonDocument = JsonDocument.Parse(responseContent);
            if (jsonDocument.RootElement.TryGetProperty("title", out var title)
                && title.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(title.GetString()))
            {
                return title.GetString()!;
            }
        }
        catch (JsonException)
        {
            return fallbackMessage;
        }

        return fallbackMessage;
    }
}

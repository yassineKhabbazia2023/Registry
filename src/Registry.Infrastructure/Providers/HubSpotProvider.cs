// <copyright file="HubSpotProvider.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Interfaces;
using Application.Models.Results;
using Application.Requests;

namespace Application.Providers;

public class HubSpotProvider : IHubSpotProvider
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IHttpClientFactory httpClientFactory;

    public HubSpotProvider(IHttpClientFactory httpClientFactory)
    {
        this.httpClientFactory = httpClientFactory;
    }

    public async Task<HubSpotSubmissionResult> SubmitIntegrationAsync(string portalId, string formGuid, HubSpotSubmissionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var url = $"{portalId}/{formGuid}";
        var httpClient = httpClientFactory.CreateClient("HubSpot");
        using var response = await httpClient.PostAsJsonAsync(url, request, JsonSerializerOptions);

        if (response.IsSuccessStatusCode)
        {
            return new HubSpotSubmissionResult
            {
                IsSuccess = true,
                StatusCode = (int)response.StatusCode
            };
        }

        var errorMessage = await response.Content.ReadAsStringAsync();

        return new HubSpotSubmissionResult
        {
            IsSuccess = false,
            StatusCode = (int)response.StatusCode,
            ErrorMessage = string.IsNullOrWhiteSpace(errorMessage) ? response.ReasonPhrase : errorMessage
        };
    }
}

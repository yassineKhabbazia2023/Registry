// <copyright file="AkuiteoCustomerProvider.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using System.Net.Http.Json;
using Application.Interfaces;
using Application.Models;
using Application.Models.Results;

namespace Application.Providers;

/// <summary>
/// Calls the real Akuiteo customer-creation API.
/// </summary>
public class AkuiteoCustomerProvider : IAkuiteoCustomerProvider
{
    private readonly HttpClient httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="AkuiteoCustomerProvider"/> class.
    /// </summary>
    /// <param name="httpClient">The typed HTTP client.</param>
    public AkuiteoCustomerProvider(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    /// <inheritdoc/>
    public async Task<AkuiteoCustomerCreationProviderResult> CreateCustomerAsync(AkuiteoCreateCustomerRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "akuiteo/account")
        {
            Content = JsonContent.Create(request, options: AkuiteoResponseHelper.JsonSerializerOptions)
        };

        using var response = await httpClient.SendAsync(httpRequest);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return new AkuiteoCustomerCreationProviderResult
            {
                IsSuccess = false,
                StatusCode = (int)response.StatusCode,
                ErrorMessage = AkuiteoResponseHelper.BuildDownstreamErrorMessage(responseBody, response.ReasonPhrase)
            };
        }

        var apiResponse = AkuiteoResponseHelper.Deserialize<AkuiteoCustomerCreationApiResponse>(responseBody);
        var metaStatus = apiResponse?.Meta?.Status;
        var accountNumber = apiResponse?.Data?.AccountNumber;

        if (!AkuiteoResponseHelper.IsSucceededStatus(metaStatus))
        {
            return new AkuiteoCustomerCreationProviderResult
            {
                IsSuccess = false,
                StatusCode = (int)response.StatusCode,
                ErrorMessage = AkuiteoResponseHelper.BuildMetaStatusErrorMessage(apiResponse?.Meta, responseBody)
            };
        }

        return new AkuiteoCustomerCreationProviderResult
        {
            IsSuccess = !string.IsNullOrWhiteSpace(accountNumber),
            StatusCode = (int)response.StatusCode,
            AccountNumber = accountNumber,
            ErrorMessage = string.IsNullOrWhiteSpace(accountNumber) ? "Akuiteo returned an empty account number." : null
        };
    }
}

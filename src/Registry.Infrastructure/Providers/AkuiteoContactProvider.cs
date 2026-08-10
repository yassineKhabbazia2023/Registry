// <copyright file="AkuiteoContactProvider.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using System.Net.Http.Json;
using Application.Interfaces;
using Application.Models;
using Application.Models.Results;

namespace Application.Providers;

/// <summary>
/// Calls the real Akuiteo contact-creation API.
/// </summary>
public class AkuiteoContactProvider : IAkuiteoContactProvider
{
    private const string ExactMatchOperator = "IS";
    private readonly HttpClient httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="AkuiteoContactProvider"/> class.
    /// </summary>
    /// <param name="httpClient">The typed HTTP client.</param>
    public AkuiteoContactProvider(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    /// <inheritdoc/>
    public async Task<AkuiteoContactCreationProviderResult> CreateContactAsync(AkuiteoCreateContactRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "akuiteo/contact")
        {
            Content = JsonContent.Create(request, options: AkuiteoResponseHelper.JsonSerializerOptions)
        };

        using var response = await httpClient.SendAsync(httpRequest);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return new AkuiteoContactCreationProviderResult
            {
                IsSuccess = false,
                StatusCode = (int)response.StatusCode,
                ErrorMessage = AkuiteoResponseHelper.BuildDownstreamErrorMessage(responseBody, response.ReasonPhrase)
            };
        }

        var apiResponse = AkuiteoResponseHelper.Deserialize<AkuiteoContactCreationApiResponse>(responseBody);
        var metaStatus = apiResponse?.Meta?.Status;
        var contactId = apiResponse?.Data?.ContactId;

        if (!AkuiteoResponseHelper.IsSucceededStatus(metaStatus))
        {
            return new AkuiteoContactCreationProviderResult
            {
                IsSuccess = false,
                StatusCode = (int)response.StatusCode,
                ErrorMessage = AkuiteoResponseHelper.BuildMetaStatusErrorMessage(apiResponse?.Meta, responseBody)
            };
        }

        return new AkuiteoContactCreationProviderResult
        {
            IsSuccess = !string.IsNullOrWhiteSpace(contactId),
            StatusCode = (int)response.StatusCode,
            ContactId = contactId,
            ErrorMessage = string.IsNullOrWhiteSpace(contactId) ? "Akuiteo returned an empty contact id." : null
        };
    }

    /// <inheritdoc/>
    public async Task<AkuiteoContactSearchProviderResult> SearchContactsAsync(string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        var request = new AkuiteoContactSearchRequest
        {
            Email = new AkuiteoContactSearchFilterRequest
            {
                Operator = ExactMatchOperator,
                Value = email.Trim()
            }
        };

        using var response = await httpClient.PostAsJsonAsync(
            "akuiteo/contact/search",
            request,
            AkuiteoResponseHelper.JsonSerializerOptions);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return new AkuiteoContactSearchProviderResult
            {
                IsSuccess = false,
                StatusCode = (int)response.StatusCode,
                ErrorMessage = AkuiteoResponseHelper.BuildDownstreamErrorMessage(responseBody, response.ReasonPhrase)
            };
        }

        var apiResponse = AkuiteoResponseHelper.DeserializeOrThrow<
            AkuiteoApiResponse<AkuiteoContactSearchApiDataResponse>>(responseBody);

        if (!AkuiteoResponseHelper.IsSucceededStatus(apiResponse?.Meta?.Status))
        {
            return new AkuiteoContactSearchProviderResult
            {
                IsSuccess = false,
                StatusCode = (int)response.StatusCode,
                ErrorMessage = AkuiteoResponseHelper.BuildMetaStatusErrorMessage(apiResponse?.Meta, responseBody)
            };
        }

        return new AkuiteoContactSearchProviderResult
        {
            IsSuccess = true,
            StatusCode = (int)response.StatusCode,
            Contacts = apiResponse?.Data is null
                ? []
                : [MapContactSearchResponse(apiResponse.Data)]
        };
    }

    /// <summary>
    /// Maps the downstream Akuiteo contact contract to the Registry QA response.
    /// </summary>
    /// <param name="contact">The downstream Akuiteo contact.</param>
    /// <returns>The Registry contact-search response.</returns>
    private static AkuiteoContactSearchDataResponse MapContactSearchResponse(
        AkuiteoContactSearchApiDataResponse contact)
    {
        var matchingSite = string.IsNullOrWhiteSpace(contact.Email)
            ? null
            : contact.SitesRelatedInformation?.FirstOrDefault(
                site => string.Equals(site.Email, contact.Email, StringComparison.OrdinalIgnoreCase));

        return new AkuiteoContactSearchDataResponse
        {
            Title = contact.Title,
            LastName = contact.Name,
            FirstName = contact.FirstName,
            Email = contact.Email,
            MobilePhone = contact.MobilePhone ?? matchingSite?.MobilePhone
        };
    }
}

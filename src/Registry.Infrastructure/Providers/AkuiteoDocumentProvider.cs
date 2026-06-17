// <copyright file="AkuiteoDocumentProvider.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using System.Net.Http.Headers;
using System.Text.Json;
using Application.Interfaces;
using Application.Models;
using Application.Models.Results;

namespace Application.Providers;

/// <summary>
/// Calls the real Akuiteo document upload API.
/// </summary>
public class AkuiteoDocumentProvider : IAkuiteoDocumentProvider
{
    private readonly HttpClient httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="AkuiteoDocumentProvider"/> class.
    /// </summary>
    /// <param name="httpClient">The typed HTTP client.</param>
    public AkuiteoDocumentProvider(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    /// <inheritdoc/>
    public async Task<AkuiteoDocumentUploadProviderResult> UploadDocumentAsync(AkuiteoDocumentUploadRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var streamContent = new StreamContent(request.Content);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(request.ContentType);

        using var multipartContent = new MultipartFormDataContent();
        multipartContent.Add(streamContent, "document", request.DocumentName);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"akuiteo/account/{Uri.EscapeDataString(request.AccountNumber)}/documents")
        {
            Content = multipartContent
        };

        using var response = await httpClient.SendAsync(httpRequest);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return new AkuiteoDocumentUploadProviderResult
            {
                IsSuccess = false,
                StatusCode = (int)response.StatusCode,
                ErrorMessage = AkuiteoResponseHelper.BuildDownstreamErrorMessage(responseBody, response.ReasonPhrase),
                RawResponseBody = responseBody
            };
        }

        var metaErrorMessage = BuildMetaErrorMessage(responseBody);
        return new AkuiteoDocumentUploadProviderResult
        {
            IsSuccess = string.IsNullOrWhiteSpace(metaErrorMessage),
            StatusCode = (int)response.StatusCode,
            ErrorMessage = metaErrorMessage,
            RawResponseBody = string.IsNullOrWhiteSpace(metaErrorMessage) ? null : responseBody
        };
    }

    /// <summary>
    /// Builds the Akuiteo metadata error message when a successful HTTP response carries a failed metadata status.
    /// </summary>
    /// <param name="responseBody">The downstream response body.</param>
    /// <returns>The metadata error message when available; otherwise <see langword="null"/>.</returns>
    private static string? BuildMetaErrorMessage(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return null;
        }

        var apiResponse = AkuiteoResponseHelper.Deserialize<AkuiteoApiResponse<JsonElement>>(responseBody);
        if (apiResponse == null)
        {
            return "Akuiteo returned an invalid response.";
        }

        if (apiResponse.Meta == null || AkuiteoResponseHelper.IsSucceededStatus(apiResponse.Meta.Status))
        {
            return null;
        }

        return AkuiteoResponseHelper.BuildMetaStatusErrorMessage(apiResponse.Meta, responseBody);
    }
}

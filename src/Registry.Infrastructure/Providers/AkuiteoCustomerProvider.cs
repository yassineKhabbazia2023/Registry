// <copyright file="AkuiteoCustomerProvider.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using System.Net.Http.Json;
using System.Text;
using Application.Interfaces;
using Application.Models;
using Application.Models.Results;
using Application.Requests;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Application.Providers;

/// <summary>
/// Calls the real Akuiteo customer APIs.
/// </summary>
public class AkuiteoCustomerProvider : IAkuiteoCustomerProvider
{
    private static readonly JsonSerializerSettings RequestSerializerSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        NullValueHandling = NullValueHandling.Ignore
    };

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

    /// <inheritdoc/>
    public async Task<AkuiteoPaymentInformationsProviderResult> GetPaymentInformationsAsync(string accountNumber)
    {
        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"akuiteo/account/{Uri.EscapeDataString(accountNumber)}/payment-informations")
        {
            Content = CreateJsonContent("{}")
        };

        using var response = await httpClient.SendAsync(httpRequest);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return new AkuiteoPaymentInformationsProviderResult
            {
                IsSuccess = false,
                StatusCode = (int)response.StatusCode,
                ErrorMessage = AkuiteoResponseHelper.BuildDownstreamErrorMessage(responseBody, response.ReasonPhrase)
            };
        }

        var apiResponse = AkuiteoResponseHelper.DeserializeOrThrow(
            responseBody,
            JsonConvert.DeserializeObject<AkuiteoPaymentInformationsResponse>);

        if (!AkuiteoResponseHelper.IsSucceededStatus(apiResponse?.Meta?.Status))
        {
            return new AkuiteoPaymentInformationsProviderResult
            {
                IsSuccess = false,
                StatusCode = (int)response.StatusCode,
                ErrorMessage = AkuiteoResponseHelper.BuildMetaStatusErrorMessage(apiResponse?.Meta, responseBody)
            };
        }

        if (apiResponse?.Data is null)
        {
            return new AkuiteoPaymentInformationsProviderResult
            {
                IsSuccess = false,
                StatusCode = (int)response.StatusCode,
                ErrorMessage = "Akuiteo returned empty payment information."
            };
        }

        if (HasNoPaymentPreference(apiResponse.Data))
        {
            apiResponse.Data = new AkuiteoPaymentInformationsDataResponse();
        }

        return new AkuiteoPaymentInformationsProviderResult
        {
            IsSuccess = true,
            StatusCode = (int)response.StatusCode,
            Response = apiResponse
        };
    }

    /// <summary>
    /// Determines whether Akuiteo returned its default representation for an account without payment preferences.
    /// </summary>
    /// <param name="data">The payment information returned by Akuiteo.</param>
    /// <returns><see langword="true"/> when the response exactly matches the Akuiteo default representation.</returns>
    private static bool HasNoPaymentPreference(AkuiteoPaymentInformationsDataResponse data)
    {
        var conditions = data.ConditionOfPayment?.ToArray();
        var methods = data.MethodOfPayment?.ToArray();
        var bankingInformationGroups = data.BankingInformations?.ToArray();

        return conditions is { Length: 1 }
            && string.Equals(conditions[0].Code, " le 0", StringComparison.Ordinal)
            && conditions[0].DeadLine is null
            && conditions[0].Term is null
            && conditions[0].Day == 0
            && conditions[0].ReferenceOnBankStatement is null
            && methods is { Length: 1 }
            && methods[0] is null
            && bankingInformationGroups is { Length: 1 }
            && bankingInformationGroups[0] is not null
            && !bankingInformationGroups[0].Any();
    }

    /// <inheritdoc/>
    public Task<AkuiteoAccountOperationProviderResult> UpdateBankingInformationsAsync(
        string accountNumber,
        IReadOnlyCollection<AkuiteoBankingInformationRequest> request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var content = CreateJsonContent(JsonConvert.SerializeObject(request, RequestSerializerSettings));
        return SendAccountOperationAsync(
            HttpMethod.Post,
            $"akuiteo/account/{Uri.EscapeDataString(accountNumber)}/banking-informations",
            content);
    }

    /// <inheritdoc/>
    public Task<AkuiteoAccountOperationProviderResult> PatchAccountAsync(string accountNumber, JObject request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var content = CreateJsonContent(request.ToString(Formatting.None));
        return SendAccountOperationAsync(
            HttpMethod.Patch,
            $"akuiteo/account/{Uri.EscapeDataString(accountNumber)}",
            content);
    }

    /// <summary>
    /// Creates JSON request content without changing the already serialized payload.
    /// </summary>
    /// <param name="json">The JSON payload.</param>
    /// <returns>The HTTP content.</returns>
    private static StringContent CreateJsonContent(string json)
    {
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    /// <summary>
    /// Sends an Akuiteo customer account operation and normalizes its metadata response.
    /// </summary>
    /// <param name="method">The downstream HTTP method.</param>
    /// <param name="path">The downstream relative path.</param>
    /// <param name="content">The JSON request content.</param>
    /// <returns>The normalized provider result.</returns>
    private async Task<AkuiteoAccountOperationProviderResult> SendAccountOperationAsync(
        HttpMethod method,
        string path,
        HttpContent content)
    {
        using var httpRequest = new HttpRequestMessage(method, path)
        {
            Content = content
        };

        using var response = await httpClient.SendAsync(httpRequest);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return new AkuiteoAccountOperationProviderResult
            {
                IsSuccess = false,
                StatusCode = (int)response.StatusCode,
                ErrorMessage = AkuiteoResponseHelper.BuildDownstreamErrorMessage(responseBody, response.ReasonPhrase)
            };
        }

        var apiResponse = AkuiteoResponseHelper.DeserializeOrThrow<AkuiteoAccountOperationResponse>(responseBody);
        if (!AkuiteoResponseHelper.IsSucceededStatus(apiResponse?.Meta?.Status))
        {
            return new AkuiteoAccountOperationProviderResult
            {
                IsSuccess = false,
                StatusCode = (int)response.StatusCode,
                ErrorMessage = AkuiteoResponseHelper.BuildMetaStatusErrorMessage(apiResponse?.Meta, responseBody)
            };
        }

        return new AkuiteoAccountOperationProviderResult
        {
            IsSuccess = true,
            StatusCode = (int)response.StatusCode,
            Response = apiResponse
        };
    }
}

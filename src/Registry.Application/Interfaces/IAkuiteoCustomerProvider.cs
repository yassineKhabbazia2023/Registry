// <copyright file="IAkuiteoCustomerProvider.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;
using Application.Models.Results;
using Application.Requests;
using Newtonsoft.Json.Linq;

namespace Application.Interfaces;

/// <summary>
/// Sends customer requests to the Akuiteo external API.
/// </summary>
public interface IAkuiteoCustomerProvider
{
    /// <summary>
    /// Creates a customer in Akuiteo.
    /// </summary>
    /// <param name="request">The outbound Akuiteo payload.</param>
    /// <returns>The provider execution result.</returns>
    Task<AkuiteoCustomerCreationProviderResult> CreateCustomerAsync(AkuiteoCreateCustomerRequest request);

    /// <summary>
    /// Retrieves payment information for an Akuiteo customer account.
    /// </summary>
    /// <param name="accountNumber">The Akuiteo customer account number.</param>
    /// <returns>The provider execution result.</returns>
    Task<AkuiteoPaymentInformationsProviderResult> GetPaymentInformationsAsync(string accountNumber);

    /// <summary>
    /// Updates banking information for an Akuiteo customer account.
    /// </summary>
    /// <param name="accountNumber">The Akuiteo customer account number.</param>
    /// <param name="request">The banking-information changes.</param>
    /// <returns>The provider execution result.</returns>
    Task<AkuiteoAccountOperationProviderResult> UpdateBankingInformationsAsync(
        string accountNumber,
        IReadOnlyCollection<AkuiteoBankingInformationRequest> request);

    /// <summary>
    /// Partially updates an Akuiteo customer account.
    /// </summary>
    /// <param name="accountNumber">The Akuiteo customer account number.</param>
    /// <param name="request">The generic JSON patch payload.</param>
    /// <returns>The provider execution result.</returns>
    Task<AkuiteoAccountOperationProviderResult> PatchAccountAsync(string accountNumber, JObject request);
}

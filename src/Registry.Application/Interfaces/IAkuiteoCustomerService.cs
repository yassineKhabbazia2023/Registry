// <copyright file="IAkuiteoCustomerService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models.Results;
using Application.Requests;
using Newtonsoft.Json.Linq;

namespace Application.Interfaces;

/// <summary>
/// Exposes Akuiteo customer creation and account update workflows.
/// </summary>
public interface IAkuiteoCustomerService
{
    /// <summary>
    /// Creates a customer in Akuiteo.
    /// </summary>
    /// <param name="request">The input payload received by Registry.</param>
    /// <returns>The created Akuiteo account number.</returns>
    Task<AkuiteoCustomerCreationResponse> CreateCustomerAsync(AkuiteoCustomerCreationRequest request);

    /// <summary>
    /// Retrieves payment information for an Akuiteo customer account.
    /// </summary>
    /// <param name="accountId">The Registry account identifier.</param>
    /// <returns>The configured payment conditions, methods, and banking information.</returns>
    Task<AkuiteoPaymentInformationsDataResponse> GetPaymentInformationsAsync(int accountId);

    /// <summary>
    /// Updates banking information for an Akuiteo customer account.
    /// </summary>
    /// <param name="accountId">The Registry account identifier.</param>
    /// <param name="request">The banking-information changes.</param>
    /// <returns>The Akuiteo metadata response.</returns>
    Task<AkuiteoAccountOperationResponse> UpdateBankingInformationsAsync(
        int accountId,
        IReadOnlyCollection<AkuiteoBankingInformationRequest> request);

    /// <summary>
    /// Partially updates an Akuiteo customer account with the caller-provided JSON payload.
    /// </summary>
    /// <param name="accountId">The Registry account identifier.</param>
    /// <param name="request">The generic JSON patch payload.</param>
    /// <returns>The Akuiteo metadata response.</returns>
    Task<AkuiteoAccountOperationResponse> PatchAccountAsync(int accountId, JObject request);
}

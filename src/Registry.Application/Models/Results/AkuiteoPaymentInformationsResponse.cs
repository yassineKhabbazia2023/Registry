// <copyright file="AkuiteoPaymentInformationsResponse.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Models.Results;

/// <summary>
/// Represents the Akuiteo payment-information response for a customer account.
/// </summary>
public class AkuiteoPaymentInformationsResponse
{
    /// <summary>
    /// Gets or sets the Akuiteo response metadata.
    /// </summary>
    public AkuiteoMetaResponse? Meta { get; set; }

    /// <summary>
    /// Gets or sets the customer payment information.
    /// </summary>
    public AkuiteoPaymentInformationsDataResponse? Data { get; set; }
}

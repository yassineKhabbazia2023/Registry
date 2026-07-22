// <copyright file="AkuiteoPaymentInformationsDataResponse.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Models.Results;

/// <summary>
/// Represents payment conditions, payment methods, and banking information returned by Akuiteo.
/// </summary>
public class AkuiteoPaymentInformationsDataResponse
{
    /// <summary>
    /// Gets or sets the configured payment conditions.
    /// </summary>
    public IEnumerable<AkuiteoConditionOfPaymentResponse>? ConditionOfPayment { get; set; }

    /// <summary>
    /// Gets or sets the configured payment methods.
    /// </summary>
    public IEnumerable<string?>? MethodOfPayment { get; set; }

    /// <summary>
    /// Gets or sets the grouped banking information returned by Akuiteo.
    /// </summary>
    public IEnumerable<IEnumerable<AkuiteoBankingInformationResponse>>? BankingInformations { get; set; }
}

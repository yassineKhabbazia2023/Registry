// <copyright file="AkuiteoSepaRequest.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Requests;

/// <summary>
/// Represents SEPA banking information sent to Akuiteo.
/// </summary>
public class AkuiteoSepaRequest
{
    /// <summary>
    /// Gets or sets the domestic bank details.
    /// </summary>
    public AkuiteoBankDetailsRequest? BankDetails { get; set; }

    /// <summary>
    /// Gets or sets the BIC components.
    /// </summary>
    public AkuiteoBicRequest? Bic { get; set; }

    /// <summary>
    /// Gets or sets the IBAN components.
    /// </summary>
    public AkuiteoIbanRequest? Iban { get; set; }
}

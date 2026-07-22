// <copyright file="AkuiteoIbanRequest.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Requests;

/// <summary>
/// Represents the IBAN components sent to Akuiteo.
/// </summary>
public class AkuiteoIbanRequest
{
    /// <summary>Gets or sets the IBAN country code.</summary>
    public string? Country { get; set; }

    /// <summary>Gets or sets the IBAN key.</summary>
    public string? Key { get; set; }

    /// <summary>Gets or sets the IBAN account number.</summary>
    public string? AccountNumber { get; set; }
}

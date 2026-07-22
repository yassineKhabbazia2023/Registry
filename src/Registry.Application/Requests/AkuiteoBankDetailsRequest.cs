// <copyright file="AkuiteoBankDetailsRequest.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Requests;

/// <summary>
/// Represents domestic bank-account details sent to Akuiteo.
/// </summary>
public class AkuiteoBankDetailsRequest
{
    /// <summary>Gets or sets the bank entity code.</summary>
    public string? Entity { get; set; }

    /// <summary>Gets or sets the branch counter code.</summary>
    public string? Counter { get; set; }

    /// <summary>Gets or sets the bank account number.</summary>
    public string? AccountNumber { get; set; }

    /// <summary>Gets or sets the bank key.</summary>
    public string? Key { get; set; }

    /// <summary>Gets or sets the bank domiciliation.</summary>
    public string? Domiciliation { get; set; }
}

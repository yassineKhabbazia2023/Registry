// <copyright file="AkuiteoCreateCustomerRequest.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Models;

/// <summary>
/// Represents the payload expected by the Akuiteo customer-creation API.
/// </summary>
public class AkuiteoCreateCustomerRequest
{
    /// <summary>
    /// Gets or sets the legal name.
    /// </summary>
    public required string LegalName { get; set; }

    /// <summary>
    /// Gets or sets the SIREN.
    /// </summary>
    public required string Siren { get; set; }

    /// <summary>
    /// Gets or sets the SIRET.
    /// </summary>
    public required string Siret { get; set; }

    /// <summary>
    /// Gets or sets the legal structure.
    /// </summary>
    public required string LegalStructure { get; set; }

    /// <summary>
    /// Gets or sets the legal form code.
    /// </summary>
    public required string LegalFormCode { get; set; }

    /// <summary>
    /// Gets or sets the NAF code.
    /// </summary>
    public required string NafCode { get; set; }

    /// <summary>
    /// Gets or sets the postal address.
    /// </summary>
    public required AkuiteoCreateCustomerAddressRequest Address { get; set; }

    /// <summary>
    /// Gets or sets the case-manager email.
    /// </summary>
    public required string CaseManagerEmail { get; set; }

    /// <summary>
    /// Gets or sets the account-manager email.
    /// </summary>
    public required string AccountManagerEmail { get; set; }
}

// <copyright file="AkuiteoCreateContactTypesRequest.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Models;

/// <summary>
/// Represents the nested contact-type flags expected by the Akuiteo contact API.
/// </summary>
public class AkuiteoCreateContactTypesRequest
{
    /// <summary>
    /// Gets or sets a value indicating whether the contact is a digital vault contact.
    /// </summary>
    public required bool IsDigitalVaultContact { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the contact is a debt collection contact.
    /// </summary>
    public required bool IsDebtCollectionContact { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the contact is a mandate signatory.
    /// </summary>
    public required bool IsMandateSignatory { get; set; }
}

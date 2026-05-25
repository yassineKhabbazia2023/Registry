// <copyright file="AkuiteoContactTypesRequest.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;

namespace Application.Requests;

/// <summary>
/// Represents the contact-type flags used to create an Akuiteo contact from Registry.
/// </summary>
public class AkuiteoContactTypesRequest
{
    /// <summary>
    /// Gets or sets a value indicating whether the contact is a digital vault contact.
    /// </summary>
    [Required]
    public bool? IsDigitalVaultContact { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the contact is a debt collection contact.
    /// </summary>
    [Required]
    public bool? IsDebtCollectionContact { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the contact is a mandate signatory.
    /// </summary>
    [Required]
    public bool? IsMandateSignatory { get; set; }
}

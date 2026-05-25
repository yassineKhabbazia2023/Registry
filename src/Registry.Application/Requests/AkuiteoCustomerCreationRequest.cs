// <copyright file="AkuiteoCustomerCreationRequest.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Application.Requests;

/// <summary>
/// Represents the payload used to create a customer in Akuiteo from Registry.
/// </summary>
public class AkuiteoCustomerCreationRequest
{
    /// <summary>
    /// Gets or sets the legal name.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string? LegalName { get; set; }

    /// <summary>
    /// Gets or sets the SIRET.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string? Siret { get; set; }

    /// <summary>
    /// Gets or sets the SIREN.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string? Siren { get; set; }

    /// <summary>
    /// Gets or sets the legal structure.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string? LegalStructure { get; set; }

    /// <summary>
    /// Gets or sets the legal form.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string? LegalForm { get; set; }

    /// <summary>
    /// Gets or sets the NAF code.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string? NafCode { get; set; }

    /// <summary>
    /// Gets or sets the address line.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string? Address { get; set; }

    /// <summary>
    /// Gets or sets the zip code.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string? ZipCode { get; set; }

    /// <summary>
    /// Gets or sets the city.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string? City { get; set; }

    /// <summary>
    /// Gets or sets the department code.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    [JsonPropertyName("code_departement")]
    public string? DepartmentCode { get; set; }

    /// <summary>
    /// Gets or sets the region code.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    [JsonPropertyName("code_region")]
    public string? RegionCode { get; set; }

    /// <summary>
    /// Gets or sets the country code.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    [JsonPropertyName("code_pays")]
    public string? CountryCode { get; set; }

    /// <summary>
    /// Gets or sets the case-manager contact identifier.
    /// </summary>
    [Required]
    [Range(1, int.MaxValue)]
    public int? CaseManagerContactId { get; set; }

    /// <summary>
    /// Gets or sets the account-manager contact identifier.
    /// </summary>
    [Required]
    [Range(1, int.MaxValue)]
    public int? AccountManagerContactId { get; set; }
}

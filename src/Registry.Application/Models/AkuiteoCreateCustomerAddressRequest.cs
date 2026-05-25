// <copyright file="AkuiteoCreateCustomerAddressRequest.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Models;

/// <summary>
/// Represents the nested address payload expected by Akuiteo.
/// </summary>
public class AkuiteoCreateCustomerAddressRequest
{
    /// <summary>
    /// Gets or sets the first address line.
    /// </summary>
    public required string Line1 { get; set; }

    /// <summary>
    /// Gets or sets the zip code.
    /// </summary>
    public required string ZipCode { get; set; }

    /// <summary>
    /// Gets or sets the city.
    /// </summary>
    public required string City { get; set; }

    /// <summary>
    /// Gets or sets the department code.
    /// </summary>
    public required string DepartmentCode { get; set; }

    /// <summary>
    /// Gets or sets the region code.
    /// </summary>
    public required string RegionCode { get; set; }

    /// <summary>
    /// Gets or sets the country code.
    /// </summary>
    public required string CountryCode { get; set; }
}

// <copyright file="AkuiteoContactSearchApiDataResponse.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Models.Results;

/// <summary>
/// Represents the contact data returned by the downstream Akuiteo search API.
/// </summary>
public class AkuiteoContactSearchApiDataResponse
{
    /// <summary>
    /// Gets or sets the contact last name exposed as <c>name</c> by Akuiteo.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the contact first name.
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Gets or sets the contact title.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the contact mobile phone number.
    /// </summary>
    public string? MobilePhone { get; set; }

    /// <summary>
    /// Gets or sets the contact email address.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the contact information attached to Akuiteo sites.
    /// </summary>
    public IReadOnlyCollection<AkuiteoContactSiteRelatedInformationResponse>? SitesRelatedInformation { get; set; } = [];
}

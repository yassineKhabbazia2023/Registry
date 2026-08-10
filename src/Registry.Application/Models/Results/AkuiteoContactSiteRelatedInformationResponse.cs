// <copyright file="AkuiteoContactSiteRelatedInformationResponse.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Models.Results;

/// <summary>
/// Represents the contact information associated with an Akuiteo site.
/// </summary>
public class AkuiteoContactSiteRelatedInformationResponse
{
    /// <summary>
    /// Gets or sets the site-specific contact email address.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the site-specific contact mobile phone number.
    /// </summary>
    public string? MobilePhone { get; set; }
}

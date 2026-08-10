// <copyright file="AkuiteoContactSearchDataResponse.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Models.Results;

/// <summary>
/// Represents a contact returned by the Akuiteo contact-search API.
/// </summary>
public class AkuiteoContactSearchDataResponse
{
    /// <summary>
    /// Gets or sets the contact title.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the contact last name.
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Gets or sets the contact first name.
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Gets or sets the contact email address.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the contact mobile phone number.
    /// </summary>
    public string? MobilePhone { get; set; }
}

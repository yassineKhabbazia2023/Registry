// <copyright file="AkuiteoContactSearchRequest.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Models;

/// <summary>
/// Represents the Akuiteo contact-search request payload.
/// </summary>
public class AkuiteoContactSearchRequest
{
    /// <summary>
    /// Gets or sets the email search filter.
    /// </summary>
    public AkuiteoContactSearchFilterRequest? Email { get; set; }
}

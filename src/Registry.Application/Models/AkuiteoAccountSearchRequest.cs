// <copyright file="AkuiteoAccountSearchRequest.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Models;

/// <summary>
/// Represents the Akuiteo account-search request payload.
/// </summary>
public class AkuiteoAccountSearchRequest
{
    /// <summary>
    /// Gets or sets the SIRET search filter.
    /// </summary>
    public AkuiteoAccountSearchFilterRequest? Siret { get; set; }
}

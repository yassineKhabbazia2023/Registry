// <copyright file="AkuiteoAccountSearchFilterRequest.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Models;

/// <summary>
/// Represents an Akuiteo account-search filter.
/// </summary>
public class AkuiteoAccountSearchFilterRequest
{
    /// <summary>
    /// Gets or sets the search operator.
    /// </summary>
    public string? Operator { get; set; }

    /// <summary>
    /// Gets or sets the searched value.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Gets or sets the wildcard mode.
    /// </summary>
    public string? Wildcards { get; set; }
}

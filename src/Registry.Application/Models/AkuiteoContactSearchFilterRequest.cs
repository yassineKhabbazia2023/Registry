// <copyright file="AkuiteoContactSearchFilterRequest.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Models;

/// <summary>
/// Represents an Akuiteo contact-search filter.
/// </summary>
public class AkuiteoContactSearchFilterRequest
{
    /// <summary>
    /// Gets or sets the search operator.
    /// </summary>
    public string? Operator { get; set; }

    /// <summary>
    /// Gets or sets the searched value.
    /// </summary>
    public string? Value { get; set; }
}

// <copyright file="AkuiteoContactSearchProviderResult.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Models.Results;

/// <summary>
/// Represents the technical result of searching contacts in Akuiteo.
/// </summary>
public class AkuiteoContactSearchProviderResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the downstream operation succeeded.
    /// </summary>
    public required bool IsSuccess { get; set; }

    /// <summary>
    /// Gets or sets the downstream HTTP status code.
    /// </summary>
    public required int StatusCode { get; set; }

    /// <summary>
    /// Gets or sets the contacts returned by Akuiteo.
    /// </summary>
    public IReadOnlyCollection<AkuiteoContactSearchDataResponse> Contacts { get; set; } = [];

    /// <summary>
    /// Gets or sets the normalized downstream error message.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

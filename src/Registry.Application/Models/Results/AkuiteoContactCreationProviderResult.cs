// <copyright file="AkuiteoContactCreationProviderResult.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Models.Results;

/// <summary>
/// Represents the technical result returned by the Akuiteo contact HTTP provider.
/// </summary>
public class AkuiteoContactCreationProviderResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the downstream call succeeded.
    /// </summary>
    public required bool IsSuccess { get; set; }

    /// <summary>
    /// Gets or sets the downstream status code.
    /// </summary>
    public required int StatusCode { get; set; }

    /// <summary>
    /// Gets or sets the created contact identifier.
    /// </summary>
    public string? ContactId { get; set; }

    /// <summary>
    /// Gets or sets the downstream error message.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

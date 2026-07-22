// <copyright file="AkuiteoAccountOperationProviderResult.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Models.Results;

/// <summary>
/// Represents the technical result of an Akuiteo account update request.
/// </summary>
public class AkuiteoAccountOperationProviderResult
{
    /// <summary>Gets or sets a value indicating whether the downstream operation succeeded.</summary>
    public required bool IsSuccess { get; set; }

    /// <summary>Gets or sets the downstream HTTP status code.</summary>
    public required int StatusCode { get; set; }

    /// <summary>Gets or sets the parsed downstream response.</summary>
    public AkuiteoAccountOperationResponse? Response { get; set; }

    /// <summary>Gets or sets the normalized downstream error message.</summary>
    public string? ErrorMessage { get; set; }
}

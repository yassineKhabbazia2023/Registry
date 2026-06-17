// <copyright file="AkuiteoDocumentUploadProviderResult.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Models.Results;

/// <summary>
/// Represents the technical result returned by the Akuiteo document upload HTTP provider.
/// </summary>
public class AkuiteoDocumentUploadProviderResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the downstream upload succeeded.
    /// </summary>
    public required bool IsSuccess { get; set; }

    /// <summary>
    /// Gets or sets the downstream status code.
    /// </summary>
    public required int StatusCode { get; set; }

    /// <summary>
    /// Gets or sets the downstream error message.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the raw downstream response body returned by Akuiteo.
    /// </summary>
    public string? RawResponseBody { get; set; }
}

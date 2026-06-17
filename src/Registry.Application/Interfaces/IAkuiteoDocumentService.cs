// <copyright file="IAkuiteoDocumentService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;
using Application.Models.Results;

namespace Application.Interfaces;

/// <summary>
/// Exposes the Akuiteo document upload workflow.
/// </summary>
public interface IAkuiteoDocumentService
{
    /// <summary>
    /// Uploads one document to Akuiteo or in mock mode depending on the active configuration.
    /// </summary>
    /// <param name="request">The input document upload request received by Registry.</param>
    /// <returns>The document upload outcome.</returns>
    Task<AkuiteoDocumentUploadResponse> UploadDocumentAsync(AkuiteoDocumentUploadRequest request);
}

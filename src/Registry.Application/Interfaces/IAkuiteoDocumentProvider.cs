// <copyright file="IAkuiteoDocumentProvider.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;
using Application.Models.Results;

namespace Application.Interfaces;

/// <summary>
/// Sends document upload requests to the Akuiteo external API.
/// </summary>
public interface IAkuiteoDocumentProvider
{
    /// <summary>
    /// Uploads one document to Akuiteo for the specified account.
    /// </summary>
    /// <param name="request">The outbound Akuiteo document upload request.</param>
    /// <returns>The technical upload result.</returns>
    Task<AkuiteoDocumentUploadProviderResult> UploadDocumentAsync(AkuiteoDocumentUploadRequest request);
}

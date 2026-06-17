// <copyright file="AkuiteoDocumentUploadResponse.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Models.Results;

/// <summary>
/// Represents the Registry response returned after a document upload in Akuiteo.
/// </summary>
public class AkuiteoDocumentUploadResponse
{
    /// <summary>
    /// Gets or sets the Akuiteo account number where the document was uploaded.
    /// </summary>
    public required string AccountNumber { get; set; }

    /// <summary>
    /// Gets or sets the uploaded document name.
    /// </summary>
    public required string DocumentName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the document was uploaded to Akuiteo.
    /// </summary>
    public required bool IsUploaded { get; set; }
}

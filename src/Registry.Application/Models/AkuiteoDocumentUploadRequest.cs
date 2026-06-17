// <copyright file="AkuiteoDocumentUploadRequest.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Models;

/// <summary>
/// Represents a document upload request for Akuiteo.
/// </summary>
public class AkuiteoDocumentUploadRequest
{
    /// <summary>
    /// Gets or sets the Akuiteo account number receiving the document.
    /// </summary>
    public required string AccountNumber { get; set; }

    /// <summary>
    /// Gets or sets the uploaded document name.
    /// </summary>
    public required string DocumentName { get; set; }

    /// <summary>
    /// Gets or sets the uploaded document content type.
    /// </summary>
    public required string ContentType { get; set; }

    /// <summary>
    /// Gets or sets the uploaded document size in bytes.
    /// </summary>
    public required long Length { get; set; }

    /// <summary>
    /// Gets or sets the uploaded document content stream.
    /// </summary>
    public required Stream Content { get; set; }
}

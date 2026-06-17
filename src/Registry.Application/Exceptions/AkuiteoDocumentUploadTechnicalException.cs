// <copyright file="AkuiteoDocumentUploadTechnicalException.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Exceptions;

/// <summary>
/// Represents a technical failure during an Akuiteo document upload.
/// </summary>
public class AkuiteoDocumentUploadTechnicalException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AkuiteoDocumentUploadTechnicalException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public AkuiteoDocumentUploadTechnicalException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AkuiteoDocumentUploadTechnicalException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public AkuiteoDocumentUploadTechnicalException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

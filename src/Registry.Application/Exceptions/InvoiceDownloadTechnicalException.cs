// <copyright file="InvoiceDownloadTechnicalException.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Exceptions;

/// <summary>
/// Represents a technical failure while downloading an invoice pdf from the blob storage.
/// </summary>
public class InvoiceDownloadTechnicalException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvoiceDownloadTechnicalException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public InvoiceDownloadTechnicalException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvoiceDownloadTechnicalException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public InvoiceDownloadTechnicalException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

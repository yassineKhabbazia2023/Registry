// <copyright file="IInvoiceBlobProvider.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models.Results;

namespace Application.Interfaces;

/// <summary>
/// Downloads invoice pdf documents from the invoice blob storage.
/// </summary>
public interface IInvoiceBlobProvider
{
    /// <summary>
    /// Opens the pdf referenced by the given ref.Invoice document path.
    /// </summary>
    /// <param name="documentPath">The blob url stored in ref.Invoice.DocumentPath.</param>
    /// <returns>The pdf, or <see langword="null"/> when the blob does not exist.</returns>
    /// <exception cref="Exceptions.InvoiceDownloadTechnicalException">
    /// Thrown when the document path is unusable or the storage call fails.
    /// </exception>
    Task<InvoiceContentResponse?> GetPdfAsync(string documentPath);
}

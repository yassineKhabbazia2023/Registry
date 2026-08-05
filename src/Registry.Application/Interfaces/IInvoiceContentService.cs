// <copyright file="IInvoiceContentService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models.Results;

namespace Application.Interfaces;

/// <summary>
/// Serves the pdf content of a referenced invoice.
/// </summary>
public interface IInvoiceContentService
{
    /// <summary>
    /// Downloads the pdf of the given invoice when it is known to ref.Invoice for that account.
    /// </summary>
    /// <param name="invoiceNumber">The invoice number to serve.</param>
    /// <param name="accountNumber">The account the invoice must belong to.</param>
    /// <returns>The pdf and its file name, or <see langword="null"/> when the invoice is unknown.</returns>
    /// <exception cref="Exceptions.InvoiceDownloadTechnicalException">
    /// Thrown when the pdf download fails.
    /// </exception>
    Task<InvoiceContentResponse?> GetPdfAsync(string invoiceNumber, string accountNumber);
}

// <copyright file="InvoiceContentService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Models.Results;
using Microsoft.Extensions.Logging;

namespace Application.Services;

/// <summary>
/// Serves the pdf content of a referenced invoice.
/// </summary>
public class InvoiceContentService : IInvoiceContentService
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IInvoiceBlobProvider _invoiceBlobProvider;
    private readonly ILogger<InvoiceContentService> _logger;

    public InvoiceContentService(
        IInvoiceRepository invoiceRepository,
        IInvoiceBlobProvider invoiceBlobProvider,
        ILogger<InvoiceContentService> logger)
    {
        _invoiceRepository = invoiceRepository;
        _invoiceBlobProvider = invoiceBlobProvider;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<InvoiceContentResponse?> GetPdfAsync(string invoiceNumber, string accountNumber)
    {
        var invoice = await _invoiceRepository.GetInsertedByInvoiceAndAccountNumberAsync(invoiceNumber, accountNumber);

        if (invoice is null)
        {
            _logger.LogWarning(
                "Invoice {InvoiceNumber} has no INSERT row in ref.Invoice for account {AccountNumber}",
                invoiceNumber,
                accountNumber);
            return null;
        }

        if (string.IsNullOrWhiteSpace(invoice.DocumentPath))
        {
            _logger.LogWarning(
                "Invoice {InvoiceNumber} (row {InvoiceId}, account {AccountNumber}) carries no DocumentPath",
                invoiceNumber,
                invoice.InvoiceId,
                accountNumber);
            return null;
        }

        return await _invoiceBlobProvider.GetPdfAsync(invoice.DocumentPath);
    }
}

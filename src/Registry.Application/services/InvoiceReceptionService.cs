// <copyright file="InvoiceReceptionService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Exceptions;
using Application.Helpers;
using Application.Interfaces;
using Application.Models;
using Application.Models.Results;
using CsvHelper;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Application.Services;

public class InvoiceReceptionService : IInvoiceReceptionService
{
    private readonly IBlobStorageManager _blobStorageManager;
    private readonly IInvoiceService _invoiceService;
    private readonly IInvoiceEventPublisher _invoiceEventPublisher;
    private readonly ILogger<InvoiceReceptionService> _logger;

    public InvoiceReceptionService(
        IBlobStorageManager blobStorageManager,
        IInvoiceService invoiceService,
        IInvoiceEventPublisher invoiceEventPublisher,
        ILogger<InvoiceReceptionService> logger)
    {
        _blobStorageManager = blobStorageManager;
        _invoiceService = invoiceService;
        _invoiceEventPublisher = invoiceEventPublisher;
        _logger = logger;
    }

    private const string BlobEndpointName = "Invoice";

    public async Task<InvoiceCsvReceptionOutcome> ReceiveAsync(string data)
    {
        List<(RefInvoiceCsv, int, string[])> csvLines;

        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(data)))
        {
            try
            {
                csvLines = CsvFileReader.ReadStreamAsync<RefInvoiceCsv>(stream).ToList();
            }
            catch (HeaderValidationException ex)
            {
                _logger.LogWarning(ex, "Invoice csv rejected: missing exploited columns in header");
                return InvoiceCsvReceptionOutcome.Rejected("Invalid data: Missing columns in header");
            }

            if (!CsvConfig.IsValidCsvFormat(csvLines, typeof(RefInvoiceCsv), out var messageError))
            {
                _logger.LogWarning("Invoice csv rejected: invalid format ({Reason})", messageError);
                return InvoiceCsvReceptionOutcome.Rejected("Invalid data: " + messageError);
            }
        }

        string blobName;
        try
        {
            blobName = await _blobStorageManager.SaveFileAsync(BlobEndpointName, data);
        }
        catch (BlobStorageOperationException ex)
        {
            _logger.LogError(ex, "Invoice csv storage failed");
            return InvoiceCsvReceptionOutcome.Rejected("Something went wrong when saving received csv");
        }

        var summary = await _invoiceService.InsertPendingLinesAsync(csvLines.Select(l => l.Item1).ToList(), blobName);

        if (summary.ValidLines > 0)
        {
            try
            {
                await _invoiceEventPublisher.SendInvoiceLinesBatchEvent(blobName);
            }
            catch (ServiceBusOperationException ex)
            {
                // Self-healing trigger: the next queue message reprocesses the lines still Pending.
                _logger.LogError(ex, "Invoice lines queue trigger failed for blob {BlobName}", blobName);
            }
        }

        return InvoiceCsvReceptionOutcome.Accepted(summary);
    }
}

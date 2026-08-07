// <copyright file="InvoiceService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Consts;
using Application.Interfaces;
using Application.Models;
using Application.Models.Results;
using Microsoft.Extensions.Logging;
using Pulse.Registry.Domain.Entities;
using System.Globalization;

namespace Application.Services;

public class InvoiceService : IInvoiceService
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IValidationHelper<RefInvoiceCsv> _validationHelper;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(
        IInvoiceRepository invoiceRepository,
        IValidationHelper<RefInvoiceCsv> validationHelper,
        ILogger<InvoiceService> logger)
    {
        _invoiceRepository = invoiceRepository;
        _validationHelper = validationHelper;
        _logger = logger;
    }

    private const string DuplicateLineReason = "Duplicate line (operation, accountNumber, invoiceNumber)";
    private const string InvoiceType = "Facture RYDGE";

    public async Task<InvoiceCsvReceptionResult> InsertPendingLinesAsync(List<RefInvoiceCsv> lines, string blobName)
    {
        var result = new InvoiceCsvReceptionResult
        {
            BlobName = blobName,
            TotalLines = lines.Count,
        };

        var validation = _validationHelper.Validate(lines);

        foreach (var error in validation.Errors)
        {
            var reason = string.Join("; ", error.Errors);
            _logger.LogWarning("Invoice csv line {LineNumber} rejected: {Reason} (blob {BlobName})", error.LineNumber, reason, blobName);
            result.Errors.Add(new InvoiceLineError { Line = error.LineNumber, Reason = reason });
        }

        var errorLines = validation.Errors.Select(e => e.LineNumber).ToHashSet();
        var numberedValidLines = lines
            .Select((line, index) => (Line: line, Number: index + 1))
            .Where(x => !errorLines.Contains(x.Number))
            .ToList();

        var existingKeys = await GetExistingKeysAsync(numberedValidLines.Select(x => x.Line.InvoiceNumber!));

        var toInsert = new List<InvoiceEntity>();
        foreach (var (line, number) in numberedValidLines)
        {
            var key = CreateKey(line.Operation!, line.AccountNumber!, line.InvoiceNumber!);
            if (!existingKeys.Add(key))
            {
                _logger.LogWarning("Invoice csv line {LineNumber} rejected: duplicate (blob {BlobName})", number, blobName);
                result.Errors.Add(new InvoiceLineError { Line = number, Reason = DuplicateLineReason });
                continue;
            }

            toInsert.Add(MapToEntity(line));
        }

        if (toInsert.Count > 0)
        {
            await _invoiceRepository.AddRangeAsync(toInsert);
        }

        result.ValidLines = toInsert.Count;
        result.RejectedLines = result.Errors.Count;
        result.Errors = result.Errors.OrderBy(e => e.Line).ToList();

        return result;
    }

    private async Task<HashSet<string>> GetExistingKeysAsync(IEnumerable<string> invoiceNumbers)
    {
        var existing = await _invoiceRepository.GetByInvoiceNumbersAsync(invoiceNumbers.Distinct());
        return existing
            .Select(e => CreateKey(e.Operation, e.AccountNumber, e.InvoiceNumber))
            .ToHashSet();
    }

    private static string CreateKey(string operation, string accountNumber, string invoiceNumber)
    {
        return string.Join('|', operation, accountNumber, invoiceNumber).ToUpperInvariant();
    }

    private static InvoiceEntity MapToEntity(RefInvoiceCsv line)
    {
        return new InvoiceEntity
        {
            AccountNumber = line.AccountNumber!,
            InvoiceNumber = line.InvoiceNumber!,
            InvoiceDate = DateTime.ParseExact(line.InvoiceDate!, CsvDateFormat.Referential, CultureInfo.InvariantCulture),
            DocumentPath = line.DocumentPath!,
            Type = InvoiceType,
            Operation = line.Operation!.ToUpperInvariant(),
            Status = InvoiceStatus.Pending,
            CreatedOn = DateTime.UtcNow,
        };
    }
}

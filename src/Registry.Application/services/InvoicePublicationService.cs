// <copyright file="InvoicePublicationService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Consts;
using Application.Interfaces;
using Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pulse.Back.Events.IntegrationEvents.EventsData;
using Pulse.Registry.Domain.Entities;

namespace Application.Services;

public class InvoicePublicationService : IInvoicePublicationService
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IInvoiceEventPublisher _invoiceEventPublisher;
    private readonly BackGroundJobOptions _options;
    private readonly ILogger<InvoicePublicationService> _logger;

    public InvoicePublicationService(
        IInvoiceRepository invoiceRepository,
        IAccountRepository accountRepository,
        IInvoiceEventPublisher invoiceEventPublisher,
        IOptions<BackGroundJobOptions> options,
        ILogger<InvoicePublicationService> logger)
    {
        _invoiceRepository = invoiceRepository;
        _accountRepository = accountRepository;
        _invoiceEventPublisher = invoiceEventPublisher;
        _options = options.Value;
        _logger = logger;
    }

    private const string InvoiceCategory = "ADMINISTRATIF";

    public async Task ProcessPendingLinesAsync()
    {
        List<InvoiceEntity> chunk;
        while ((chunk = await _invoiceRepository.GetByStatusAsync(InvoiceStatus.Pending, _options.Chunk)).Count > 0)
        {
            await ProcessChunkAsync(chunk);
        }
    }

    private async Task ProcessChunkAsync(List<InvoiceEntity> chunk)
    {
        var existingAccounts = await GetExistingAccountsAsync(chunk);

        var validLines = new List<InvoiceEntity>();
        var rejectedLines = new List<InvoiceEntity>();
        foreach (var line in chunk)
        {
            if (existingAccounts.Contains(line.AccountNumber))
            {
                validLines.Add(line);
            }
            else
            {
                _logger.LogWarning("Invoice line {InvoiceId} rejected: unknown account {AccountNumber}", line.InvoiceId, line.AccountNumber);
                rejectedLines.Add(line);
            }
        }

        var createdEvents = validLines.Where(l => l.Operation == OperationAction.Insert).Select(MapToCreatedEventData).ToList();
        var removedEvents = validLines.Where(l => l.Operation == OperationAction.Delete).Select(MapToRemovedEventData).ToList();

        if (createdEvents.Count > 0)
        {
            await _invoiceEventPublisher.SendInvoiceCreatedEventsAsync(createdEvents);
        }

        if (removedEvents.Count > 0)
        {
            await _invoiceEventPublisher.SendInvoiceRemovedEventsAsync(removedEvents);
        }

        if (validLines.Count > 0)
        {
            await _invoiceRepository.UpdateStatusAsync(validLines.Select(l => l.InvoiceId), InvoiceStatus.Processed);
        }

        if (rejectedLines.Count > 0)
        {
            await _invoiceRepository.UpdateStatusAsync(rejectedLines.Select(l => l.InvoiceId), InvoiceStatus.Rejected);
        }

        _logger.LogInformation("Invoice lines chunk processed: {ValidCount} published, {RejectedCount} rejected", validLines.Count, rejectedLines.Count);
    }

    private async Task<HashSet<string>> GetExistingAccountsAsync(List<InvoiceEntity> chunk)
    {
        var accountNumbers = chunk.Select(l => l.AccountNumber).Distinct(StringComparer.OrdinalIgnoreCase);
        var existing = await _accountRepository.GetExistingAccountNumbersAsync(accountNumbers);

        return new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);
    }

    private static RegistryInvoiceCreatedEventData MapToCreatedEventData(InvoiceEntity line)
    {
        return new RegistryInvoiceCreatedEventData
        {
            InvoiceNumber = line.InvoiceNumber,
            AccountNumber = line.AccountNumber,
            DocumentPath = line.DocumentPath,
            InvoiceDate = line.InvoiceDate,
            DepositDate = line.CreatedOn,
            Type = line.Type,
            Category = InvoiceCategory,
        };
    }

    private static RegistryInvoiceRemovedEventData MapToRemovedEventData(InvoiceEntity line)
    {
        return new RegistryInvoiceRemovedEventData
        {
            InvoiceNumber = line.InvoiceNumber,
        };
    }
}

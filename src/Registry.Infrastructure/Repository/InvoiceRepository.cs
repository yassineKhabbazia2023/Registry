// <copyright file="InvoiceRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Pulse.Registry.Domain.Context;
using Pulse.Registry.Domain.Entities;

namespace Infrastructure.Repository;

public class InvoiceRepository : IInvoiceRepository
{
    private readonly RefContext dbContext;

    public InvoiceRepository(RefContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<List<InvoiceEntity>> GetByInvoiceNumbersAsync(IEnumerable<string> invoiceNumbers)
    {
        var result = new List<InvoiceEntity>();

        foreach (var batch in invoiceNumbers.Distinct().Chunk(QueryBatching.BatchSize))
        {
            result.AddRange(await this.dbContext.InvoiceEntity
                .Where(i => batch.Contains(i.InvoiceNumber))
                .ToListAsync());
        }

        return result;
    }

    public async Task AddRangeAsync(IEnumerable<InvoiceEntity> invoices)
    {
        await this.dbContext.BulkInsertAsync(invoices);
    }

    public async Task<List<InvoiceEntity>> GetByStatusAsync(string status, int take)
    {
        return await this.dbContext.InvoiceEntity
            .AsNoTracking()
            .Where(i => i.Status == status)
            .OrderBy(i => i.InvoiceId)
            .Take(take)
            .ToListAsync();
    }

    public async Task UpdateStatusAsync(IEnumerable<int> invoiceIds, string status)
    {
        foreach (var batch in invoiceIds.Distinct().Chunk(QueryBatching.BatchSize))
        {
            await this.dbContext.InvoiceEntity
                .Where(i => batch.Contains(i.InvoiceId))
                .ExecuteUpdateAsync(s => s.SetProperty(i => i.Status, status));
        }
    }
}

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
    // Bounds the IN/OPENJSON query size: a single file can carry 50k numbers.
    private const int QueryBatchSize = 2000;

    private readonly RefContext dbContext;

    public InvoiceRepository(RefContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<List<InvoiceEntity>> GetByInvoiceNumbersAsync(IEnumerable<string> invoiceNumbers)
    {
        var result = new List<InvoiceEntity>();

        foreach (var batch in invoiceNumbers.Distinct().Chunk(QueryBatchSize))
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
}

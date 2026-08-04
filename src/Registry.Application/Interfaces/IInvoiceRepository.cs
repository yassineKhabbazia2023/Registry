// <copyright file="IInvoiceRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Pulse.Registry.Domain.Entities;

namespace Application.Interfaces;

public interface IInvoiceRepository
{
    Task<List<InvoiceEntity>> GetByInvoiceNumbersAsync(IEnumerable<string> invoiceNumbers);

    Task AddRangeAsync(IEnumerable<InvoiceEntity> invoices);

    Task<List<InvoiceEntity>> GetByStatusAsync(string status, int take);

    Task UpdateStatusAsync(IEnumerable<int> invoiceIds, string status);
}

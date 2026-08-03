// <copyright file="IInvoiceReceptionService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models.Results;

namespace Application.Interfaces;

public interface IInvoiceReceptionService
{
    /// <summary>
    /// Runs the reception pipeline: format validation, raw storage, line
    /// processing and asynchronous trigger.
    /// </summary>
    /// <param name="data">Raw csv content.</param>
    /// <returns>The reception outcome.</returns>
    Task<InvoiceCsvReceptionOutcome> ReceiveAsync(string data);
}

// <copyright file="IInvoiceService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;
using Application.Models.Results;

namespace Application.Interfaces;

public interface IInvoiceService
{
    /// <summary>
    /// Inserts the valid lines as Pending and summarizes the rejections.
    /// </summary>
    /// <param name="lines">Lines extracted from the csv, in file order.</param>
    /// <param name="blobName">Name of the stored blob.</param>
    /// <returns>The reception summary.</returns>
    Task<InvoiceCsvReceptionResult> InsertPendingLinesAsync(List<RefInvoiceCsv> lines, string blobName);
}

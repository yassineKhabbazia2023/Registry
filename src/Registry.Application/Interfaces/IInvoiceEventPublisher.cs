// <copyright file="IInvoiceEventPublisher.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Interfaces;

public interface IInvoiceEventPublisher
{
    /// <summary>
    /// Sends the blob name to the dedicated queue.
    /// </summary>
    /// <param name="blobName">Name of the stored csv blob.</param>
    Task SendInvoiceLinesBatchEvent(string blobName);
}

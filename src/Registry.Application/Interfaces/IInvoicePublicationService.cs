// <copyright file="IInvoicePublicationService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Interfaces;

public interface IInvoicePublicationService
{
    /// <summary>
    /// Processes the pending invoice lines.
    /// </summary>
    Task ProcessPendingLinesAsync();
}

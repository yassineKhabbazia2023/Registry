// <copyright file="IInvoiceDematerializationNotifier.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Requests;

namespace Application.Interfaces;

public interface IInvoiceDematerializationNotifier
{
    /// <summary>
    /// Notifies the middle office that an invoice dematerialization has been created.
    /// </summary>
    /// <param name="accountNumber">The account number associated with the submission.</param>
    /// <param name="request">The submission input request containing user and vault contact details.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task NotifyDematerializationCreatedAsync(string? accountNumber, HubSpotSubmissionInputRequest request);
}

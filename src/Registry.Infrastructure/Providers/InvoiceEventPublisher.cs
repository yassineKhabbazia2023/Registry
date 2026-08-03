// <copyright file="InvoiceEventPublisher.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Exceptions;
using Application.Interfaces;
using Application.Options;
using Microsoft.Extensions.Options;
using Registry.Infrastructure.Managers;

namespace Infrastructure.Providers;

public class InvoiceEventPublisher : IInvoiceEventPublisher
{
    private readonly INotificationManager notificationManager;
    private readonly InvoiceOptions options;

    public InvoiceEventPublisher(INotificationManager notificationManager, IOptions<InvoiceOptions> options)
    {
        this.notificationManager = notificationManager;
        this.options = options.Value;
    }

    public async Task SendInvoiceLinesBatchEvent(string blobName)
    {
        if (string.IsNullOrWhiteSpace(this.options.InvoiceLinesQueueName))
        {
            throw new ServiceBusOperationException("Invoice lines queue name is not configured.");
        }

        await this.notificationManager.SendMessageToQueueAsync(blobName, this.options.InvoiceLinesQueueName);
    }
}

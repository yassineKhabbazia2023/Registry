// <copyright file="ProcessInvoiceLines.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Registry.AzureFuctions.Functions
{
    /// <summary>
    /// Azure Function processing the pending invoice lines when a csv reception message arrives.
    /// </summary>
    public class ProcessInvoiceLines
    {
        private readonly ILogger<ProcessInvoiceLines> logger;
        private readonly IInvoicePublicationService invoicePublicationService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessInvoiceLines"/> class.
        /// </summary>
        /// <param name="loggerFactory">The logger factory used to create loggers.</param>
        /// <param name="invoicePublicationService">The service publishing the pending invoice lines.</param>
        public ProcessInvoiceLines(ILoggerFactory loggerFactory, IInvoicePublicationService invoicePublicationService)
        {
            this.logger = loggerFactory.CreateLogger<ProcessInvoiceLines>();
            this.invoicePublicationService = invoicePublicationService;
        }

        /// <summary>
        /// Processes the pending ref.Invoice lines and settles the queue message on success.
        /// </summary>
        /// <param name="message">The queue message carrying the received csv blob name.</param>
        /// <param name="messageActions">The Service Bus settlement actions.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        [Function(nameof(ProcessInvoiceLines))]
        public async Task Run(
            [ServiceBusTrigger("%Invoice:InvoiceLinesQueueName%", Connection = "ServiceBusConnectionString", AutoCompleteMessages = false)]
            ServiceBusReceivedMessage message,
            ServiceBusMessageActions messageActions)
        {
            this.logger.LogInformation("Invoice lines processing triggered by blob {BlobName}", message.Body.ToString());

            await this.invoicePublicationService.ProcessPendingLinesAsync();

            // No completion on failure: the message is redelivered and the lines still Pending are retried.
            await messageActions.CompleteMessageAsync(message);
        }
    }
}

// <copyright file="InvoiceOptions.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Options
{
    /// <summary>
    /// Invoices csv flow options.
    /// </summary>
    public class InvoiceOptions
    {
        /// <summary>
        /// Name of the Service Bus queue dedicated to the invoice lines asynchronous processing.
        /// </summary>
        public string InvoiceLinesQueueName { get; set; } = string.Empty;
    }
}

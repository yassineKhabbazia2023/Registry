// <copyright file="ProcessEventPublishOptions.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace ContactRegistry.AzureFuctions.Options
{
    /// <summary>
    /// ProcessEventPublishOptions.
    /// </summary>
    public class ProcessEventPublishOptions
    {
        /// <summary>
        /// Gets or sets ProcessEventPublishBatchSize.
        /// </summary>
        public required int ProcessEventPublishBatchSize { get; set; }
    }
}

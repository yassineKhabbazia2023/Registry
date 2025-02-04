// <copyright file="IReplaySafeLoggerAdapter.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Registry.AzureFuctions.Logging
{
    using Microsoft.DurableTask;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Defines an interface for creating replay-safe logger instances.
    /// </summary>
    public interface IReplaySafeLoggerAdapter
    {
        /// <summary>
        /// Creates a replay-safe logger instance.
        /// </summary>
        /// <param name="context">The task orchestration context.</param>
        /// <param name="categoryName">The category name for the logger.</param>
        /// <returns>A replay-safe logger instance.</returns>
        ILogger CreateReplaySafeLogger(TaskOrchestrationContext context, string categoryName);
    }
}

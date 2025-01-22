// <copyright file="ReplaySafeLoggerAdapter.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace ContactRegistry.AzureFuctions.Logging
{
    /// <summary>
    /// Provides a wrapper around the ILoggerFactory's CreateLogger method,
    /// ensuring that the created logger is replay-safe.
    /// </summary>
    public class ReplaySafeLoggerAdapter : IReplaySafeLoggerAdapter
    {
        /// <summary>
        /// Creates a replay-safe logger instance.
        /// </summary>
        /// <param name="context">The task orchestration context.</param>
        /// <param name="categoryName">The category name for the logger.</param>
        /// <returns>A replay-safe logger instance.</returns>
        public ILogger CreateReplaySafeLogger(TaskOrchestrationContext context, string categoryName)
        {
            return context.CreateReplaySafeLogger(categoryName);
        }
    }
}

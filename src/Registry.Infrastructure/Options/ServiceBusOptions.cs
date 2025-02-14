// <copyright file="ServiceBusOptions.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Registry.Infrastructure
{
    /// <summary>
    /// Defining configuration variables for service bus.
    /// </summary>
    public class ServiceBusOptions
    {
        /// <summary>
        /// Gets or sets ServiceBusContactQueueName.
        /// </summary>
        public required string ServiceBusContactQueueName { get; set; }

        /// <summary>
        /// Gets or sets ServiceBusRegistryTopicName.
        /// </summary>
        public required string ServiceBusRegistryTopicName { get; set; }
    }
}

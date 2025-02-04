// <copyright file="BrokerSetting.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Registry.AzureFuctions
{
    /// <summary>
    /// BrokerSetting.
    /// </summary>
    public class BrokerSetting
    {
        /// <summary>
        /// Gets or sets fullyQualifiedNamespace.
        /// </summary>
        public string? FullyQualifiedNamespace { get; set; }

        /// <summary>
        /// Gets or sets clientId.
        /// </summary>
        public string? ClientId { get; set; }

        /// <summary>
        /// Gets or sets PushTopicNames.
        /// </summary>
        public List<string> PushTopicNames { get; set; } = new List<string>();
    }
}

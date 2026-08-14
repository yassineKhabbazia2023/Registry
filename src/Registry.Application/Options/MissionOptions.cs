// <copyright file="MissionOptions.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Options
{
    /// <summary>
    /// Missions csv flow options.
    /// </summary>
    public class MissionOptions
    {
        /// <summary>
        /// Name of the Service Bus queue dedicated to the mission lines asynchronous processing.
        /// </summary>
        public string MissionLinesQueueName { get; set; } = string.Empty;
    }
}

// <copyright file="MissionCreatedEventHandler.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Consts;
using Application.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Pulse.Back.Events.IntegrationEvents;

namespace Registry.AzureFuctions.Functions
{
    /// <summary>
    /// Handles MissionCreatedEvent, published by Offer once the subscription of an engagement
    /// has been created. Confirms the matching INSERT line of mission.Processing.
    /// </summary>
    public class MissionCreatedEventHandler
    {
        private readonly ILogger<MissionCreatedEventHandler> logger;
        private readonly IMissionConfirmationService missionConfirmationService;

        /// <summary>
        /// Initializes a new instance of the <see cref="MissionCreatedEventHandler"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="missionConfirmationService">The service confirming mission lines answered by Offer.</param>
        public MissionCreatedEventHandler(ILogger<MissionCreatedEventHandler> logger, IMissionConfirmationService missionConfirmationService)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.missionConfirmationService = missionConfirmationService ?? throw new ArgumentNullException(nameof(missionConfirmationService));
        }

        /// <summary>
        /// Processes MissionCreatedEvent from Service Bus.
        /// </summary>
        /// <param name="event">The event payload from Service Bus.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        [Function(nameof(ProcessMissionCreatedAsync))]
        public async Task ProcessMissionCreatedAsync(
            [ServiceBusTrigger(
                "%Mission:OfferTopicName%",
                "%Mission:MissionCreatedSubscriptionName%",
                Connection = "ServiceBusConnectionString")]
            MissionCreatedEvent @event,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(@event);

            if (string.IsNullOrWhiteSpace(@event.Data?.EngagementCode))
            {
                this.logger.LogError("MissionCreatedEvent received without an engagement code");
                return;
            }

            this.logger.LogInformation("Processing MissionCreatedEvent for engagement code {EngagementCode}", @event.Data.EngagementCode);

            // The runtime settles the message when the method returns. An exception leaves it
            // unsettled instead, so redelivered then dead-lettered, which is what a technical
            // failure deserves; a line that cannot be found is a normal case the service handles
            // without throwing.
            await this.missionConfirmationService.ConfirmAsync(@event.Data.EngagementCode, OperationAction.Insert, cancellationToken);
        }
    }
}

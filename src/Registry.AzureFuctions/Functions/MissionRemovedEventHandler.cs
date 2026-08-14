// <copyright file="MissionRemovedEventHandler.cs" company="Pulse">
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
    /// Handles MissionRemovedEvent, published by Offer once an engagement has been deactivated.
    /// Confirms the matching DELETE line of mission.Processing.
    /// <para>
    /// Offer answers here even when it found no engagement to deactivate: when the mapping was
    /// missing at INSERT time no row was ever created on its side, so there is nothing to revoke
    /// and the target state is already reached.
    /// </para>
    /// </summary>
    public class MissionRemovedEventHandler
    {
        private readonly ILogger<MissionRemovedEventHandler> logger;
        private readonly IMissionConfirmationService missionConfirmationService;

        /// <summary>
        /// Initializes a new instance of the <see cref="MissionRemovedEventHandler"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="missionConfirmationService">The service confirming mission lines answered by Offer.</param>
        public MissionRemovedEventHandler(ILogger<MissionRemovedEventHandler> logger, IMissionConfirmationService missionConfirmationService)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.missionConfirmationService = missionConfirmationService ?? throw new ArgumentNullException(nameof(missionConfirmationService));
        }

        /// <summary>
        /// Processes MissionRemovedEvent from Service Bus.
        /// </summary>
        /// <param name="event">The event payload from Service Bus.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        [Function(nameof(ProcessMissionRemovedAsync))]
        public async Task ProcessMissionRemovedAsync(
            [ServiceBusTrigger(
                "%Mission:OfferTopicName%",
                "%Mission:MissionRemovedSubscriptionName%",
                Connection = "ServiceBusConnectionString")]
            MissionRemovedEvent @event,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(@event);

            if (string.IsNullOrWhiteSpace(@event.Data?.EngagementCode))
            {
                this.logger.LogError("MissionRemovedEvent received without an engagement code");
                return;
            }

            this.logger.LogInformation("Processing MissionRemovedEvent for engagement code {EngagementCode}", @event.Data.EngagementCode);

            await this.missionConfirmationService.ConfirmAsync(@event.Data.EngagementCode, OperationAction.Delete, cancellationToken);
        }
    }
}

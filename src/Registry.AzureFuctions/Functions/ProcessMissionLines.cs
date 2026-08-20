// <copyright file="ProcessMissionLines.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Registry.AzureFuctions.Functions
{
    /// <summary>
    /// Azure Function processing pending mission lines.
    /// Triggers: ServiceBusTrigger on CSV reception and TimerTrigger for periodic batch processing.
    /// </summary>
    public class ProcessMissionLines
    {
        private readonly ILogger<ProcessMissionLines> _logger;
        private readonly IMissionPublicationService _missionPublicationService;
        private readonly IMissionReaperService _missionReaperService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessMissionLines"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="missionPublicationService">The service publishing pending mission lines.</param>
        /// <param name="missionReaperService">The service flipping unacknowledged SENT lines to FAILED.</param>
        public ProcessMissionLines(ILogger<ProcessMissionLines> logger, IMissionPublicationService missionPublicationService, IMissionReaperService missionReaperService)
        {
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this._missionPublicationService = missionPublicationService ?? throw new ArgumentNullException(nameof(missionPublicationService));
            this._missionReaperService = missionReaperService ?? throw new ArgumentNullException(nameof(missionReaperService));
        }

        /// <summary>
        /// Processes pending mission lines triggered by CSV blob arrival on Service Bus.
        /// </summary>
        /// <param name="message">The queue message carrying the received CSV blob name.</param>
        /// <param name="messageActions">The Service Bus settlement actions.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        [Function(nameof(ProcessMissionLinesOnCsvReceivedAsync))]
        public async Task ProcessMissionLinesOnCsvReceivedAsync(
            [ServiceBusTrigger(
                "%Mission:MissionLinesQueueName%",
                Connection = "ServiceBusConnectionString",
                AutoCompleteMessages = false)]
            ServiceBusReceivedMessage message,
            ServiceBusMessageActions messageActions,
            CancellationToken cancellationToken)
        {
            try
            {
                await this._missionPublicationService.ProcessPendingLinesAsync(cancellationToken);
                await messageActions.CompleteMessageAsync(message, cancellationToken);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error processing mission lines from CSV trigger");

                // No completion on failure: the message is redelivered and processing retried.
                throw;
            }
        }

        /// <summary>
        /// Processes pending mission lines on a periodic timer schedule (daily).
        /// Reaps unacknowledged SENT lines to FAILED, then processes pending lines: this also
        /// ensures lines waiting for account resolution are retried periodically. A reap failure
        /// is logged but does not prevent the publication pass from running this cycle.
        /// </summary>
        /// <param name="timer">The timer information.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        [Function(nameof(ProcessMissionLinesOnTimerAsync))]
        public async Task ProcessMissionLinesOnTimerAsync([TimerTrigger("%Mission:ProcessingSchedule%")] TimerInfo timer, CancellationToken cancellationToken)
        {
            if (timer.IsPastDue)
            {
                this._logger.LogWarning("Mission processing timer is running past schedule");
            }

            try
            {
                await this._missionReaperService.ReapUnacknowledgedMissionsAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                // Logged, not rethrown: a transient reap failure must not suspend the retry of
                // READY lines below, which is this timer's original purpose.
                this._logger.LogError(ex, "Error reaping unacknowledged mission lines");
            }

            try
            {
                await this._missionPublicationService.ProcessPendingLinesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error processing mission lines from timer trigger");
                throw;
            }
        }
    }
}

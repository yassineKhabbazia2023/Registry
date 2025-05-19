// <copyright file="CreateOfferSubscriptionEvents.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Mappers;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Registry.AzureFuctions.Functions
{
    /// <summary>
    /// Azure Function to process offers associated with a specific batch ID and publish subscription events.
    /// </summary>
    public class CreateOfferSubscriptionEvents
    {
        private readonly ILogger<RunDeepValidationsDurable> logger;
        private readonly IOfferEventPublisher offerEventPublisher;
        private readonly IOfferRepository offerRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateOfferSubscriptionEvents"/> class.
        /// </summary>
        /// <param name="loggerFactory">The logger factory used to create loggers.</param>
        /// <param name="eventPublisher">The event publisher used to publish offer events.</param>
        /// <param name="offerRepository">The repository used to retrieve offers by batch ID.</param>
        public CreateOfferSubscriptionEvents(ILoggerFactory loggerFactory, IOfferEventPublisher eventPublisher, IOfferRepository offerRepository)
        {
            this.logger = loggerFactory.CreateLogger<RunDeepValidationsDurable>();
            this.offerEventPublisher = eventPublisher;
            this.offerRepository = offerRepository;
        }

        /// <summary>
        /// Processes offers for a given batch ID and publishes subscription events.
        /// </summary>
        /// <param name="batchId">The unique identifier of the batch to process.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <remarks>
        /// This function is triggered by a Service Bus message from the "registry-offer-batch" topic.
        /// It retrieves offers associated with the batch ID from the repository, logs the count of offers found,
        /// and publishes the offers as subscription events using the event publisher.
        /// </remarks>
        [Function(nameof(CreateOfferSubscriptionEvents))]
        public async Task Run(
            [ServiceBusTrigger("%OffersMigration:RegistryOfferBatchQueueName%", Connection = "ServiceBusConnectionString")]
            string batchId)
        {

            var offers = await this.offerRepository.GetOffersByBatchIdAsync(Guid.Parse(batchId));

            this.logger.LogInformation($"Found {offers.Count()} offers for batch ID: {batchId}");

            await this.offerEventPublisher.BulkPublishOfferEventAsync(offers.Select(o => o.MapOfferEntityToOffer()).ToList());
            this.logger.LogInformation($"Published {offers.Count()} offer events for batch ID: {batchId}");
        }
    }
}

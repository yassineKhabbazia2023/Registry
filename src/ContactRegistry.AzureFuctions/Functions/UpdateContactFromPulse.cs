// <copyright file="UpdateContactFromPulse.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Azure.Messaging.ServiceBus;
using Infrastructure.Context;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Pulse.Back.Events.IntegrationEvents;
using System.Text;

namespace ContactRegistry.AzureFuctions.Functions
{
    /// <summary>
    /// Sync Contact update event from Pulse to CRE Contact Table.
    /// </summary>
    public class UpdateContactFromPulse
    {
        private readonly ILogger<UpdateContactFromPulse> logger;
        private readonly IDbContextFactory<ApplicationDbContext> dbContextFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateContactFromPulse"/> class.
        /// </summary>
        /// <param name="logger">logger.</param>
        /// <param name="contextFactory">contextFactory.</param>
        public UpdateContactFromPulse(ILogger<UpdateContactFromPulse> logger, IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            this.logger = logger;
            this.dbContextFactory = contextFactory;
        }

        /// <summary>
        /// Run.
        /// </summary>
        /// <param name="message">message.</param>
        /// <param name="messageActions">messageActions.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Function(nameof(UpdateContactFromPulse))]
        public async Task Run(
            [ServiceBusTrigger("%ContactTopic%", "%UpdateContactTopicSub%", Connection = "hubServiceBus")]
            ServiceBusReceivedMessage message,
            ServiceBusMessageActions messageActions)
        {
            this.logger.LogInformation("Message ID: {id}", message.MessageId);
            this.logger.LogDebug("Message Body: {body}", message.Body);
            this.logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);

            if (messageActions == null)
            {
                throw new ArgumentNullException("messageActions");
            }

            var updateContactEvent = JsonConvert.DeserializeObject<ContactUpdatedEvent>(Encoding.UTF8.GetString(message.Body));

            if (updateContactEvent == null || updateContactEvent.Data == null)
            {
                this.logger.LogError("UpdateContactFromPulse Item {id} failed when DeserializeObject", message.MessageId);
                await messageActions.DeadLetterMessageAsync(message, default, $"UpdateContactFromPulse Item {message.MessageId} failed when DeserializeObject");
                return;
            }

            using var applicationContext = await this.dbContextFactory.CreateDbContextAsync();

            var contact = await applicationContext.CreContacts.Where(c => c.Id == updateContactEvent.Data.ContactGlobalUniqueId)
                 .FirstOrDefaultAsync();

            if (contact == null)
            {
                this.logger.LogError("UpdateContactFromPulse contact with id {id} was not found.", updateContactEvent!.Data!.ContactGlobalUniqueId);
                await messageActions.DeadLetterMessageAsync(message, default, $"UpdateContactFromPulse contact with id {updateContactEvent.Data.ContactGlobalUniqueId} was not found.");
                return;
            }

            contact.MobilePhone = updateContactEvent.Data.MobilePhone ?? string.Empty;
            contact.LandPhone = updateContactEvent.Data.LandPhone ?? string.Empty;
            contact.Updated = DateTime.UtcNow;

            applicationContext.CreContacts.Update(contact);
            await applicationContext.SaveChangesAsync();

            // Complete the message
            await messageActions.CompleteMessageAsync(message);
        }
    }
}

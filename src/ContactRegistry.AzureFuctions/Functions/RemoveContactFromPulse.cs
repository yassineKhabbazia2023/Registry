// <copyright file="RemoveContactFromPulse.cs" company="Pulse">
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
    /// Sync Contact removed event from Pulse to CRE Contact Table.
    /// </summary>
    public class RemoveContactFromPulse
    {
        private readonly ILogger<RemoveContactFromPulse> logger;
        private readonly IDbContextFactory<ApplicationDbContext> dbContextFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoveContactFromPulse"/> class.
        /// </summary>
        /// <param name="logger">logger.</param>
        /// <param name="contextFactory">contextFactory.</param>
        public RemoveContactFromPulse(ILogger<RemoveContactFromPulse> logger, IDbContextFactory<ApplicationDbContext> contextFactory)
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
        [Function(nameof(RemoveContactFromPulse))]
        public async Task Run(
            [ServiceBusTrigger("#ContactTopic#", "#RemoveContactTopicSub#", Connection = "hubServiceBus")]
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

            var revokeContactEvent = JsonConvert.DeserializeObject<ContactRemovedEvent>(Encoding.UTF8.GetString(message.Body));

            if (revokeContactEvent == null || revokeContactEvent.Data == null)
            {
                this.logger.LogError("RemoveContactFromPulse Item {id} failed when DeserializeObject", message.MessageId);
                await messageActions.DeadLetterMessageAsync(message, default, $"RemoveContactFromPulse Item {message.MessageId} failed when DeserializeObject");
                return;
            }

            using var applicationContext = await this.dbContextFactory.CreateDbContextAsync();

            var contact = await applicationContext.CreContacts.Where(c => c.Id == revokeContactEvent.Data.ContactGlobalUniqueId!.Value)
                 .FirstOrDefaultAsync();

            if (contact == null)
            {
                this.logger.LogError("RemoveContactFromPulse : Contact with id {id} was not found.", revokeContactEvent!.Data!.ContactId);
                await messageActions.DeadLetterMessageAsync(message, default, $"RemoveContactFromPulse : contact with id {revokeContactEvent.Data.ContactId} was not found.");
                return;
            }

            contact.Deleted = DateTime.UtcNow;

            applicationContext.CreContacts.Update(contact);
            await applicationContext.SaveChangesAsync();

            // Complete the message
            await messageActions.CompleteMessageAsync(message);
        }
    }
}

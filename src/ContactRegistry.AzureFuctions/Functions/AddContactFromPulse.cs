// <copyright file="AddContactFromPulse.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Azure.Messaging.ServiceBus;
using Domain.Entities;
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
    /// Sync Contact creation event from Pulse to CRE Contact Table.
    /// </summary>
    public class AddContactFromPulse
    {
        private readonly ILogger<AddContactFromPulse> logger;
        private readonly IDbContextFactory<ApplicationDbContext> dbContextFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddContactFromPulse"/> class.
        /// </summary>
        /// <param name="logger">logger.</param>
        /// <param name="contextFactory">contextFactory.</param>
        public AddContactFromPulse(ILogger<AddContactFromPulse> logger, IDbContextFactory<ApplicationDbContext> contextFactory)
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
        [Function(nameof(AddContactFromPulse))]
        public async Task Run(
            [ServiceBusTrigger("#ContactTopic#", "#AddContactTopicSub#", Connection = "hubServiceBus")]
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

            var addContactEvent = JsonConvert.DeserializeObject<ContactCreatedEvent>(Encoding.UTF8.GetString(message.Body));

            if (addContactEvent == null || addContactEvent!.Data == null)
            {
                this.logger.LogError("AddContactFromPulse Item {id} failed when DeserializeObject", message.MessageId);
                await messageActions.DeadLetterMessageAsync(message, default, $"AddContactFromPulse Item {message.MessageId} failed when DeserializeObject");
                return;
            }

            var contactToAdd = new CreContact()
            {
                Id = addContactEvent.Data.ContactGlobalUniqueId!.Value,
                Email = addContactEvent.Data.Email,
                FirstName = addContactEvent.Data.FirstName,
                LastName = addContactEvent.Data.LastName,
                IsCustomer = true,
                Source = "Pulse",
                IsActive = true,
                MobilePhone = addContactEvent.Data.MobilePhone ?? string.Empty,
                LandPhone = addContactEvent.Data.LandPhone ?? string.Empty,
                JobDescription = string.Empty,
            };

            using var applicationContext = await this.dbContextFactory.CreateDbContextAsync();

            var isContactExist = await applicationContext.CreContacts.Where(c => c.Id == addContactEvent.Data.ContactGlobalUniqueId)
                .AnyAsync();

            if (isContactExist)
            {
                this.logger.LogInformation("AddContactFromPulse contact with id {id} already exist in database.", addContactEvent!.Data!.ContactGlobalUniqueId);
                return;
            }

            await applicationContext.CreContacts.AddAsync(contactToAdd);
            await applicationContext.SaveChangesAsync();

            // Complete the message
            await messageActions.CompleteMessageAsync(message);
        }
    }
}

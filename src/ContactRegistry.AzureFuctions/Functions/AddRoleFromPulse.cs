// <copyright file="AddRoleFromPulse.cs" company="Pulse">
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
using System.Data;
using System.Text;

namespace ContactRegistry.AzureFuctions.Functions
{
    /// <summary>
    /// Sync Role add event from Pulse to CRE Role Table.
    /// </summary>
    public class AddRoleFromPulse
    {
        private readonly ILogger<AddRoleFromPulse> logger;
        private readonly IDbContextFactory<ApplicationDbContext> dbContextFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddRoleFromPulse"/> class.
        /// </summary>
        /// <param name="logger">logger.</param>
        /// <param name="contextFactory">contextFactory.</param>
        public AddRoleFromPulse(ILogger<AddRoleFromPulse> logger, IDbContextFactory<ApplicationDbContext> contextFactory)
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
        [Function(nameof(AddRoleFromPulse))]
        public async Task Run(
            [ServiceBusTrigger("#AccountTopic#", "#AddRoleTopicSub#", Connection = "hubServiceBus")]
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

            var addRoleEvent = JsonConvert.DeserializeObject<RoleCreatedEvent>(Encoding.UTF8.GetString(message.Body));

            if (addRoleEvent == null || addRoleEvent!.Data == null)
            {
                this.logger.LogError("AddRoleFromPulse Item {id} failed when DeserializeObject", message.MessageId);
                await messageActions.DeadLetterMessageAsync(message, default, $"AddRoleFromPulse Item {message.MessageId} failed when DeserializeObject");
                return;
            }

            using var applicationContext = await this.dbContextFactory.CreateDbContextAsync();

            var roleToAdd = new CreRole()
            {
                Id = Guid.NewGuid(),
                AccountId = addRoleEvent.Data.AccountGlobalUniqueId,
                ContactId = addRoleEvent.Data.ContactGlobalUniqueId,
            };

            await applicationContext.CreRoles.AddAsync(roleToAdd);
            await applicationContext.SaveChangesAsync();

            // Complete the message
            await messageActions.CompleteMessageAsync(message);
        }
    }
}

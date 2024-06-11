// <copyright file="RemoveRoleFromPulse.cs" company="Pulse">
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
    public class RemoveRoleFromPulse
    {
        private readonly ILogger<RemoveRoleFromPulse> logger;
        private readonly IDbContextFactory<ApplicationDbContext> dbContextFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoveRoleFromPulse"/> class.
        /// </summary>
        /// <param name="logger">logger.</param>
        /// <param name="contextFactory">contextFactory.</param>
        public RemoveRoleFromPulse(ILogger<RemoveRoleFromPulse> logger, IDbContextFactory<ApplicationDbContext> contextFactory)
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
            [ServiceBusTrigger("#AccountTopic#", "#RemoveRoleTopicSub#", Connection = "hubServiceBus")]
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

            var removeRoleEvent = JsonConvert.DeserializeObject<RoleDeletedEvent>(Encoding.UTF8.GetString(message.Body));

            if (removeRoleEvent == null || removeRoleEvent!.Data == null)
            {
                this.logger.LogError("RemoveRoleFromPulse Item {id} failed when DeserializeObject", message.MessageId);
                await messageActions.DeadLetterMessageAsync(message, default, $"RemoveRoleFromPulse Item {message.MessageId} failed when DeserializeObject");
                return;
            }

            using var applicationContext = await this.dbContextFactory.CreateDbContextAsync();

            var roles = await applicationContext.CreRoles
                .Where(c => c.ContactId == removeRoleEvent.Data.ContactGlobalUniqueId
                    && c.AccountId == removeRoleEvent.Data.AccountGlobalUniqueId && c.Deleted != null)
                .ToListAsync();

            foreach (var role in roles)
            {
                role.Deleted = DateTime.UtcNow;
                applicationContext.CreRoles.Update(role);
            }

            await applicationContext.SaveChangesAsync();

            // Complete the message
            await messageActions.CompleteMessageAsync(message);
        }
    }
}

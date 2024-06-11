// <copyright file="ProcessEventPublish.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Azure.Messaging.ServiceBus;
using ContactRegistry.AzureFuctions.Const;
using ContactRegistry.AzureFuctions.Managers;
using ContactRegistry.AzureFuctions.Message;
using Domain.Entities;
using Infrastructure.Context;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Pulse.Back.Events.IntegrationEvents;
using Pulse.Back.Events.IntegrationEvents.EventsData;
using System.Text;

namespace ContactRegistry.AzureFuctions.Functions
{
    /// <summary>
    /// ProcessEventPublish.
    /// </summary>
    public class ProcessEventPublish
    {
        private readonly ILogger<ProcessEventPublish> logger;
        private readonly IDbContextFactory<ApplicationDbContext> dbContextFactory;
        private readonly INotificationManager notificationManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessEventPublish"/> class.
        /// </summary>
        /// <param name="logger">logger.</param>
        /// <param name="contextFactory">contextFactory.</param>
        /// <param name="notificationManager">notificationManager.</param>
        public ProcessEventPublish(ILogger<ProcessEventPublish> logger, IDbContextFactory<ApplicationDbContext> contextFactory, INotificationManager notificationManager)
        {
            this.logger = logger;
            this.dbContextFactory = contextFactory;
            this.notificationManager = notificationManager;
        }

        /// <summary>
        /// Run.
        /// </summary>
        /// <param name="message">message.</param>
        /// <param name="messageActions">messageActions.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Function(nameof(ProcessEventPublish))]
        public async Task Run(
            [ServiceBusTrigger("%ServiceBusQueueProcessName%", Connection = "hubServiceBus")]
            ServiceBusReceivedMessage message,
            ServiceBusMessageActions messageActions)
        {
            this.logger.LogInformation("Message ID: {id}", message.MessageId);
            this.logger.LogDebug("Message Body: {body}", message.Body);
            this.logger.LogDebug("Message Content-Type: {contentType}", message.ContentType);

            if (messageActions == null)
            {
                throw new ArgumentNullException("messageActions");
            }

            var registryEntityType = JsonConvert.DeserializeObject<RegistryEntityType>(Encoding.UTF8.GetString(message.Body));

            switch (registryEntityType!.EntityType)
            {
                case OperationType.Contact:
                    await this.ProcessContactPublishAsync();
                    break;
                case OperationType.Account:
                    await this.ProcessAccountPublishAsync();
                    break;
                case OperationType.Role:
                    await this.ProcessRolePublishAsync();
                    break;
            }

            this.logger.LogInformation("ProcessEventPublish : function completed for {type}", registryEntityType.EntityType);

            // Complete the message
            await messageActions.CompleteMessageAsync(message);
        }

        private async Task ProcessContactPublishAsync()
        {
            using var applicationContext = await this.dbContextFactory.CreateDbContextAsync();

            var operations = await applicationContext.CreOperations
                .Where(o => o.Type == OperationType.Contact && o.PublishedAt == null)
                .ToListAsync();

            foreach (var operation in operations)
            {
                try
                {
                    var contact = await applicationContext.CreContacts
                        .Where(c => c.Id == operation.EntityId).FirstAsync();
                    await this.ProcessContactOperationAsync(operation, contact);
                    await UpdateOperationToPublishAync(applicationContext, operation);
                }
                catch (Exception ex)
                {
                    this.logger.LogError("ProcessEventPublish : ProcessContactPublishAsync publish {operation} ko for contact id '{contactId}. Exception : {message}", operation.Operation, operation.EntityId, ex.Message);
                    continue;
                }
            }
        }

        private async Task ProcessContactOperationAsync(CreOperation operation, CreContact contact)
        {
            switch (operation.Operation)
            {
                case OperationName.Insert:
                    var contactCreatedEvent = new RegistryContactCreatedEventData()
                    {
                        Id = contact.Id,
                        IsCustomer = contact.IsCustomer,
                        FirstName = contact.FirstName,
                        LastName = contact.LastName,
                        Email = contact.Email,
                        OfficeId = contact.OfficeId,
                        LandPhone = contact.LandPhone,
                        MobilePhone = contact.MobilePhone,
                        JobDescription = contact.JobDescription,
                        Source = contact.Source,
                    };
                    await this.notificationManager.PublishAsync(new RegistryContactCreatedEvent(contactCreatedEvent));
                    this.logger.LogInformation("ProcessEventPublish : ProcessContactPublishAsync publish create ok for contact id '{contactId}'", operation.EntityId);
                    break;

                case OperationName.Delete:
                    var contactRemovedEvent = new RegistryContactRemovedEventData()
                    {
                        Id = contact.Id,
                        Email = contact.Email,
                    };
                    await this.notificationManager.PublishAsync(new RegistryContactRemovedEvent(contactRemovedEvent));
                    this.logger.LogInformation("ProcessEventPublish : ProcessContactPublishAsync publish remove ok for contact id '{contactId}'", operation.EntityId);
                    break;

                case OperationName.Update:
                    var contactUpdatedEvent = new RegistryContactUpdatedEventData()
                    {
                        Id = contact.Id,
                        Email = contact.Email,
                        OfficeId = contact.OfficeId,
                        JobDescription = contact.JobDescription,
                        LandPhone = contact.LandPhone,
                        MobilePhone = contact.MobilePhone,
                        LastName = contact.LastName,
                        FirstName = contact.FirstName,
                        IsCustomer = contact.IsCustomer,
                    };
                    await this.notificationManager.PublishAsync(new RegistryContactUpdatedEvent(contactUpdatedEvent));
                    this.logger.LogInformation("ProcessEventPublish : ProcessContactPublishAsync publish update ok for contact id '{contactId}'", operation.EntityId);
                    break;
            }
        }

        private async Task ProcessAccountPublishAsync()
        {
            using var applicationContext = await this.dbContextFactory.CreateDbContextAsync();

            var operations = await applicationContext.CreOperations
                .Where(o => o.Type == OperationType.Account && o.PublishedAt == null)
                .ToListAsync();

            foreach (var operation in operations)
            {
                try
                {
                    var account = await applicationContext.CreAccounts
                        .Where(c => c.Id == operation.EntityId).FirstAsync();
                    await this.ProcessAccountOperationAsync(operation, account);
                    await UpdateOperationToPublishAync(applicationContext, operation);
                }
                catch (Exception ex)
                {
                    this.logger.LogError("ProcessEventPublish : ProcessAccountPublishAsync publish {operation} ko for account id '{contactId}. Exception : {message}", operation.Operation, operation.EntityId, ex.Message);
                    continue;
                }
            }
        }

        private async Task ProcessAccountOperationAsync(CreOperation operation, CreAccount account)
        {
            switch (operation.Operation)
            {
                case OperationName.Insert:
                    var accountCreatedEvent = new RegistryAccountCreatedEventData()
                    {
                        Id = account.Id,
                        AccountNumber = account.AccountNumber,
                        LegalName = account.LegalName,
                    };
                    await this.notificationManager.PublishAsync(new RegistryAccountCreatedEvent(accountCreatedEvent));
                    this.logger.LogInformation("ProcessEventPublish : ProcessAccountPublishAsync publish create ok for account id '{contactId}'", operation.EntityId);
                    break;

                case OperationName.Delete:
                    var accountRemovedEvent = new RegistryAccountRemovedEventData()
                    {
                        Id = account.Id,
                        AccountNumber = account.AccountNumber,
                    };
                    await this.notificationManager.PublishAsync(new RegistryAccountRemovedEvent(accountRemovedEvent));
                    this.logger.LogInformation("ProcessEventPublish : ProcessAccountPublishAsync publish remove ok for account id '{contactId}'", operation.EntityId);
                    break;

                case OperationName.Update:
                    var accountUpdatedEvent = new RegistryAccountUpdatedEventData()
                    {
                        Id = account.Id,
                        AccountNumber = account.AccountNumber,
                    };
                    await this.notificationManager.PublishAsync(new RegistryAccountUpdatedEvent(accountUpdatedEvent));
                    this.logger.LogInformation("ProcessEventPublish : ProcessAccountPublishAsync publish update ok for account id '{contactId}'", operation.EntityId);
                    break;
            }
        }

        private async Task ProcessRolePublishAsync()
        {
            using var applicationContext = await this.dbContextFactory.CreateDbContextAsync();

            var operations = await applicationContext.CreOperations
                .Where(o => o.Type == OperationType.Role && o.PublishedAt == null)
                .ToListAsync();

            foreach (var operation in operations)
            {
                try
                {
                    var role = await applicationContext.CreRoles
                        .Include(r => r.Contact)
                        .Include(r => r.Account)
                        .Where(c => c.Id == operation.EntityId).FirstAsync();
                    await this.ProcessRoleOperationAsync(operation, role, applicationContext);
                    await UpdateOperationToPublishAync(applicationContext, operation);
                }
                catch (Exception ex)
                {
                    this.logger.LogError("ProcessEventPublish : ProcessRolePublishAsync publish {operation} ko for role id '{roleId}. Exception : {message}", operation.Operation, operation.EntityId, ex.Message);
                    continue;
                }
            }
        }

        private async Task ProcessRoleOperationAsync(CreOperation operation, CreRole role, ApplicationDbContext applicationContext)
        {
            var roleEvent = new RegistryRoleEventData()
            {
                AccountId = role.AccountId,
                Email = role.Contact.Email,
                AccountNumber = role.Account.AccountNumber,
                ContactId = role.ContactId,
            };

            var roleCount = await applicationContext.CreRoles
                .Where(c => c.ContactId == role.ContactId && c.AccountId == role.AccountId && c.Deleted == null)
                .CountAsync();

            switch (operation.Operation)
            {
                case OperationName.Insert:
                    if (roleCount == 1)
                    {
                        await this.notificationManager.PublishAsync(new RegistryRoleCreatedEvent(roleEvent));
                        this.logger.LogInformation("ProcessEventPublish : ProcessRolePublishAsync publish create ok for role id '{contactId}'", operation.EntityId);
                    }

                    break;

                case OperationName.Delete:
                    if (roleCount == 0)
                    {
                        await this.notificationManager.PublishAsync(new RegistryRoleCreatedEvent(roleEvent));
                        this.logger.LogInformation("ProcessEventPublish : ProcessRolePublishAsync publish create ok for role id '{contactId}'", operation.EntityId);
                    }

                    await this.notificationManager.PublishAsync(new RegistryRoleRemovedEvent(roleEvent));
                    this.logger.LogInformation("ProcessEventPublish : ProcessRolePublishAsync publish remove ok for role id '{contactId}'", operation.EntityId);
                    break;
            }
        }

        private static async Task UpdateOperationToPublishAync(ApplicationDbContext applicationContext, CreOperation operation)
        {
            operation.PublishedAt = DateTime.UtcNow;
            applicationContext.CreOperations.Update(operation);
            await applicationContext.SaveChangesAsync();
        }
    }
}

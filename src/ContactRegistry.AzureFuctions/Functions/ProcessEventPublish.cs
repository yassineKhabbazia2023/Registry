// <copyright file="ProcessEventPublish.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Azure.Messaging.ServiceBus;
using ContactRegistry.AzureFuctions.Const;
using ContactRegistry.AzureFuctions.Managers;
using ContactRegistry.AzureFuctions.Message;
using ContactRegistry.AzureFuctions.Options;
using Domain.Entities;
using Infrastructure.Context;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Pulse.Back.Events.Abstractions;
using Pulse.Back.Events.IntegrationEvents;
using Pulse.Back.Events.IntegrationEvents.EventsData;
using System.Data;
using System.Text;

namespace ContactRegistry.AzureFuctions.Functions;

/// <summary>
/// ProcessEventPublish.
/// </summary>
public class ProcessEventPublish
{
    private readonly ILogger<ProcessEventPublish> logger;
    private readonly IDbContextFactory<ApplicationDbContext> dbContextFactory;
    private readonly INotificationManager notificationManager;
    private readonly IServiceBusMessageFactory serviceBusMessageFactory;
    private List<ServiceBusMessage> messagesToSendInBatch = new List<ServiceBusMessage>();
    private readonly IOptions<ProcessEventPublishOptions> options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessEventPublish"/> class.
    /// </summary>
    /// <param name="logger">logger.</param>
    /// <param name="contextFactory">contextFactory.</param>
    /// <param name="notificationManager">notificationManager.</param>
    /// <param name="serviceBusMessageFactory">serviceBusMessageFactory.</param>
    /// <param name="operationRepository">operationRepository.</param>
    /// <param name="options">options.</param>
    public ProcessEventPublish(
        ILogger<ProcessEventPublish> logger,
        IDbContextFactory<ApplicationDbContext> contextFactory,
        INotificationManager notificationManager,
        IServiceBusMessageFactory serviceBusMessageFactory,
        IOptions<ProcessEventPublishOptions> options)
    {
        this.logger = logger;
        this.dbContextFactory = contextFactory;
        this.notificationManager = notificationManager;
        this.serviceBusMessageFactory = serviceBusMessageFactory;
        this.options = options;
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

        var query1 = from operation in applicationContext.CreOperations
                     join contact in applicationContext.CreContacts
                     on operation.EntityId equals contact.Id
                     where operation.Type == OperationType.Contact
                     && operation.PublishedAt == null
                     && operation.Status == OperationStatus.Approved
                     select new { Operation = operation, Contact = contact };
        var nbOperation = 0;

        do
        {
            var operationBatch = await query1
                .Take(this.options.Value.ProcessEventPublishBatchSize)
                .ToListAsync();
            nbOperation = operationBatch.Count;

            if (nbOperation == 0)
            {
                break;
            }

            foreach (var row in operationBatch)
            {
                this.ProcessContactOperation(row.Operation, row.Contact);
            }

            await this.SendBatchMessageAsync();
            var operations = operationBatch.Select(o => this.UpdateOperationsToPublisAt(o.Operation)).ToList();
            await this.UpdateOperationsAsync(applicationContext);
        }
        while (nbOperation != 0);
    }

    private void ProcessContactOperation(CreOperation operation, CreContact contact)
    {
        ServiceBusMessage? serviceBusMessage = null;
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

                serviceBusMessage = this.serviceBusMessageFactory.CreateMessage(new RegistryContactCreatedEvent(contactCreatedEvent));
                this.messagesToSendInBatch.Add(serviceBusMessage);
                break;

            case OperationName.Delete:
                var contactRemovedEvent = new RegistryContactRemovedEventData()
                {
                    Id = contact.Id,
                    Email = contact.Email,
                };

                serviceBusMessage = this.serviceBusMessageFactory.CreateMessage(new RegistryContactRemovedEvent(contactRemovedEvent));
                this.messagesToSendInBatch.Add(serviceBusMessage);
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
                    IsActive = contact.IsActive,
                };

                serviceBusMessage = this.serviceBusMessageFactory.CreateMessage(new RegistryContactUpdatedEvent(contactUpdatedEvent));
                this.messagesToSendInBatch.Add(serviceBusMessage);
                break;
        }
    }

    private async Task ProcessAccountPublishAsync()
    {
        using var applicationContext = await this.dbContextFactory.CreateDbContextAsync();

        var query1 = from operation in applicationContext.CreOperations
                     join account in applicationContext.CreAccounts
                     on operation.EntityId equals account.Id
                     where operation.Type == OperationType.Account
                     && operation.PublishedAt == null
                     && operation.Status == OperationStatus.Approved
                     select new { Operation = operation, Account = account };
        var nbOperation = 0;

        do
        {
            var operationBatch = await query1
                .Take(this.options.Value.ProcessEventPublishBatchSize)
                .ToListAsync();
            nbOperation = operationBatch.Count;

            if (nbOperation == 0)
            {
                break;
            }

            foreach (var row in operationBatch)
            {
                this.ProcessAccountOperation(row.Operation, row.Account);
            }

            await this.SendBatchMessageAsync();
            var operations = operationBatch.Select(o => this.UpdateOperationsToPublisAt(o.Operation)).ToList();
            await this.UpdateOperationsAsync(applicationContext);
        }
        while (nbOperation != 0);
    }

    private void ProcessAccountOperation(CreOperation operation, CreAccount account)
    {
        ServiceBusMessage? serviceBusMessage = null;
        switch (operation.Operation)
        {
            case OperationName.Insert:
                var accountCreatedEvent = account.ToRegistryAccountCreatedEventData();
                serviceBusMessage = this.serviceBusMessageFactory.CreateMessage(new RegistryAccountCreatedEvent(accountCreatedEvent));
                this.messagesToSendInBatch.Add(serviceBusMessage);
                break;

            case OperationName.Delete:
                var accountRemovedEvent = new RegistryAccountRemovedEventData()
                {
                    AccountGlobalUniqueIdentifier = account.Id,
                    AccountNumber = account.AccountNumber,
                };

                serviceBusMessage = this.serviceBusMessageFactory.CreateMessage(new RegistryAccountRemovedEvent(accountRemovedEvent));
                this.messagesToSendInBatch.Add(serviceBusMessage);
                break;

            case OperationName.Update:
                var accountUpdatedEvent = account.ToRegistryAccountUpdatedEventData();
                serviceBusMessage = this.serviceBusMessageFactory.CreateMessage(new RegistryAccountUpdatedEvent(accountUpdatedEvent));
                this.messagesToSendInBatch.Add(serviceBusMessage);
                break;
        }
    }

    private async Task ProcessRolePublishAsync()
    {
        using var applicationContext = await this.dbContextFactory.CreateDbContextAsync();

        var query1 = from operation in applicationContext.CreOperations
                     join role in applicationContext.CreRoles.Include(r => r.Contact).Include(r => r.Account)
                     on operation.EntityId equals role.RoleId
                     where operation.Type == OperationType.Role
                     && operation.PublishedAt == null
                     && operation.Status == OperationStatus.Approved
                     select new
                     {
                         Operation = operation,
                         Role = role,
                         RoleCount = applicationContext.CreRoles
                         .Where(c => c.ContactId == role.ContactId && c.AccountId == role.AccountId && c.Deleted == null)
                         .Count(),
                     };

        var nbOperation = 0;
        do
        {
            var operationBatch = await query1
            .Take(this.options.Value.ProcessEventPublishBatchSize)
            .ToListAsync();
            nbOperation = operationBatch.Count;

            if (nbOperation == 0)
            {
                break;
            }

            foreach (var row in operationBatch)
            {
                await this.ProcessRoleOperationAsync(row.Operation, row.Role, row.RoleCount);
            }

            await this.SendBatchMessageAsync();
            var operations = operationBatch.Select(o => this.UpdateOperationsToPublisAt(o.Operation)).ToList();
            await this.UpdateOperationsAsync(applicationContext);
        }
        while (nbOperation != 0);
    }

    private async Task ProcessRoleOperationAsync(CreOperation operation, CreRole role, int roleCount)
    {
        ServiceBusMessage? serviceBusMessage = null;
        switch (operation.Operation)
        {
            case OperationName.Insert:
                if (roleCount == 1)
                {
                    var roleEvent = new RegistryRoleCreatedEventData()
                    {
                        AccountId = role.AccountId,
                        Email = role.Contact.Email,
                        AccountNumber = role.Account.AccountNumber,
                        ContactId = role.ContactId,
                        RoleDelegataireEmail = role.RoleDelegataireEmail,
                        RoleSignatory = role.RoleSignatory,
                        IsFavorite = role.IsFavorite,
                    };

                    serviceBusMessage = this.serviceBusMessageFactory.CreateMessage(new RegistryRoleCreatedEvent(roleEvent));
                    this.messagesToSendInBatch.Add(serviceBusMessage);
                }

                break;

            case OperationName.Delete:
                if (roleCount == 0)
                {
                    var roleEvent = new RegistryRoleRemovedEventData()
                    {
                        AccountId = role.AccountId,
                        Email = role.Contact.Email,
                        AccountNumber = role.Account.AccountNumber,
                        ContactId = role.ContactId,
                    };

                    serviceBusMessage = this.serviceBusMessageFactory.CreateMessage(new RegistryRoleRemovedEvent(roleEvent));
                    this.messagesToSendInBatch.Add(serviceBusMessage);
                }

                break;
        }
    }

    private async Task SendBatchMessageAsync()
    {
        if (this.messagesToSendInBatch.Count == 0)
        {
            return;
        }

        await this.notificationManager.BulkPublishAsync(this.messagesToSendInBatch);
        this.logger.LogInformation("ProcessEventPublish : SendBatchMessageAsync publish '{count}' events success.", this.messagesToSendInBatch.Count);
        this.messagesToSendInBatch.Clear();
    }

    private CreOperation UpdateOperationsToPublisAt(CreOperation operation)
    {
        operation.PublishedAt = DateTime.UtcNow;
        return operation;
    }

    private async Task UpdateOperationsAsync(ApplicationDbContext dbContext)
    {
        await dbContext.SaveChangesAsync();
    }
}

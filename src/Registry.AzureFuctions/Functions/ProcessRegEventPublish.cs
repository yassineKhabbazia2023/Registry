// <copyright file="ProcessRegEventPublish.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using System.Data;
using System.Text;
using Application.Consts;
using Azure.Messaging.ServiceBus;
using Registry.AzureFuctions.Managers;
using Registry.AzureFuctions.Message;
using Registry.AzureFuctions.Options;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Pulse.Back.Events.Abstractions;
using Pulse.Back.Events.IntegrationEvents;
using Pulse.Back.Events.IntegrationEvents.EventsData;
using Pulse.ContactRegistry.Domain.Context;
using Pulse.ContactRegistry.Domain.Entities;
using Registry.Application.Consts;

namespace Registry.AzureFuctions.Functions;

/// <summary>
/// ProcessRegEventPublish.
/// </summary>
public class ProcessRegEventPublish
{
    private readonly ILogger<ProcessRegEventPublish> logger;
    private readonly IDbContextFactory<RefContext> dbContextFactory;
    private readonly INotificationManager notificationManager;
    private readonly IServiceBusMessageFactory serviceBusMessageFactory;
    private readonly List<ServiceBusMessage> messagesToSendInBatch = new List<ServiceBusMessage>();
    private readonly IOptions<ProcessEventPublishOptions> options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessRegEventPublish"/> class.
    /// </summary>
    /// <param name="logger">logger.</param>
    /// <param name="contextFactory">contextFactory.</param>
    /// <param name="notificationManager">notificationManager.</param>
    /// <param name="serviceBusMessageFactory">serviceBusMessageFactory.</param>
    /// <param name="processDeltaTriggerRepository">processDeltaTriggerRepository.</param>
    /// <param name="options">options.</param>
    public ProcessRegEventPublish(
        ILogger<ProcessRegEventPublish> logger,
        IDbContextFactory<RefContext> contextFactory,
        INotificationManager notificationManager,
        IServiceBusMessageFactory serviceBusMessageFactory,
        IOptions<ProcessEventPublishOptions> options)
    {
        this.logger = logger;
        dbContextFactory = contextFactory;
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
    [Function(nameof(ProcessRegEventPublish))]
    public async Task Run(
        [ServiceBusTrigger("%ServiceBusQueueProcessName%", Connection = "hubServiceBus")]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageActions);

        logger.LogInformation("Begin ProcessRegEventPublish Message ID: {Id}", message.MessageId);
        logger.LogDebug("Message Body: {Body}", message.Body);
        logger.LogDebug("Message Content-Type: {ContentType}", message.ContentType);

        var registryEntityType = JsonConvert.DeserializeObject<RegistryEntityType>(Encoding.UTF8.GetString(message.Body));

        switch (registryEntityType!.EntityType)
        {
            case OperationType.Contact:
                await ProcessContactPublishAsync();
                break;
            case OperationType.Account:
                await ProcessAccountPublishAsync();
                break;
            case OperationType.Role:
                await ProcessRolePublishAsync();
                break;
        }

        logger.LogInformation("Function completed for {Type} - ProcessRegEventPublish", registryEntityType.EntityType);

        // Complete the message
        await messageActions.CompleteMessageAsync(message);
    }

    private async Task ProcessContactPublishAsync()
    {
        logger.LogInformation("Send Contact event data executed at: {Date} - ProcessContactPublishAsync", DateTime.UtcNow);

        using var applicationContext = await dbContextFactory.CreateDbContextAsync();

        var query1 = from operation in applicationContext.RegOperationEntity
                     join contact in applicationContext.RegContactEntity
                     on operation.EntityId equals contact.Id
                     where operation.Type == OperationType.Contact
                     && operation.PublishedAt == null
                     && operation.ApprovalStatus == ApprovalStatus.Approved
                     select new { Operation = operation, Contact = contact };
        var nbOperation = 0;

        do
        {
            var operationBatch = await query1
                .Take(options.Value.ProcessEventPublishBatchSize)
                .ToListAsync();
            nbOperation = operationBatch.Count;

            if (nbOperation == 0)
            {
                break;
            }

            foreach (var row in operationBatch)
            {
                ProcessContactOperation(row.Operation, row.Contact);
            }

            await SendBatchMessageAsync();
            var operations = operationBatch.Select(o => UpdateOperationsToPublisAt(o.Operation)).ToList();
            await UpdateOperationsAsync(applicationContext);
        }
        while (nbOperation != 0);

        logger.LogInformation("Send Contact event data succeed at: {Date} - ProcessContactPublishAsync", DateTime.UtcNow);
    }

    private void ProcessContactOperation(RegOperationEntity operation, RegContactEntity contact)
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
                    OfficeCode = contact.OfficeCode,
                    LandPhone = contact.LandPhone,
                    MobilePhone = contact.MobilePhone,
                    JobDescription = contact.JobDescription,
                    Source = contact.Source,
                };

                serviceBusMessage = serviceBusMessageFactory.CreateMessage(new RegistryContactCreatedEvent(contactCreatedEvent));
                messagesToSendInBatch.Add(serviceBusMessage);
                break;

            case OperationName.Delete:
                var contactRemovedEvent = new RegistryContactRemovedEventData()
                {
                    Id = contact.Id,
                    Email = contact.Email,
                };

                serviceBusMessage = serviceBusMessageFactory.CreateMessage(new RegistryContactRemovedEvent(contactRemovedEvent));
                messagesToSendInBatch.Add(serviceBusMessage);
                break;

            case OperationName.Update:
                var contactUpdatedEvent = new RegistryContactUpdatedEventData()
                {
                    Id = contact.Id,
                    Email = contact.Email,
                    OfficeCode = contact.OfficeCode,
                    JobDescription = contact.JobDescription,
                    LandPhone = contact.LandPhone,
                    MobilePhone = contact.MobilePhone,
                    LastName = contact.LastName,
                    FirstName = contact.FirstName,
                    IsCustomer = contact.IsCustomer,
                    IsActive = contact.IsActive,
                };

                serviceBusMessage = serviceBusMessageFactory.CreateMessage(new RegistryContactUpdatedEvent(contactUpdatedEvent));
                messagesToSendInBatch.Add(serviceBusMessage);
                break;
        }
    }

    private async Task ProcessAccountPublishAsync()
    {
        logger.LogInformation("Send Account event data executed at: {Date} - ProcessAccountPublishAsync", DateTime.UtcNow);

        using var applicationContext = await dbContextFactory.CreateDbContextAsync();

        var query1 = from operation in applicationContext.RegOperationEntity
                     join account in applicationContext.RegAccountEntity
                     on operation.EntityId equals account.Id
                     where operation.Type == OperationType.Account
                     && operation.PublishedAt == null
                     && operation.ApprovalStatus == ApprovalStatus.Approved
                     select new { Operation = operation, Account = account };
        var nbOperation = 0;

        do
        {
            var operationBatch = await query1
                .Take(options.Value.ProcessEventPublishBatchSize)
                .ToListAsync();
            nbOperation = operationBatch.Count;

            if (nbOperation == 0)
            {
                break;
            }

            foreach (var row in operationBatch)
            {
                ProcessAccountOperation(row.Operation, row.Account);
            }

            await SendBatchMessageAsync();
            var operations = operationBatch.Select(o => UpdateOperationsToPublisAt(o.Operation)).ToList();
            await UpdateOperationsAsync(applicationContext);
        }
        while (nbOperation != 0);

        logger.LogInformation("Send Account event data succeed at: {Date} - ProcessAccountPublishAsync", DateTime.UtcNow);
    }

    private void ProcessAccountOperation(RegOperationEntity operation, RegAccountEntity account)
    {
        ServiceBusMessage? serviceBusMessage = null;
        switch (operation.Operation)
        {
            case OperationName.Insert:
                var accountCreatedEvent = account.ToRegAccountCreatedEventData();
                serviceBusMessage = serviceBusMessageFactory.CreateMessage(new RegistryAccountCreatedEvent(accountCreatedEvent));
                messagesToSendInBatch.Add(serviceBusMessage);
                break;

            case OperationName.Delete:
                var accountRemovedEvent = new RegistryAccountRemovedEventData()
                {
                    AccountGlobalUniqueIdentifier = account.Id,
                    AccountNumber = account.AccountNumber!,
                };

                serviceBusMessage = serviceBusMessageFactory.CreateMessage(new RegistryAccountRemovedEvent(accountRemovedEvent));
                messagesToSendInBatch.Add(serviceBusMessage);
                break;

            case OperationName.Update:
                var accountUpdatedEvent = account.ToRegAccountUpdatedEventData();
                serviceBusMessage = serviceBusMessageFactory.CreateMessage(new RegistryAccountUpdatedEvent(accountUpdatedEvent));
                messagesToSendInBatch.Add(serviceBusMessage);
                break;
        }
    }

    private async Task ProcessRolePublishAsync()
    {
        logger.LogInformation("Send Role event data executed at: {Date} - ProcessRolePublishAsync", DateTime.UtcNow);

        using var applicationContext = await dbContextFactory.CreateDbContextAsync();

        var query1 = from operation in applicationContext.RegOperationEntity
                     join role in applicationContext.RegRoleEntity.Include(r => r.ContactEmailNavigation).Include(r => r.AccountNumberNavigation)
                     on operation.EntityId equals role.RoleId
                     where operation.Type == OperationType.Role
                     && operation.PublishedAt == null
                     && operation.ApprovalStatus== ApprovalStatus.Approved
                     select new
                     {
                         Operation = operation,
                         Role = role,
                         RoleCount = applicationContext.RegRoleEntity
                         .Where(c => c.ContactEmail == role.ContactEmail && c.AccountNumber == role.AccountNumber && c.Deleted == null)
                         .Count(),
                     };

        var nbOperation = 0;
        do
        {
            var operationBatch = await query1
            .Take(options.Value.ProcessEventPublishBatchSize)
            .ToListAsync();
            nbOperation = operationBatch.Count;

            if (nbOperation == 0)
            {
                break;
            }

            foreach (var row in operationBatch)
            {
                ProcessRoleOperationAsync(row.Operation, row.Role, row.RoleCount);
            }

            await SendBatchMessageAsync();
            var operations = operationBatch.Select(o => UpdateOperationsToPublisAt(o.Operation)).ToList();
            await UpdateOperationsAsync(applicationContext);
        }
        while (nbOperation != 0);

        logger.LogInformation("Send Role event data executed at: {Date} - ProcessRolePublishAsync", DateTime.UtcNow);
    }

    private void ProcessRoleOperationAsync(RegOperationEntity operation, RegRoleEntity role, int roleCount)
    {
        ServiceBusMessage? serviceBusMessage = null;
        switch (operation.Operation)
        {
            case OperationName.Insert:
                    var createRoleEvent = new RegistryRoleCreatedEventData()
                    {
                        AccountId = role.AccountId,
                        Email = role.ContactEmail,
                        AccountNumber = role.AccountNumber!,
                        ContactId = role.ContactId,
                        RoleDelegataireEmail = role.RoleDelegataireEmail,
                        RoleSignatory = role.RoleSignatory,
                        IsFavorite = role.IsFavorite,
                    };

                    serviceBusMessage = serviceBusMessageFactory.CreateMessage(new RegistryRoleCreatedEvent(createRoleEvent));
                    messagesToSendInBatch.Add(serviceBusMessage);
                break;

            case OperationName.Delete:
                    var deleteRoleEvent = new RegistryRoleRemovedEventData()
                    {
                        AccountId = role.AccountId,
                        Email = role.ContactEmail,
                        AccountNumber = role.AccountNumber!,
                        ContactId = role.ContactId,
                    };

                    serviceBusMessage = serviceBusMessageFactory.CreateMessage(new RegistryRoleRemovedEvent(deleteRoleEvent));
                    messagesToSendInBatch.Add(serviceBusMessage);
                break;
        }
    }

    private async Task SendBatchMessageAsync()
    {
        if (messagesToSendInBatch.Count == 0)
        {
            return;
        }

        await notificationManager.BulkPublishAsync(messagesToSendInBatch);
        logger.LogInformation("ProcessRegEventPublish : SendBatchMessageAsync publish '{Count}' events success.", messagesToSendInBatch.Count);
        messagesToSendInBatch.Clear();
    }

    private RegOperationEntity UpdateOperationsToPublisAt(RegOperationEntity operation)
    {
        operation.PublishedAt = DateTime.UtcNow;
        return operation;
    }

    private async Task UpdateOperationsAsync(RefContext dbContext)
    {
        await dbContext.SaveChangesAsync();
    }
}

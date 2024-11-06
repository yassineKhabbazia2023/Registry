// <copyright file="ProcessRegEventPublish.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using System.Data;
using System.Text;
using Application.Interfaces;
using Azure.Messaging.ServiceBus;
using ContactRegistry.AzureFuctions.Const;
using ContactRegistry.AzureFuctions.Managers;
using ContactRegistry.AzureFuctions.Message;
using ContactRegistry.AzureFuctions.Options;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Pulse.Back.Events.Abstractions;
using Pulse.Back.Events.IntegrationEvents;
using Pulse.Back.Events.IntegrationEvents.EventsData;
using Pulse.ContactRegistry.Infrastructure.Context;
using Pulse.ContactRegistry.Infrastructure.Entities;

namespace ContactRegistry.AzureFuctions.Functions.Registry;

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
    private readonly IRegProcessDeltaTriggerRepository processDeltaTriggerRepository;

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
        IOptions<ProcessEventPublishOptions> options,
        IRegProcessDeltaTriggerRepository processDeltaTriggerRepository)
    {
        this.logger = logger;
        this.dbContextFactory = contextFactory;
        this.notificationManager = notificationManager;
        this.serviceBusMessageFactory = serviceBusMessageFactory;
        this.options = options;
        this.processDeltaTriggerRepository = processDeltaTriggerRepository;
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

        this.logger.LogInformation("Begin ProcessRegEventPublish Message ID: {Id}", message.MessageId);
        this.logger.LogDebug("Message Body: {Body}", message.Body);
        this.logger.LogDebug("Message Content-Type: {ContentType}", message.ContentType);

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

        this.logger.LogInformation("Function completed for {Type} - ProcessRegEventPublish", registryEntityType.EntityType);

        // Complete the message
        await messageActions.CompleteMessageAsync(message);
    }

    private async Task ProcessContactPublishAsync()
    {
        this.logger.LogInformation("Send Contact event data executed at: {Date} - ProcessContactPublishAsync", DateTime.UtcNow);

        var canProcess = await this.processDeltaTriggerRepository.GetProcessAsync();

        if (!canProcess.Contact)
        {
            this.logger.LogInformation("Contact ProcessDeltaTrigger is false - Stopped at: {Date}", DateTime.UtcNow);
            return;
        }

        using var applicationContext = await this.dbContextFactory.CreateDbContextAsync();

        var query1 = from operation in applicationContext.RegOperationEntity
                     join contact in applicationContext.RegContactEntity
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

            await this.processDeltaTriggerRepository.UpdateContactProcessAsync(false);
        }
        while (nbOperation != 0);

        this.logger.LogInformation("Send Contact event data succeed at: {Date} - ProcessContactPublishAsync", DateTime.UtcNow);
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
        this.logger.LogInformation("Send Account event data executed at: {Date} - ProcessAccountPublishAsync", DateTime.UtcNow);

        var canProcess = await this.processDeltaTriggerRepository.GetProcessAsync();

        if (!canProcess.Account)
        {
            this.logger.LogInformation("Account ProcessDeltaTrigger is false - Stopped at: {Date}", DateTime.UtcNow);
            return;
        }

        using var applicationContext = await this.dbContextFactory.CreateDbContextAsync();

        var query1 = from operation in applicationContext.RegOperationEntity
                     join account in applicationContext.RegAccountEntity
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

            await this.processDeltaTriggerRepository.UpdateAccountProcessAsync(false);
        }
        while (nbOperation != 0);

        this.logger.LogInformation("Send Account event data succeed at: {Date} - ProcessAccountPublishAsync", DateTime.UtcNow);
    }

    private void ProcessAccountOperation(RegOperationEntity operation, RegAccountEntity account)
    {
        ServiceBusMessage? serviceBusMessage = null;
        switch (operation.Operation)
        {
            case OperationName.Insert:
                var accountCreatedEvent = account.ToRegAccountCreatedEventData();
                serviceBusMessage = this.serviceBusMessageFactory.CreateMessage(new RegistryAccountCreatedEvent(accountCreatedEvent));
                this.messagesToSendInBatch.Add(serviceBusMessage);
                break;

            case OperationName.Delete:
                var accountRemovedEvent = new RegistryAccountRemovedEventData()
                {
                    AccountGlobalUniqueIdentifier = account.Id,
                    AccountNumber = account.AccountNumber!,
                };

                serviceBusMessage = this.serviceBusMessageFactory.CreateMessage(new RegistryAccountRemovedEvent(accountRemovedEvent));
                this.messagesToSendInBatch.Add(serviceBusMessage);
                break;

            case OperationName.Update:
                var accountUpdatedEvent = account.ToRegAccountUpdatedEventData();
                serviceBusMessage = this.serviceBusMessageFactory.CreateMessage(new RegistryAccountUpdatedEvent(accountUpdatedEvent));
                this.messagesToSendInBatch.Add(serviceBusMessage);
                break;
        }
    }

    private async Task ProcessRolePublishAsync()
    {
        this.logger.LogInformation("Send Role event data executed at: {Date} - ProcessRolePublishAsync", DateTime.UtcNow);

        var canProcess = await this.processDeltaTriggerRepository.GetProcessAsync();

        if (!canProcess.Role)
        {
            this.logger.LogInformation("Role ProcessDeltaTrigger is false - Stopped at: {Date}", DateTime.UtcNow);
            return;
        }

        using var applicationContext = await this.dbContextFactory.CreateDbContextAsync();

        var query1 = from operation in applicationContext.RegOperationEntity
                     join role in applicationContext.RegRoleEntity.Include(r => r.ContactEmailNavigation).Include(r => r.AccountNumberNavigation)
                     on operation.EntityId equals role.RoleId
                     where operation.Type == OperationType.Role
                     && operation.PublishedAt == null
                     && operation.Status == OperationStatus.Approved
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
            .Take(this.options.Value.ProcessEventPublishBatchSize)
            .ToListAsync();
            nbOperation = operationBatch.Count;

            if (nbOperation == 0)
            {
                break;
            }

            foreach (var row in operationBatch)
            {
                this.ProcessRoleOperationAsync(row.Operation, row.Role, row.RoleCount);
            }

            await this.SendBatchMessageAsync();
            var operations = operationBatch.Select(o => this.UpdateOperationsToPublisAt(o.Operation)).ToList();
            await this.UpdateOperationsAsync(applicationContext);

            await this.processDeltaTriggerRepository.UpdateRoleProcessAsync(false);
        }
        while (nbOperation != 0);

        this.logger.LogInformation("Send Role event data executed at: {Date} - ProcessRolePublishAsync", DateTime.UtcNow);
    }

    private void ProcessRoleOperationAsync(RegOperationEntity operation, RegRoleEntity role, int roleCount)
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
                        Email = role.ContactEmail,
                        AccountNumber = role.AccountNumber!,
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
                        Email = role.ContactEmail,
                        AccountNumber = role.AccountNumber!,
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
        this.logger.LogInformation("ProcessRegEventPublish : SendBatchMessageAsync publish '{Count}' events success.", this.messagesToSendInBatch.Count);
        this.messagesToSendInBatch.Clear();
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

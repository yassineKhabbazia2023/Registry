using Application.Consts;
using Application.Interfaces;
using Application.Models;
using Application.Options;
using Azure.Messaging.ServiceBus;
using Domain.Entities.Accounts;
using Domain.Entities.Contacts;
using Infrastructure.Helper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pulse.Back.Events.Abstractions;
using Pulse.Back.Events.IntegrationEvents;
using Pulse.Back.Events.IntegrationEvents.EventsData;
using Pulse.Registry.Domain.Context;
using Pulse.Registry.Domain.Entities;
using Registry.Application.Consts;
using Registry.Infrastructure.Managers;
using Domain.Entities.Accounts;
using Domain.Entities.Contacts;
using Infrastructure.Helper;
using EFCore.BulkExtensions;
using Domain.Constants;

namespace Infrastructure.Orchestrators
{
    public class RoleOrchestrator : IRoleOrchestrator
    {
        private readonly ILogger<RoleOrchestrator> logger;
        private readonly RefContext refContext;
        private readonly INotificationManager notificationManager;
        private readonly IOptions<BackGroundJobOptions> options;
        private readonly IServiceBusMessageFactory serviceBusMessageFactory;
        private readonly IOperationService operationService;
        private readonly List<ServiceBusMessage> messagesToSendInBatch = new List<ServiceBusMessage>();

        public RoleOrchestrator(
            ILogger<RoleOrchestrator> logger,
            RefContext refContext,
            INotificationManager notificationManager,
            IOptions<BackGroundJobOptions> options,
            IServiceBusMessageFactory serviceBusMessageFactory,
            IOperationService operationService)
        {
            this.logger = logger;
            this.refContext = refContext;
            this.notificationManager = notificationManager;
            this.options = options;
            this.serviceBusMessageFactory = serviceBusMessageFactory;
            this.operationService = operationService;
        }

        public async Task ProcessRolePublishAsync(string operationType)
        {
            await ProcessRolesOperationsAsync(operationType);
        }

        private async Task ProcessRolesOperationsAsync(string operationName)
        {
            logger.LogInformation("Send Role event data started at: {Date} - ProcessRolesOperationsAsync", DateTime.UtcNow);

            var nbOperation = 0;
            do
            {
                var operationBatch = await GeRolesOperationDetailsAsync(operationName, this.options.Value.Chunk);

                nbOperation = operationBatch.Count;

                if (nbOperation == 0)
                {
                    break;
                }

                foreach (var row in operationBatch)
                {
                    CreateRegistryRoleEvent(row.Operation, row.Role, row.RoleCount);
                }

                await OrchestratorHelper.SendBatchMessageAsync<RoleOrchestrator>(messagesToSendInBatch, notificationManager, logger);

                var operations = operationBatch.Select(o => OrchestratorHelper.UpdateOperationsToPublisAt(o.Operation)).ToList();

                await this.operationService.UpdateOperationStatusListASync(ProcessStatus.Sent, operations);
            }
            while (nbOperation != 0);

            await this.operationService.TryToProceedUntilTimeoutAsync(OperationTypeConsts.ROLE, operationName);

            logger.LogInformation("Send Role event data finished at: {Date} - ProcessRolesOperationsAsync", DateTime.UtcNow);
        }

        private async Task<List<RoleOperationDetail>> GeRolesOperationDetailsAsync(string operationName, int chuckSize)
        {
            var query = from operation in this.refContext.RegOperationEntity
                        join role in this.refContext.RefRoleEntity
                        on operation.EntityId equals role.EntityId
                        where operation.Type == OperationTypeConsts.ROLE
                        && operation.Operation.Equals(operationName)
                        && operation.PublishedAt == null
                        && operation.ApprovalStatus == ApprovalStatus.Approved
                        select new RoleOperationDetail()
                        {
                            Operation = operation,
                            Role = role,
                        };

            var operationBatch = await query
                .Take(chuckSize)
                .ToListAsync();

            return operationBatch;
        }

        private async void CreateRegistryRoleEvent(RegOperationEntity operation, RefRoleEntity role, int roleCount)
        {
            ServiceBusMessage? serviceBusMessage = null;


            AccountEntity accountEntity = this.refContext.AccountEntities.FirstOrDefault(x => x.AccountNumber == role.AccountNumber);
            ContactEntity contactEntity = this.refContext.ContactEntities.FirstOrDefault(x => x.Email == role.ContactEmail);

            if (contactEntity != null && accountEntity != null)
            {
                switch (operation.Operation)
                {
                    case OperationName.Insert:
                        var createRoleEvent = new RegistryRoleCreatedEventData()
                        {
                            AccountId = accountEntity.AccountGlobalUniqueId!.Value,
                            Email = role.ContactEmail,
                            AccountNumber = role.AccountNumber,
                            ContactId = contactEntity.ContactGlobalUniqueId!.Value,
                        };

                        serviceBusMessage = serviceBusMessageFactory.CreateMessage(new RegistryRoleCreatedEvent(createRoleEvent));
                        messagesToSendInBatch.Add(serviceBusMessage);
                        break;

                    case OperationName.Delete:
                        var deleteRoleEvent = new RegistryRoleRemovedEventData()
                        {
                            AccountId = accountEntity.AccountGlobalUniqueId!.Value,
                            Email = role.ContactEmail,
                            AccountNumber = role.AccountNumber,
                            ContactId = contactEntity.ContactGlobalUniqueId!.Value,
                        };

                        serviceBusMessage = serviceBusMessageFactory.CreateMessage(new RegistryRoleRemovedEvent(deleteRoleEvent));
                        messagesToSendInBatch.Add(serviceBusMessage);
                        break;
                }
            }

        }
    }
}

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
using Application.Models.Accounts;

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

            // Handle role operations that are cerated upon a delete account or contact operation 
            await HandleRolesOperationProcessingAsync(operationName, true);

            // Handle role operation that came from Akuiteo
            await HandleRolesOperationProcessingAsync(operationName);

            await this.operationService.TryToProceedUntilTimeoutAsync(OperationTypeConsts.ROLE, operationName);

            logger.LogInformation("Send Role event data finished at: {Date} - ProcessRolesOperationsAsync", DateTime.UtcNow);
        }

        private async Task HandleRolesOperationProcessingAsync(string operationName, bool? processSystemCreatedOperations = false)
        {
            var nbOperation = 0;
            do
            {
                var operationBatch = await GeRolesOperationDetailsAsync(operationName, this.options.Value.Chunk, processSystemCreatedOperations!.Value);

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
        }
        /// <summary>
        /// A method to get roles approved operations that are not processed.
        /// </summary>
        /// <param name="operationName"></param>
        /// <param name="chuckSize"></param>
        /// <param name="fetchSystemCreatedOperations">A flag to get the operations that are created by the system (example: upon delete contact or account).</param>
        /// <returns></returns>
        private async Task<List<RoleOperationDetail>> GeRolesOperationDetailsAsync(string operationName, int chuckSize, bool? fetchSystemCreatedOperations = false)
        {
            IQueryable<RoleOperationDetail> query = default;
            if (fetchSystemCreatedOperations.Value)
            {
                var filteredOperations = this.refContext.RegOperationEntity
                    .Where(o => o.Type == OperationTypeConsts.ROLE &&
                                o.Operation.Equals(operationName) &&
                                o.PublishedAt == null &&
                                o.ApprovalStatus == ApprovalStatus.Approved &&
                                o.CreatedBySystem == true);

                query = from op in filteredOperations
                        join account in this.refContext.AccountEntities
                            on op.EntityId equals account.AccountGlobalUniqueId
                        join role in this.refContext.RoleEntities
                            on account.AccountId equals role.AccountId
                        group new { op, account, role }
                              by new { account.AccountNumber, role.ContactEmail } into g
                        select new RoleOperationDetail
                        {
                            Operation = g.First().op,
                            Role = new RefRoleEntity
                            {
                                AccountNumber = g.Key.AccountNumber,
                                ContactEmail = g.Key.ContactEmail
                            }
                        };
            }
            else
            {
                query = from operation in this.refContext.RegOperationEntity
                        join role in this.refContext.RefRoleEntity
                        on operation.EntityId equals role.EntityId
                        where operation.Type == OperationTypeConsts.ROLE
                        && operation.Operation.Equals(operationName)
                        && operation.PublishedAt == null
                        && operation.ApprovalStatus == ApprovalStatus.Approved
                        && (operation.CreatedBySystem == false || operation.CreatedBySystem == null)
                        select new RoleOperationDetail()
                        {
                            Operation = operation,
                            Role = role,
                        };
            }

            var operationBatch = await query
                .Take(chuckSize)
                .ToListAsync();

            return operationBatch;
        }

        private async void CreateRegistryRoleEvent(RegOperationEntity operation, RefRoleEntity role, int roleCount)
        {
            var accountEntity = this.refContext.AccountEntities.FirstOrDefault(x => x.AccountNumber == role.AccountNumber);
            var contactEntity = this.refContext.ContactEntities.FirstOrDefault(x => x.Email == role.ContactEmail);

            if (contactEntity != null && accountEntity != null)
            {
                ServiceBusMessage? serviceBusMessage = default;
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

using Application.Consts;
using Application.Interfaces;
using Application.Options;
using Azure.Messaging.ServiceBus;
using Infrastructure.Helper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pulse.Back.Events.Abstractions;
using Pulse.Back.Events.IntegrationEvents;
using Pulse.Back.Events.IntegrationEvents.EventsData;
using Pulse.Registry.Domain.Context;
using Pulse.Registry.Domain.Entities;
using Registry.Infrastructure.Managers;
using Pulse.Registry.Domain.Constants;
using Application.Enums;

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

        public async Task ProcessRolePublishAsync(string operationType, bool? processPennylaneDeletedRoles = false)
        {
            await ProcessRolesOperationsAsync(operationType, processPennylaneDeletedRoles);
        }

        private async Task ProcessRolesOperationsAsync(string operationName, bool? processPennylaneDeletedRoles = false)
        {
            logger.LogInformation("Send Role event data started at: {Date} - ProcessRolesOperationsAsync", DateTime.UtcNow);
            if (processPennylaneDeletedRoles.HasValue && processPennylaneDeletedRoles.Value)
            {
                // Handle only role deletion operations that are created by Pennylane
                await HandleRolesOperationProcessingAsync(operationName, processSystemCreatedOperations: false, processPennylaneDeletedRoles: true);
                await this.operationService.TryToProceedUntilTimeoutAsync(OperationCategory.ROLE, operationName);

                return;
            }
            // Handle role operations that are cerated upon a delete account or contact operation 
            await HandleRolesOperationProcessingAsync(operationName, true);

            // Handle role operation that came from Akuiteo
            await HandleRolesOperationProcessingAsync(operationName);

            await this.operationService.TryToProceedUntilTimeoutAsync(OperationCategory.ROLE, operationName);

            logger.LogInformation("Send Role event data finished at: {Date} - ProcessRolesOperationsAsync", DateTime.UtcNow);
        }

        private async Task HandleRolesOperationProcessingAsync(string operationName, bool? processSystemCreatedOperations = false, bool? processPennylaneDeletedRoles = false)
        {
            var nbOperation = 0;
            do
            {
                var operationBatch = await this.operationService.GetRoleOperationRecordsAsync(operationName, this.options.Value.Chunk, processSystemCreatedOperations!.Value);

                if (processPennylaneDeletedRoles.HasValue && processPennylaneDeletedRoles.Value)
                {
                    operationBatch = operationBatch.Where(o => !string.IsNullOrWhiteSpace(o.Role.RoleSource) && o.Role.RoleSource.Equals(DataSources.PENNYLANE.ToString(), StringComparison.OrdinalIgnoreCase)).ToList();
                }

                nbOperation = operationBatch is not null ? operationBatch.Count : 0;

                if (nbOperation == 0)
                {
                    break;
                }

                foreach (var row in operationBatch)
                {
                    CreateRegistryRoleEvent(row.Operation, row.Role, row.RoleCount);
                }

                await OrchestratorHelper.SendBatchMessageAsync<RoleOrchestrator>(messagesToSendInBatch, notificationManager, logger);

                logger.LogInformation("Send Role event data compelted at: {Date}  - ProcessRolesOperationsAsync", DateTime.UtcNow);

                var operations = operationBatch.Select(o => OrchestratorHelper.UpdateOperationsToPublisAt(o.Operation)).ToList();

                await this.operationService.UpdateOperationStatusListASync(ProcessStatus.Sent, operations);
            }
            while (nbOperation != 0);
        }

        private void CreateRegistryRoleEvent(RegOperationEntity operation, RefRoleEntity role, int roleCount)
        {
            var accountEntity = this.refContext.AccountEntities.FirstOrDefault(x => x.AccountNumber == role.AccountNumber);
            var contactEntity = this.refContext.ContactEntities.FirstOrDefault(x => x.Email == role.ContactEmail);

            if (contactEntity != null && accountEntity != null)
            {
                ServiceBusMessage? serviceBusMessage = default;
                switch (operation.Operation)
                {
                    case OperationAction.Insert:
                        var createRoleEvent = new RegistryRoleCreatedEventData()
                        {
                            AccountGuid = accountEntity.AccountGlobalUniqueId,
                            ContactGuid = contactEntity.ContactGlobalUniqueId,
                            AccountId = accountEntity.AccountId,
                            Email = role.ContactEmail,
                            AccountNumber = role.AccountNumber,
                            ContactId = contactEntity.ContactId,
                            IsCustomerRelation = CheckIsCustomerRelation(role.Description)
                        };

                        serviceBusMessage = serviceBusMessageFactory.CreateMessage(new RegistryRoleCreatedEvent(createRoleEvent));
                        messagesToSendInBatch.Add(serviceBusMessage);
                        break;

                    case OperationAction.Delete:
                        var deleteRoleEvent = new RegistryRoleRemovedEventData()
                        {
                            AccountId = accountEntity.AccountId,
                            Email = role.ContactEmail,
                            AccountNumber = role.AccountNumber,
                            ContactId = contactEntity.ContactId,
                        };


                        serviceBusMessage = serviceBusMessageFactory.CreateMessage(new RegistryRoleRemovedEvent(deleteRoleEvent));
                        messagesToSendInBatch.Add(serviceBusMessage);
                        break;
                }
            }
        }

        private bool CheckIsCustomerRelation(string description)
        {
            return description.Equals(GlobalConstants.CLP, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}

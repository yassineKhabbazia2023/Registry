using Application.Consts;
using Application.Enums;
using Application.Interfaces;
using Azure.Messaging.ServiceBus;
using Infrastructure.Exceptions;
using Infrastructure.Helper;
using Microsoft.Extensions.Logging;
using Pulse.Back.Events.Abstractions;
using Pulse.Back.Events.IntegrationEvents;
using Pulse.Back.Events.IntegrationEvents.EventsData;
using Pulse.Registry.Domain.Context;
using Pulse.Registry.Domain.Entities;
using Registry.Infrastructure.Managers;

namespace Infrastructure.Orchestrators
{
    public class ContactOrchestrator(
        ILogger<ContactOrchestrator> logger,
        RefContext refContext,
        INotificationManager notificationManager,
        IServiceBusMessageFactory serviceBusMessageFactory,
        IOperationService operationService) : IContactOrchestrator
    {
        private readonly ILogger<ContactOrchestrator> logger = logger;
        private readonly RefContext refContext = refContext;
        private readonly INotificationManager notificationManager = notificationManager;
        private readonly IServiceBusMessageFactory serviceBusMessageFactory = serviceBusMessageFactory;
        private readonly IOperationService operationService = operationService;

        public async Task ProcessContactPublishAsync(string operationType)
        {
            try
            {
                logger.LogInformation("Start {OperationType} process contact publish at: {Date} - ProcessContactPublishAsync", operationType, DateTime.UtcNow);

                var operationContactList = operationService.GeContactOperationRecords(operationType);
                if(operationContactList is not null && operationContactList.Any())
                {
                    var operationsContacts = operationContactList
                        .Where(item => item != null && item.Operation != null && item.RefContactEntity != null)
                        .ToList();
                    if (operationsContacts.Count > 0)
                    {
                        var messages = new List<ServiceBusMessage>();
                        foreach (var contactOperation in operationsContacts)
                        {
                            var message = ProcessContactOperation(contactOperation.Operation, contactOperation.RefContactEntity);
                            if (message != null)
                            {
                                messages.Add(message);
                            }
                        }

                        await this.notificationManager.BulkPublishAsync(messages);
                        var operations = operationsContacts.Select(o => OrchestratorHelper.UpdateOperationsToPublisAt(o.Operation)).ToList();
                        await this.operationService.UpdateOperationStatusListASync(ProcessStatus.Sent, operations);
                        await this.operationService.TryToProceedUntilTimeoutAsync(OperationCategory.CONTACT, operationType);

                        logger.LogInformation("Finished {OperationType} process contact publish at: {Date} - ProcessContactPublishAsync", operationType, DateTime.UtcNow);
                    }
                    else 
                    { 
                        logger.LogWarning("The {OperationsContacts} is empty after filtering at: {Date} - ProcessContactPublishAsync", nameof(operationsContacts), DateTime.UtcNow);
                    }
                }
                else
                {
                    logger.LogWarning("When {OperationType} the {OperationContactList} is null or empty at: {Date} - ProcessContactPublishAsync", operationType, nameof(operationContactList), DateTime.UtcNow);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send Contact event data - ProcessContactPublishAsync");
                throw new ContactPublishException("Failed to process contact publish operation", ex);
            }
        }

        public ServiceBusMessage? ProcessContactOperation(RegOperationEntity operation, RefContactEntity contact)
        {
            try
            {
                return operation.Operation switch
                {
                    OperationAction.Insert => CreateInsertMessage(contact),
                    OperationAction.Delete => CreateDeleteMessage(operation, contact),
                    OperationAction.Update => CreateUpdateMessage(operation, contact),
                    _ => throw new ProcessContactOperationException($"Failed to process contact operation {operation.Id} because cannot found the operation type")
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process operation {Id} - ProcessContactPublishAsync", operation.Id);
                return null;
            }
        }

        private ServiceBusMessage CreateInsertMessage(RefContactEntity contact)
        {
            var contactCreatedEvent = new RegistryContactCreatedEventData
            {
                Id = contact.EntityId,
                IsCustomer = contact.IsCustomer ?? false,
                FirstName = contact.FirstName,
                LastName = contact.LastName,
                Email = contact.Email,
                OfficeCode = contact.OfficeCode,
                LandPhone = contact.LandPhone,
                MobilePhone = contact.MobilePhone,
                JobDescription = contact.JobDescription,
                Source = !string.IsNullOrWhiteSpace(contact.ContactSource)
                    ? contact.ContactSource
                    : DataSources.AKUITEO.ToString(),
            };

            if (ShouldIncludeAccountNumber(contact))
            {
                contactCreatedEvent.AccountNumber = contact.AccountNumber!;
            }

            return serviceBusMessageFactory.CreateMessage(new RegistryContactCreatedEvent(contactCreatedEvent));
        }

        private ServiceBusMessage CreateDeleteMessage(RegOperationEntity operation, RefContactEntity contact)
        {
            var contactEntity = refContext.ContactEntities.FirstOrDefault(x => x.Email == contact.Email);

            if (contactEntity == null || contactEntity.ContactGlobalUniqueId == null)
            {
                throw new ProcessContactOperationException($"Failed to process contact operation {operation.Id}");
            }

            var contactRemovedEvent = new RegistryContactRemovedEventData
            {
                Id = contactEntity.ContactGlobalUniqueId.Value,
                Email = contact.Email,
            };

            return serviceBusMessageFactory.CreateMessage(new RegistryContactRemovedEvent(contactRemovedEvent));
        }

        private ServiceBusMessage CreateUpdateMessage(RegOperationEntity operation, RefContactEntity contact)
        {
            var emailToSearch = operation.OldContactEmail ?? contact.Email;
            var contactEntity = refContext.ContactEntities.FirstOrDefault(x => x.Email == emailToSearch);

            if (contactEntity == null || !contactEntity.ContactGlobalUniqueId.HasValue)
            {
                throw new ProcessContactOperationException($"Failed to process contact operation {operation.Id} because cannot found contact");
            }

            var contactUpdatedEvent = new RegistryContactUpdatedEventData
            {
                Id = contactEntity.ContactGlobalUniqueId.Value,
                Email = contact.Email,
                OfficeCode = contact.OfficeCode,
                JobDescription = contact.JobDescription,
                LandPhone = contact.LandPhone,
                MobilePhone = contact.MobilePhone,
                LastName = contact.LastName,
                FirstName = contact.FirstName,
                IsCustomer = contact.IsCustomer ?? false,
                IsActive = true,
            };

            return serviceBusMessageFactory.CreateMessage(new RegistryContactUpdatedEvent(contactUpdatedEvent));
        }

        private static bool ShouldIncludeAccountNumber(RefContactEntity contact) =>
            contact.ContactSource is not null &&
            contact.ContactSource.Equals(DataSources.PENNYLANE.ToString(), StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrEmpty(contact.AccountNumber);
    }
}

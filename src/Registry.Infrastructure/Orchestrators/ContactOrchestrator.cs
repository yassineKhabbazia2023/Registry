using Application.Consts;
using Application.Enums;
using Application.Interfaces;
using Azure.Messaging.ServiceBus;
using Domain.Constants;
using Domain.Entities.Contacts;
using Infrastructure.Helper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pulse.Back.Events.Abstractions;
using Pulse.Back.Events.IntegrationEvents;
using Pulse.Back.Events.IntegrationEvents.EventsData;
using Pulse.Registry.Domain.Context;
using Pulse.Registry.Domain.Entities;
using Registry.Application.Consts;
using Registry.Infrastructure.Managers;

namespace Infrastructure.Orchestrators
{
    public class ContactOrchestrator : IContactOrchestrator
    {
        private readonly ILogger<ContactOrchestrator> logger;
        private readonly RefContext refContext;
        private readonly INotificationManager notificationManager;
        private readonly IServiceBusMessageFactory serviceBusMessageFactory;
        private readonly IOperationService operationService;

        public ContactOrchestrator(
        ILogger<ContactOrchestrator> logger,
        RefContext refContext,
        INotificationManager notificationManager,
        IServiceBusMessageFactory serviceBusMessageFactory,
        IOperationService operationService)
        {
            this.logger = logger;
            this.refContext = refContext;
            this.notificationManager = notificationManager;
            this.serviceBusMessageFactory = serviceBusMessageFactory;
            this.operationService = operationService;
        }

        public async Task ProcessContactPublishAsync(string operationType)
        {
            logger.LogInformation("Send Contact event data started at: {Date} - ProcessContactPublishAsync", DateTime.UtcNow);

            var operationContactList = operationService.GeContactOperationRecords(operationType);
            if (operationContactList != null && operationContactList.Any())
            {
                string result = string.Join(", ", operationContactList.Select(o => o.Operation.Id).ToArray());
                logger.LogInformation("Contact event operation id data : {Data} - ProcessContactPublishAsync", result);

                var messages = operationContactList
                    .Select(op => ProcessContactOperation(op.Operation, op.RefContactEntity))
                    .Where(item => item != null)
                    .ToList();

                if (messages != null && messages.Count != 0)
                {
                    await this.notificationManager.BulkPublishAsync(messages);
                }

                var operations = operationContactList.Select(o => OrchestratorHelper.UpdateOperationsToPublisAt(o.Operation)).ToList();
                await this.operationService.UpdateOperationStatusListASync(ProcessStatus.Sent, operations);
            }
            await this.operationService.TryToProceedUntilTimeoutAsync(OperationCategory.CONTACT, operationType);
            logger.LogInformation("Send Contact event data finished at: {Date} - ProcessContactPublishAsync", DateTime.UtcNow);
        }

        public ServiceBusMessage? ProcessContactOperation(RegOperationEntity operation, RefContactEntity contact)
        {
            ServiceBusMessage? serviceBusMessage = null;
            ContactEntity? contactEntity = default;

            switch (operation.Operation)
            {
                case OperationAction.Insert:
                    var contactCreatedEvent = new RegistryContactCreatedEventData()
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
                        Source = !string.IsNullOrWhiteSpace(contact.ContactSource) ?
                        contact.ContactSource : DataSources.AKUITEO.ToString(),
                    };

                    // Include account number only for PennyLane contacts to enable onboarding process when handling the event
                    if (ShouldIncludeAccountNumber(contact))
                    {
                        contactCreatedEvent.AccountNumber = contact.AccountNumber!;
                    }

                    serviceBusMessage = serviceBusMessageFactory.CreateMessage(new RegistryContactCreatedEvent(contactCreatedEvent));
                    break;

                case OperationAction.Delete:
                    contactEntity = refContext.ContactEntities
                                .FirstOrDefault(x => x.Email == contact.Email);

                    if (contactEntity != null)
                    {
                        var contactRemovedEvent = new RegistryContactRemovedEventData()
                        {
                            Id = contactEntity.ContactGlobalUniqueId.Value,
                            Email = contact.Email,
                        };

                        serviceBusMessage = serviceBusMessageFactory.CreateMessage(new RegistryContactRemovedEvent(contactRemovedEvent));
                    }

                    break;

                case OperationAction.Update:
                    contactEntity = refContext.ContactEntities.FirstOrDefault(x => x.Email == (operation.OldContactEmail ?? contact.Email));
                    if (contactEntity != null && contactEntity.ContactGlobalUniqueId.HasValue)
                    {
                        var contactUpdatedEvent = new RegistryContactUpdatedEventData()
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

                        serviceBusMessage = serviceBusMessageFactory.CreateMessage(new RegistryContactUpdatedEvent(contactUpdatedEvent));
                    }
                    break;

            }

            return serviceBusMessage;
        }

        private bool ShouldIncludeAccountNumber(RefContactEntity contact)
        {
            return contact.ContactSource is not null &&
            contact.ContactSource.Equals(DataSources.PENNYLANE.ToString(), StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrEmpty(contact.AccountNumber);
        }
    }
}

using Application.Consts;
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
    public class ContactOrchetrator : IContactOrchestrator
    {
        private readonly ILogger<ContactOrchetrator> logger;
        private readonly RefContext refContext;
        private readonly INotificationManager notificationManager;
        private readonly IServiceBusMessageFactory serviceBusMessageFactory;
        private readonly IOperationService operationService;

        public ContactOrchetrator(
        ILogger<ContactOrchetrator> logger,
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

            var operationContactList = (from operation in refContext.RegOperationEntity
                                        join contact in refContext.RefContactEntity
                                        on operation.EntityId equals contact.EntityId
                                        where operation.Type == OperationTypeConsts.CONTACT
                                        && operation.PublishedAt == null
                                        && operation.ApprovalStatus == ApprovalStatus.Approved
                                        && operation.Operation == operationType
                                        orderby operation.CreationDate
                                        select new OperationWithContact { Operation = operation, RefContactEntity = contact })
                         .AsNoTracking()
                         .AsEnumerable();

            List<ServiceBusMessage?>? messages = operationContactList?
                .Select(op => ProcessContactOperation(op.Operation, op.RefContactEntity))?
                .Where(item => item != null)
                .ToList();

            if (messages != null && messages.Count != 0)
            {
                await this.notificationManager.BulkPublishAsync(messages);
            }

            var operations = operationContactList.Select(o => OrchestratorHelper.UpdateOperationsToPublisAt(o.Operation)).ToList();

            await this.operationService.UpdateOperationStatusListASync(ProcessStatus.Sent, operations);

            await this.operationService.TryToProceedUntilTimeoutAsync(OperationTypeConsts.CONTACT, operationType);


            logger.LogInformation("Send Contact event data finished at: {Date} - ProcessContactPublishAsync", DateTime.UtcNow);
        }

        public ServiceBusMessage? ProcessContactOperation(RegOperationEntity operation, RefContactEntity contact)
        {
            ServiceBusMessage? serviceBusMessage = null;
            ContactEntity? contactEntity = default;

            switch (operation.Operation)
            {
                case OperationName.Insert:
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
                        Source = "AKUITEO",
                    };

                    serviceBusMessage = serviceBusMessageFactory.CreateMessage(new RegistryContactCreatedEvent(contactCreatedEvent));
                    break;

                case OperationName.Delete:
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

                case OperationName.Update:
                    contactEntity = refContext.ContactEntities
                                .FirstOrDefault(x => x.Email == contact.Email);

                    if (contactEntity != null)
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
    }
    public class OperationWithContact
    {
        public required RegOperationEntity Operation { get; set; }

        public required RefContactEntity RefContactEntity { get; set; }
    }
}

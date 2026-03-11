// <copyright file="ContactsDeepValidationsService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Consts;
using Application.Enums;
using Application.Exceptions;
using Application.Interfaces;
using Application.Requests;
using Microsoft.Extensions.Logging;
using Pulse.Registry.Domain.Entities;
using Pulse.Registry.Domain.Entities.Audits;
using Registry.Application.Consts;

namespace Application.services;

public class ContactsDeepValidationsService(
    IContactRepository contactRepository, IOperationRepository operationRepository,
    ILogger<ContactsDeepValidationsService> logger,
    IRoleRepository roleRepository,
    IDeepValidationRepository deepValidationRepository
    ) : IContactsDeepValidationsService
{
    public async Task CreateValidContactsOperationsAsync()
    {
        const int pageSize = 1000;
        Guid? lastEntityId = null;
        IList<RefContactEntity>? contacts = new List<RefContactEntity>() { };

        do
        {
            contacts = await contactRepository.GetContactsWithoutOperationsPagedAsync(pageSize, lastEntityId);

            foreach (RefContactEntity contact in contacts)
            {
                contact.ValidationDate = DateTime.UtcNow;

                switch (contact.OperationType)
                {
                    case OperationAction.Insert:
                        await ValidateCreateInsertContactOperationAsync(contact);
                        break;
                    case OperationAction.Delete:
                        await ValidateCreateDeleteContactOperationAsync(contact);
                        break;
                    case OperationAction.Update:
                        await ValidateCreateUpdateContactOperationAsync(contact);
                        break;
                }
            }

            // Update lastEntityId to the last record processed.
            if (contacts.Any())
            {
                lastEntityId = contacts.Last().EntityId;
            }

        } while (contacts.Any());
    }

    private async Task ValidateCreateInsertContactOperationAsync(RefContactEntity refContactEntity)
    {
        bool isContactExists = await contactRepository
            .DoesContactGlobalUniqueIdExistByEmailAsync(refContactEntity.Email);

        var criteria = new OperationSearchCriteria
        {
            OperationName = OperationAction.Insert
        };

        var operations = await operationRepository.FetchOperationsByCriteriaAsync(
            criteria,
            OperationStrategyType.CONTACT,
            refContactEntity.Email);

        if (isContactExists || operations.Any())
        {
            logger.LogInformation("{RepositoryName} Transforming the insert contact operation to an update for the contact {Email}", 
                nameof(ContactsDeepValidationsService), 
                refContactEntity.Email);

            refContactEntity.OperationType = OperationAction.Update;
        }

        await CreateOperationAsync(refContactEntity);
    }

    private async Task ValidateCreateUpdateContactOperationAsync(RefContactEntity refContactEntity)
    {
        bool isContactExists = await contactRepository
            .DoesContactExistByEmailAsync(refContactEntity.Email);

        var criteria = new OperationSearchCriteria
        {
            OperationName = OperationAction.Insert,
            OperationProcessStatus = new string[] { ProcessStatus.Ready }
        };

        var insertContactReadyOperations = await operationRepository.FetchOperationsByCriteriaAsync(
            criteria,
            OperationStrategyType.CONTACT,
            refContactEntity.Email);


        if (!isContactExists && insertContactReadyOperations.Count().Equals(0))
        {
            string message = string.Format("{0} Transforming the update contact operation to an insert for the contact {1}",
            nameof(ContactsDeepValidationsService), refContactEntity.Email);
            logger.LogInformation(message);

            refContactEntity.OperationType = OperationAction.Insert;
            await CreateOperationAsync(refContactEntity);
            return;
        }

        if (!isContactExists && !insertContactReadyOperations.Equals(0))
        {
            if (insertContactReadyOperations.Count() > 1)
            {
                string message = string.Format("{0} found an unexpected behaviour contact {1} has multipe insert ready operations",
                nameof(ContactsDeepValidationsService), refContactEntity.Email);
                logger.LogWarning(message);

                await deepValidationRepository.AddDeepValidationAsync(new DeepValidationEntity
                {
                    Type = "CONTACT",
                    EntityId = refContactEntity.EntityId,
                    Reason = message,
                    CreationDate = DateTime.UtcNow,
                });

                return;
            }

            await CreateOperationAsync(refContactEntity);
            return;
        }

        await CreateOperationAsync(refContactEntity);
    }

    private async Task ValidateCreateDeleteContactOperationAsync(RefContactEntity refContactEntity)
    {
        bool isContactExists = await contactRepository
            .DoesContactExistByEmailAsync(refContactEntity.Email);

        var insertContactReadyOperationsCriteria = new OperationSearchCriteria
        {
            OperationName = OperationAction.Insert,
            OperationProcessStatus = new string[] { ProcessStatus.Ready }
        };

        var insertContactReadyOperations = await operationRepository.FetchOperationsByCriteriaAsync(
            insertContactReadyOperationsCriteria,
            OperationStrategyType.CONTACT,
            refContactEntity.Email);


        var operations = await operationRepository.FetchOperationsByCriteriaAsync(
            new OperationSearchCriteria
            {
                OperationName = OperationAction.Delete,
            },
            OperationStrategyType.CONTACT,
            refContactEntity.Email);

        bool isContactOperationExists = operations.Any();

        if (!isContactExists && insertContactReadyOperations.Count().Equals(0))
        {
            string message = string.Format("{0} skipping creating an delete contact operation for the contact {1}, contact does not exists",
                nameof(ContactsDeepValidationsService), refContactEntity.Email);

            logger.LogInformation(message);
            await deepValidationRepository.AddDeepValidationAsync(new DeepValidationEntity
            {
                Type = "CONTACT",
                EntityId = refContactEntity.EntityId,
                Reason = message,
                CreationDate = DateTime.UtcNow,
            });
            return;
        }

        if (!isContactExists && !insertContactReadyOperations.Equals(0))
        {
            if (insertContactReadyOperations.Count() > 1)
            {
                string message = string.Format("{0} found an unexpected behaviour contact {1} has multipe insert ready operations",
                nameof(ContactsDeepValidationsService), refContactEntity.Email);
                logger.LogWarning(message);


                await deepValidationRepository.AddDeepValidationAsync(new DeepValidationEntity
                {
                    Type = "CONTACT",
                    EntityId = refContactEntity.EntityId,
                    Reason = message,
                    CreationDate = DateTime.UtcNow,
                });

                return;
            }
            await CreateOperationAsync(refContactEntity);

            return;
        }

        if (isContactOperationExists)
        {
            string message = string.Format("{0} skipping creating an delete contact operation for the contact {1}, an operation {2} already exist",
                nameof(ContactsDeepValidationsService), refContactEntity.Email, refContactEntity.OperationType);

            logger.LogInformation(message);
            await deepValidationRepository.AddDeepValidationAsync(new DeepValidationEntity
            {
                Type = "CONTACT",
                EntityId = refContactEntity.EntityId,
                Reason = message,
                CreationDate = DateTime.UtcNow,
            });
            return;
        }

        await CreateOperationAsync(refContactEntity);
    }

    private async Task CreateOperationAsync(RefContactEntity refContactEntity)
    {
        try
        {
            await operationRepository.CreateOperationAsync(new RegOperationEntity
            {
                Operation = refContactEntity.OperationType,
                Type = OperationCategory.CONTACT,
                EntityId = refContactEntity.EntityId,
                ApprovalStatus = ApprovalStatus.Approved,
                CreationDate = DateTime.UtcNow,
                ProcessStatus = ProcessStatus.Ready
            });
        }
        catch (DbOperationException ex)
        {
            string message = string.Format("{0} Unable to add operation {1} for entity {2}, {3}",
               nameof(ContactsDeepValidationsService), refContactEntity.OperationType, refContactEntity.EntityId, ex.InnerException);

            logger.LogError(message);

            await deepValidationRepository.AddDeepValidationAsync(new DeepValidationEntity
            {
                Type = "CONTACT",
                EntityId = refContactEntity.EntityId,
                Reason = message,
                CreationDate = DateTime.UtcNow,
            });
        }
    }
}

using Application.Consts;
using Application.Exceptions;
using Application.Interfaces;
using Domain.Entities.Accounts;
using Microsoft.Extensions.Logging;
using Pulse.Registry.Domain.Entities;
using Registry.Application.Consts;

namespace Application.services
{
    public class ContactsDeepValidationsService(
        IContactRepository contactRepository, IOperationRepository operationRepository,
        ILogger<ContactsDeepValidationsService> logger,
        IRoleRepository roleRepository
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
                    switch (contact.OperationType)
                    {
                        case OperationName.Insert:
                            await ValidateCreateInsertContactOperationAsync(contact);
                            break;
                        case OperationName.Delete:
                            await ValidateCreateDeleteContactOperationAsync(contact);
                            break;
                        case OperationName.Update:
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
                .DoesContactExistAsync(refContactEntity.Email);
            bool isContactOperationExists = await contactRepository
                .DoesOperationContactExistAsync(refContactEntity.Email);
            if (isContactExists || isContactOperationExists)
            {
                string message = string.Format("{0} skipping creating an insert contact operation for the contact {1}, contact or operation already exists",
                    nameof(ContactsDeepValidationsService), refContactEntity.Email);

                await contactRepository.InsertContactNewAudit(refContactEntity, message);
                return;
            }

            await CreateOperationAsync(refContactEntity);
        }

        private async Task ValidateCreateUpdateContactOperationAsync(RefContactEntity refContactEntity)
        {
            bool isContactExists = await contactRepository
                .DoesContactExistAsync(refContactEntity.Email);

            var insertContactReadyOperations = await operationRepository.FindContactsReadyOperationsAsync(
                refContactEntity.Email,
                OperationName.Insert
                );

            if (!isContactExists && insertContactReadyOperations.Equals(0))
            {
                string message = string.Format("{0} skipping creating an update contact operation for the contact {1}, contact does not exists",
                nameof(ContactsDeepValidationsService), refContactEntity.Email);
                logger.LogInformation(message);

                await contactRepository.InsertContactNewAudit(refContactEntity, message);

                return;
            }

            if (!isContactExists && !insertContactReadyOperations.Equals(0))
            {
                if (insertContactReadyOperations > 1)
                {
                    string message = string.Format("{0} found an unexpected behaviour contact {1} has multipe insert ready operations",
                    nameof(ContactsDeepValidationsService), refContactEntity.Email);
                    logger.LogWarning(message);

                    await contactRepository.InsertContactNewAudit(refContactEntity, message);

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
                .DoesContactExistAsync(refContactEntity.Email);

            var insertContactReadyOperations = await operationRepository.FindContactsReadyOperationsAsync(
                refContactEntity.Email,
                OperationName.Insert
                );
            bool isContactOperationExists = await contactRepository
                .DoesOperationContactExistAsync(refContactEntity.Email);

            if (!isContactExists && insertContactReadyOperations.Equals(0))
            {
                string message = string.Format("{0} skipping creating an delete contact operation for the contact {1}, contact does not exists",
                    nameof(ContactsDeepValidationsService), refContactEntity.Email);

                logger.LogInformation(message);
                await contactRepository.InsertContactNewAudit(refContactEntity, message);
                return;
            }

            if (!isContactExists && !insertContactReadyOperations.Equals(0))
            {
                if (insertContactReadyOperations > 1)
                {
                    string message = string.Format("{0} found an unexpected behaviour contact {1} has multipe insert ready operations",
                    nameof(ContactsDeepValidationsService), refContactEntity.Email);
                    logger.LogWarning(message);

                    await contactRepository.InsertContactNewAudit(refContactEntity, message);

                    return;
                }
                await CreateOperationAsync(refContactEntity);
                await DeleteContactRelatedRolesAsync(refContactEntity.Email, refContactEntity.EntityId);

                return;
            }

            if (isContactOperationExists)
            {
                string message = string.Format("{0} skipping creating an delete contact operation for the contact {1}, an operation {2} already exist",
                    nameof(ContactsDeepValidationsService), refContactEntity.Email, refContactEntity.OperationType);

                logger.LogInformation(message);
                await contactRepository.InsertContactNewAudit(refContactEntity, message);
                return;
            }

            await CreateOperationAsync(refContactEntity);
            await DeleteContactRelatedRolesAsync(refContactEntity.Email, refContactEntity.EntityId);
        }

        private async Task CreateOperationAsync(RefContactEntity refContactEntity)
        {
            try
            {
                await operationRepository.InsertNewOperation(new RegOperationEntity
                {
                    Operation = refContactEntity.OperationType,
                    Type = "CONTACT",
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

                await contactRepository.InsertContactNewAudit(refContactEntity, message);
            }
        }

        private async Task DeleteContactRelatedRolesAsync(string email, Guid refContactEntityId)
        {
            var roles = await roleRepository.GetRolesForContactAsync(email);
            if (roles == null)
            {
                logger.LogInformation("{Instance} no roles to delete for the contact {email}", nameof(ContactsDeepValidationsService), email);
                return;
            }

            foreach (RoleEntity role in roles)
            {
                await HandleRoleDetetionAsync(role, refContactEntityId);
            }
        }

        private async Task HandleRoleDetetionAsync(RoleEntity role, Guid refContactEntityId)
        {
            if (role.RoleDuplicatesCounter > 0)
            {
                role.RoleDuplicatesCounter = 0;
                await roleRepository.UpdatePulseRole(role);
                logger.LogInformation("{Instance} Deleting role occurance for contact {email} on account {accountNumber}", nameof(ContactsDeepValidationsService), role.ContactEmail, role.AccountNumber);
            }

            // Create an approved delete operations

            await operationRepository.InsertNewOperation(new RegOperationEntity
            {
                Operation = OperationName.Delete,
                Type = "ROLE",
                EntityId = refContactEntityId,
                ApprovalStatus = ApprovalStatus.Approved,
                CreationDate = DateTime.UtcNow,
                ProcessStatus = ProcessStatus.Ready
            });
        }
    }
}

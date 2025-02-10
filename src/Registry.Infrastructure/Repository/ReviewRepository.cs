using Application.Interfaces;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Pulse.ContactRegistry.Domain.Context;
using Pulse.ContactRegistry.Domain.Entities;
using Registry.Application.Consts;

namespace Infrastructure.Repository
{
    public class ReviewRepository(RefContext refContext) : IReviewRepository
    {
        public async Task ReviewChangeEmailAsync()
        {
            var operationsCreated = await refContext.RegOperationEntity
                                                    .Where(x => x.Type == "CONTACT" && x.Operation == OperationType.Insert.ToString())
                                                    .Join(refContext.RefContactEntity,
                                                        operation => operation.EntityId,
                                                        contact => contact.EntityId,
                                                        (operation, contact) => new OperationContact
                                                        {
                                                            Operation = operation,
                                                            ContactFirstName = contact.FirstName,
                                                            ContactLastName = contact.LastName,
                                                            ContactLandPhone = contact.LandPhone,
                                                            ContactEmail = contact.Email
                                                        })
                                                    .AsNoTracking()
                                                    .ToListAsync();

            foreach (var op in operationsCreated)
            {
                var operationDeleted = await refContext.RegOperationEntity
                                                .Where(x => x.Type == "CONTACT" && x.Operation == OperationType.Delete.ToString())
                                                .Join(refContext.RefContactEntity,
                                                    operation => operation.EntityId,
                                                    contact => contact.EntityId,
                                                    (operation, contact) => new OperationContact
                                                    {
                                                        Operation = operation,
                                                        ContactFirstName = contact.FirstName,
                                                        ContactLastName = contact.LastName,
                                                        ContactLandPhone = contact.LandPhone,
                                                        ContactEmail = contact.Email
                                                    })
                                                .FirstOrDefaultAsync(x => x.ContactLandPhone == op.ContactLandPhone
                                                            && x.ContactFirstName == op.ContactFirstName
                                                            && x.ContactLastName == op.ContactLastName);

                if (operationDeleted != null)
                {
                    await ReplaceInsertAndDeleteByUpdate(op, operationDeleted);
                }
            }
        }

        #region Function ReplaceInsertAndDeleteByUpdate
        private class OperationContact
        {
            public RegOperationEntity? Operation;
            public string? ContactFirstName;
            public string? ContactLastName;
            public string? ContactEmail;
            public string? ContactLandPhone;
        }

        private async Task ReplaceInsertAndDeleteByUpdate(OperationContact operationInsert, OperationContact operationDelete)
        {
            var operationUpdate = new RegOperationEntity
            {
                CreationDate = DateTime.Now,
                EntityId = operationInsert.Operation!.EntityId,
                Type = "CONTACT",
                Operation = OperationType.Update.ToString(),
                ApprovalStatus = ApprovalStatus.Approved
            };
            refContext.RegOperationEntity.Add(operationUpdate);

            var operationsCreatedRole = await refContext.RegOperationEntity
                                                .Join(refContext.RefRoleEntity,
                                                    operation => operation.EntityId,
                                                    refRole => refRole.EntityId,
                                                    (operation, refRole) => new { operation, refRole })
                                                .Where(x => x.refRole.ContactEmail == operationInsert.ContactEmail
                                                    || x.refRole.ContactEmail == operationDelete.ContactEmail)
                                                .Select(x => x.operation)
                                                .ToListAsync();
            if (operationsCreatedRole.Count != 0)
            {
                refContext.RegOperationEntity.RemoveRange(operationsCreatedRole);
            }

            var operationsDeletedRole = await refContext.RegOperationEntity
                                                .Join(refContext.RefRoleEntity,
                                                operation => operation.EntityId,
                                                refRole => refRole.EntityId,
                                                (operation, refRole) => new { operation, refRole })
                                                .Where(x => x.refRole.ContactEmail == operationDelete.ContactEmail
                                                    || x.refRole.ContactEmail == operationDelete.ContactEmail)
                                                .Select(x => x.operation)
                                                .ToListAsync();
            if (operationsDeletedRole.Count != 0)
            {
                refContext.RegOperationEntity.RemoveRange(operationsDeletedRole);
            }

            refContext.RegOperationEntity.Remove(operationDelete.Operation!);
            refContext.RegOperationEntity.Remove(operationInsert.Operation!);

            await refContext.SaveChangesAsync();
        }

        #endregion
    }
}

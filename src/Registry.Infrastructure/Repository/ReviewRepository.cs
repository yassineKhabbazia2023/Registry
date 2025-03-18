using Application.Consts;
using Application.Interfaces;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Pulse.Registry.Domain.Context;
using Pulse.Registry.Domain.Entities;
using Registry.Application.Consts;
using OperationType = EFCore.BulkExtensions.OperationType;

namespace Infrastructure.Repository
{
    public class ReviewRepository(RefContext refContext) : IReviewRepository
    {
        public async Task ReviewChangeEmailAsync()
        {
            var operationsCreated = await refContext.RegOperationEntity
                                                    .Where(x => x.Type == "CONTACT"
                                                        && x.Operation == OperationName.Insert
                                                        && x.ProcessStatus == ProcessStatus.Ready)
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
                                                .Where(x => x.Type == "CONTACT" && x.Operation == OperationName.Delete)
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
                                                            && x.ContactLastName == op.ContactLastName
                                                            && x.Operation!.ProcessStatus == ProcessStatus.Ready);

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
            var oldRefContact = refContext.RefContactEntity.FirstOrDefault(x => x.EntityId == operationDelete.Operation!.EntityId);
            var operationUpdate = new RegOperationEntity
            {
                CreationDate = DateTime.Now,
                EntityId = operationInsert.Operation!.EntityId,
                Type = "CONTACT",
                Operation = OperationName.Update,
                ApprovalStatus = ApprovalStatus.Approved,
                ProcessStatus = ProcessStatus.Ready,
                OldContactEmail = oldRefContact!.Email
            };
            refContext.RegOperationEntity.Add(operationUpdate);

            var operationsCreatedRole = await refContext.RegOperationEntity
                                                .Where(x => x.EntityId == operationInsert.Operation.EntityId)
                                                .ToListAsync();
            if (operationsCreatedRole.Count != 0)
            {
                refContext.RegOperationEntity.RemoveRange(operationsCreatedRole);
            }

            var operationsDeletedRole = await refContext.RegOperationEntity
                                                .Where(x => x.EntityId == operationDelete.Operation.EntityId)
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

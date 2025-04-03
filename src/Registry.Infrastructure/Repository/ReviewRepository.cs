using Application.Consts;
using Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Pulse.Registry.Domain.Context;
using Pulse.Registry.Domain.Entities;
using Registry.Application.Consts;

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
                var operationsDeleted = await refContext.RegOperationEntity
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
                                                .ToListAsync();

                var operationDeleted = operationsDeleted.FirstOrDefault(x => IsUpdateEmail(x, op));

                if (operationDeleted != null)
                {
                    await ReplaceInsertAndDeleteByUpdate(op, operationDeleted);
                }
            }
        }

        private static bool IsUpdateEmail(OperationContact source, OperationContact destination)
        {
            return ConditionSameNameAndTel(source, destination)
                || ConditionSameNameAndDontHaveTel(source, destination);
        }

        #region Condition verify if it's an update email
        private static bool ConditionSameNameAndDontHaveTel(OperationContact source, OperationContact destination)
        {
            return (source.ContactFirstName == destination.ContactFirstName
               && source.ContactLastName == destination.ContactLastName
               && source.ContactLandPhone == null
               && destination.ContactLandPhone == null
               && source.ContactEmail != destination.ContactEmail
               && source.Operation!.ProcessStatus == ProcessStatus.Ready);
        }

        private static bool ConditionSameNameAndTel(OperationContact source, OperationContact destination)
        {
            return (source.ContactFirstName == destination.ContactFirstName
               && source.ContactLastName == destination.ContactLastName
               && source.ContactLandPhone == destination.ContactLandPhone
               && source.ContactEmail != destination.ContactEmail
               && source.Operation!.ProcessStatus == ProcessStatus.Ready);
        }
        #endregion

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

            await this.DeleteOperationRole(operationDelete.ContactEmail!, operationInsert.ContactEmail!);
            await this.UpdateRoleContactEmail(operationDelete.ContactEmail!, operationInsert.ContactEmail!);

            refContext.RegOperationEntity.Remove(operationInsert.Operation);
            refContext.RegOperationEntity.Remove(operationDelete!.Operation!);

            await refContext.SaveChangesAsync();
        }

        private async Task DeleteOperationRole(string oldEmail, string newEmail)
        {
            var operationRoleOldEmail = (from refRole in refContext.RefRoleEntity
                                         join opRole in refContext.RegOperationEntity on refRole.EntityId equals opRole.EntityId
                                         where refRole.ContactEmail == oldEmail && opRole.ProcessStatus != ProcessStatus.Succeeded
                                         select opRole);
            var operationRoleNewEmail = (from refRole in refContext.RefRoleEntity
                                         join opRole in refContext.RegOperationEntity on refRole.EntityId equals opRole.EntityId
                                         where refRole.ContactEmail == newEmail && opRole.ProcessStatus != ProcessStatus.Succeeded
                                         select opRole);
            if (operationRoleNewEmail.Any())
            {
                refContext.RegOperationEntity.RemoveRange(operationRoleNewEmail);
            }
            if (operationRoleOldEmail.Any())
            {
                refContext.RegOperationEntity.RemoveRange(operationRoleOldEmail);
            }           

            await refContext.SaveChangesAsync();
        }

        private async Task UpdateRoleContactEmail(string oldEmail, string newEmail)
        {
            var oldRoles = (from role in refContext.RoleEntities
                           where role.ContactEmail == oldEmail
                           select role);

            if (oldRoles.Any())
            {
                foreach(var role in oldRoles)
                {
                    role.ContactEmail = newEmail;
                    refContext.RoleEntities.Update(role);
                }
            }

            await refContext.SaveChangesAsync();
        }

        #endregion
    }
}

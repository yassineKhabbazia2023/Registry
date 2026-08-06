// <copyright file="RoleRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Consts;
using Application.Interfaces;
using Application.Models;
using EFCore.BulkExtensions;
using Application.Mappers;
using Microsoft.EntityFrameworkCore;
using Pulse.Registry.Domain.Entities;
using Pulse.Registry.Domain.Entities.Accounts;
using Pulse.Registry.Domain.Context;
namespace Infrastructure.Repository;

/// <summary>
/// ContactsRepository.
/// </summary>  
/// <param name="dbContext">dbContext.</param>
public class RoleRepository(RefContext refContext) : IRoleRepository
{
    public async Task AddRoleAsync(RoleEntity role)
    {
        var roleExists = await refContext.RoleEntity.FirstOrDefaultAsync(r => r.ContactId == role.ContactId && r.AccountId == role.AccountId) != null;
        if (!roleExists)
        {
            refContext.RoleEntity.Add(role);
            await refContext.SaveChangesAsync();
        }
    }

    public async Task DeleteRoleAsync(RoleEntity role)
    {
        refContext.RoleEntity.Remove(role);
        await refContext.SaveChangesAsync();
    }

    public async Task AddRolesAsync(IEnumerable<RefRoleCsv> roles, string? source = null)
    {
        var refRolesEntities = roles.MapRoleCsvsToRoleEntities();
        if(!string.IsNullOrWhiteSpace(source))
        {
            foreach (var refRole in refRolesEntities)
            {
                refRole.RoleSource = source;
            }
        }
        await refContext.BulkInsertAsync(refRolesEntities);
    }

    public IEnumerable<RefRoleEntity> GetUnprocessedRoles()
    {
        var roles = refContext.RefRoleEntity
            .AsNoTracking()
            .Where(role => role.ValidationDate == null)
            .OrderByDescending(r => r.OperationDate)
            .AsEnumerable();
        return roles;
    }

    public async Task<RefRoleEntity?> GetRefRoleAsync(string accountNumber, string emailAddress)
    {
        return await refContext.RefRoleEntity.AsNoTracking()
            .FirstOrDefaultAsync(role => role.AccountNumber == accountNumber
            && role.ContactEmail == emailAddress);
    }

    /// <inheritdoc />
    public async Task<bool> HasInsertRefRoleAsync(string accountNumber, string emailAddress)
    {
        return await refContext.RefRoleEntity
            .AsNoTracking()
            .AnyAsync(role => role.AccountNumber == accountNumber
                && role.ContactEmail == emailAddress
                && role.OperationType == OperationAction.Insert);
    }

    public async Task<bool> AddRefRoleAsync(RefRoleEntity refRoleEntity)
    {
        await refContext.AddAsync(refRoleEntity);
        await refContext.SaveChangesAsync();
        return true;
    }

    public bool DoesRoleExistInPulse(string accountNumber, string contactEmail)
    {
        var existed = (from roles in refContext.RoleEntity
                       join accounts in refContext.AccountEntity on roles.AccountId equals accounts.AccountId
                       join contacts in refContext.ContactEntity on roles.ContactId equals contacts.ContactId
                       where accounts.AccountNumber == accountNumber
                             && contacts.Email == contactEmail
                       select 1).Any();
        return existed;
    }

    public async Task<bool> DoesRoleExistInPulse(int accountId, int contactId)
    {
       bool existed = await refContext.RoleEntity.AnyAsync(role => role.AccountId == accountId && role.ContactId == contactId);
        return existed;
    }

    public async Task<RoleEntity?> GetPulseRole(string email, string accountNumber)
    {
        var role = await (from roles in refContext.RoleEntity
                          join accounts in refContext.AccountEntity on roles.AccountId equals accounts.AccountId
                          join contacts in refContext.ContactEntity on roles.ContactId equals contacts.ContactId
                          where accounts.AccountNumber == accountNumber
                                && contacts.Email == email
                          select new RoleEntity
                          {
                              AccountId = roles.AccountId,
                              ContactId = roles.ContactId,
                              AccountGlobalUniqueId = roles.AccountGlobalUniqueId,
                              AccountNumber = roles.AccountNumber,
                              ContactEmail = roles.ContactEmail,
                              ContactGlobalUniqueId = roles.AccountGlobalUniqueId,
                              RoleDuplicatesCounter = roles.RoleDuplicatesCounter,
                              ContactFlagPortailFactures = roles.ContactFlagPortailFactures,
                              ContactFlagMainContact = roles.ContactFlagMainContact,
                          }).AsNoTracking().FirstOrDefaultAsync();
        return role;
    }

    public async Task<IEnumerable<RoleEntity>?> GetRolesForContactAsync(string email)
    {
        return await (from roles in refContext.RoleEntity
                          join contacts in refContext.ContactEntity on roles.ContactId equals contacts.ContactId
                          where contacts.Email == email
                          select new RoleEntity
                          {
                              AccountId = roles.AccountId,
                              ContactId = roles.ContactId,
                              AccountGlobalUniqueId = roles.AccountGlobalUniqueId,
                              AccountNumber = roles.AccountNumber,
                              ContactEmail = roles.ContactEmail,
                              ContactGlobalUniqueId = roles.AccountGlobalUniqueId,
                              RoleDuplicatesCounter = roles.RoleDuplicatesCounter,
                              ContactFlagPortailFactures = roles.ContactFlagPortailFactures,
                              ContactFlagMainContact = roles.ContactFlagMainContact,
                          }).AsNoTracking().ToListAsync();

    }

    public async Task<bool> UpdatePulseRole(RoleEntity updatedRole)
    {
        bool exists = await DoesRoleExistInPulse(updatedRole.AccountId, updatedRole.ContactId);
        if (!exists)
        {
            return false;
        }
        refContext.RoleEntity.Update(updatedRole);
        await refContext.SaveChangesAsync();
        /*
         * When reviewing failed operation, once we update the role counter once, the object become tracked
         * by entity framework. So, if another line in the audit table refers the same role, when trying to
         * update it, EF complains that we try to track an entity that is already tracked.
         * As a temporary solution, we will clear tracking and come back later for a deeper reflexion
         * on object lifecycle and concurrent update.
         */
        refContext.ChangeTracker.Clear();
        return true;
    }

    public async Task<IList<RefRoleEntity>?> GetDeepValidationFailedRoles(int numberOfDays = -7)
    {
        var nDaysAgo = DateTime.UtcNow.AddDays(numberOfDays);

        var result = await (from roles in refContext.RefRoleEntity
                            join deepValidation in refContext.DeepValidationEntity
                                on roles.EntityId equals deepValidation.EntityId
                            where deepValidation.Type == "role"
                                  && deepValidation.CreationDate >= nDaysAgo
                            select roles)
                            .AsNoTracking()
                            .ToListAsync();

        return result;
    }

    public async Task UpdateRefRoleAsync(RefRoleEntity refRoleEntity)
    {
        refContext.Update(refRoleEntity);
        await refContext.SaveChangesAsync();
;    }
}

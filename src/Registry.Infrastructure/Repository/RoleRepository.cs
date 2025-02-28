// <copyright file="RoleRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Models;
using Domain.Entities.Accounts;
using EFCore.BulkExtensions;
using Pulse.Registry.Domain.Context;
using Application.Mappers;
using Microsoft.EntityFrameworkCore;
using Pulse.Registry.Domain.Entities;
using Application.Consts;
using System.Runtime.CompilerServices;
using Application.Models.Accounts;
using Azure;
using Microsoft.Extensions.Options;
using Application.Options;
namespace Infrastructure.Repository;

/// <summary>
/// ContactsRepository.
/// </summary>  
/// <param name="dbContext">dbContext.</param>
public class RoleRepository(RefContext refContext) : IRoleRepository
{
    public async Task AddRoleAsync(RoleEntity role)
    {
        var roleExists = refContext.RoleEntities.FirstOrDefault(r => r.ContactId == role.ContactId && r.AccountId == role.AccountId) != null;
        if (!roleExists)
        {
            refContext.RoleEntities.Add(role);
            await refContext.SaveChangesAsync();
        }
    }

    public async Task DeleteRoleAsync(RoleEntity role)
    {
        refContext.RoleEntities.Remove(role);
        await refContext.SaveChangesAsync();
    }
    public async Task AddRolesAsync(IEnumerable<RefRoleCsv> roles)
    {
        await refContext.BulkInsertAsync(roles.MapRoleCsvsToRoleEntities());
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

    public async Task<bool> AddRefRoleAsync(RefRoleEntity refRoleEntity)
    {
        await refContext.AddAsync(refRoleEntity);
        await refContext.SaveChangesAsync();
        return true;
    }

    public bool DoesRoleExistInPulse(string accountNumber, string contactEmail)
    {
        var existed = (from roles in refContext.RoleEntities
                       join accounts in refContext.AccountEntities on roles.AccountId equals accounts.AccountId
                       join contacts in refContext.ContactEntities on roles.ContactId equals contacts.ContactId
                       where accounts.AccountNumber == accountNumber
                             && contacts.Email == contactEmail
                       select 1).Any();
        return existed;
    }

    public async Task<bool> DoesRoleExistInPulse(int accountId, int contactId)
    {
       bool existed = await refContext.RoleEntities.AnyAsync(role => role.AccountId == accountId && role.ContactId == contactId);
        return existed;
    }

    public async Task<bool> DoesRoleExistInOperations(string accountNumber, string contactEmail, string operation, string processStatus)
    {
        var existed = await (from roleOperation in refContext.RegOperationEntity
                       join roleRef in refContext.RefRoleEntity on roleOperation.EntityId equals roleRef.EntityId
                       where roleRef.AccountNumber == accountNumber
                           && roleRef.ContactEmail == contactEmail
                           && roleOperation.Operation == operation
                           && roleOperation.ProcessStatus == processStatus
                       select 1).AnyAsync();
        return existed;
    }


    public async Task<RoleEntity?> GetPulseRole(string email, string accountNumber)
    {
        var role = await (from roles in refContext.RoleEntities
                          join accounts in refContext.AccountEntities on roles.AccountId equals accounts.AccountId
                          join contacts in refContext.ContactEntities on roles.ContactId equals contacts.ContactId
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
                          }).AsNoTracking().FirstOrDefaultAsync();
        return role;
    }

    public async Task<IEnumerable<RoleEntity>?> GetRolesForContactAsync(string email)
    {
        return await (from roles in refContext.RoleEntities
                          join contacts in refContext.ContactEntities on roles.ContactId equals contacts.ContactId
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
                          }).AsNoTracking().ToListAsync();

    }

    public async Task<bool> UpdatePulseRole(RoleEntity updatedRole)
    {
        bool exists = await DoesRoleExistInPulse(updatedRole.AccountId, updatedRole.ContactId);
        if (!exists)
        {
            return false;
        }
        refContext.RoleEntities.Update(updatedRole);
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
                            join deepValidation in refContext.DeepValidationEntities
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

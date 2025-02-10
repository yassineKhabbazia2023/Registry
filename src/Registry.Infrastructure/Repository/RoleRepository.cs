// <copyright file="RoleRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Models;
using Domain.Entities.Accounts;
using EFCore.BulkExtensions;
using Pulse.ContactRegistry.Domain.Context;
using Application.Mappers;
using Microsoft.EntityFrameworkCore;
using Pulse.ContactRegistry.Domain.Entities;
using Application.Consts;
using System.Runtime.CompilerServices;
using Application.Models.Accounts;
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
            .Where(role => !refContext.RegOperationEntity.Any(operation => operation.EntityId == role.EntityId && operation.Type == "ROLE"))
            .AsEnumerable();
        return roles;
    }

    public async Task<RefRoleEntity?> GetRefRoleAsync(string accountNumber, string emailAddress)
    {
        return await refContext.RefRoleEntity.AsNoTracking()
            .FirstOrDefaultAsync(role => role.AccountNumber.ToLower() == accountNumber.ToLower()
            && role.ContactEmail.ToLower() == emailAddress.ToLower());
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
                       where accounts.AccountNumber.ToLower() == accountNumber.ToLower()
                             && contacts.Email.ToLower() == contactEmail.ToLower()
                       select 1).Any();
        return existed;
    }

    public async Task<bool> DoesRoleExistInPulse(int accountId, int contactId)
    {
       bool existed = await refContext.RoleEntities.AnyAsync(role => role.AccountId == accountId && role.ContactId == contactId);
        return existed;
    }


    public async Task<RoleEntity?> GetPulseRole(string email, string accountNumber)
    {
        var role = await (from roles in refContext.RoleEntities
                          join accounts in refContext.AccountEntities on roles.AccountId equals accounts.AccountId
                          join contacts in refContext.ContactEntities on roles.ContactId equals contacts.ContactId
                          where accounts.AccountNumber.ToLower() == accountNumber.ToLower()
                                && contacts.Email.ToLower() == email.ToLower()
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
                          where contacts.Email.ToLower() == email.ToLower()
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
        return true;
    }



}

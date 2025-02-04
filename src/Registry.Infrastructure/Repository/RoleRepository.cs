// <copyright file="RoleRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Models;
using Domain.Entities.Accounts;
using EFCore.BulkExtensions;
using Pulse.ContactRegistry.Domain.Context;
using Application.Mappers;
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
}

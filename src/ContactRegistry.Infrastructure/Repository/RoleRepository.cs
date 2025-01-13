// <copyright file="RoleRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Models;
using Domain.Entities;
using EFCore.BulkExtensions;
using Infrastructure.Context;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;
using Pulse.ContactRegistry.Infrastructure.Context;
using CreRole = Domain.Entities.CreRole;

namespace Infrastructure.Repository;

/// <summary>
/// ContactsRepository.
/// </summary>  
/// <param name="dbContext">dbContext.</param>
public class RoleRepository(ApplicationDbContext dbContext, RefContext refContext) : IRoleRepository
{
    public async Task AddRolesAsync(IEnumerable<AlxRole> roles)
    {
        await dbContext.BulkInsertAsync(roles);
    }

    public async Task AddRolesAsync(IEnumerable<RefRoleCsv> roles)
    {
        await refContext.BulkInsertAsync(roles.MapRoleCsvsToRoleEntities());
    }

    public async IAsyncEnumerable<CreRole> GetRolesAsync()
    {
        if(dbContext.CreRoles.Any())
        {
            await foreach (var role in dbContext.CreRoles.AsAsyncEnumerable())
            {
                yield return role;
            }
        }
    }

    public async Task<(int creRoleActif, int alxRoleActif)> GetCountRolesActifAsync()
    {
        var countCreAct = await dbContext.CreRoles.CountAsync(r => r.Deleted == null);
        var countAlxAct = await dbContext.AlxRoles.CountAsync();
        return (countAlxAct, countAlxAct);
    }

    public async Task ClearAlxAsync()
    {
        await dbContext.AlxRoles.ExecuteDeleteAsync();
    }
}

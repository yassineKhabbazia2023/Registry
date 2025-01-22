// <copyright file="RoleRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Models;
using EFCore.BulkExtensions;
using Infrastructure.Mappers;
using Pulse.ContactRegistry.Infrastructure.Context;

namespace Infrastructure.Repository;

/// <summary>
/// ContactsRepository.
/// </summary>  
/// <param name="dbContext">dbContext.</param>
public class RoleRepository(RefContext refContext) : IRoleRepository
{
 
    public async Task AddRolesAsync(IEnumerable<RefRoleCsv> roles)
    {
        await refContext.BulkInsertAsync(roles.MapRoleCsvsToRoleEntities());
    }
}

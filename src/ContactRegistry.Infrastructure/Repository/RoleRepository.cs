// <copyright file="ContactConfiguration.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Domain.Entities;
using EFCore.BulkExtensions;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace Infrastructure.Repository
{
    /// <summary>
    /// ContactsRepository.
    /// </summary>  
    /// <param name="dbContext">dbContext.</param>

    [ExcludeFromCodeCoverage]
    public class RoleRepository(ApplicationDbContext dbContext)
        : IRoleRepository
    {
        public async Task AddRolesAsync(IEnumerable<AlxRole> roles)
        {
            await dbContext.BulkInsertAsync(roles);
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
}

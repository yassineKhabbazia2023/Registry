// <copyright file="ContactConfiguration.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Context;

namespace Infrastructure.Repository
{
    /// <summary>
    /// ContactsRepository.
    /// </summary>
    /// <param name="dbContext">dbContext.</param>
    public class RoleRepository(ApplicationDbContext dbContext)
        : IRoleRepository
    {
        public async Task AddRolesAsync(IEnumerable<AlxRole> roles)
        {
            await dbContext.AlxRoles.AddRangeAsync(roles);
            await dbContext.SaveChangesAsync();
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
    }
}

// <copyright file="IRoleRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;
using Domain.Entities;
using CreRole = Domain.Entities.CreRole;

namespace Application.Interfaces;

public interface IRoleRepository
{
    Task AddRolesAsync(IEnumerable<AlxRole> roles);

    IAsyncEnumerable<CreRole> GetRolesAsync();

    Task<(int creRoleActif, int alxRoleActif)> GetCountRolesActifAsync();

    Task ClearAlxAsync();

    Task AddRolesAsync(IEnumerable<RefRoleCsv> roles);
}

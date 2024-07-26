// <copyright file="IRoleService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;
using Domain.Entities;

namespace Application.Interfaces
{
    public interface IRoleService
    {
        Task ProcessRoleAsync(IEnumerable<RoleCsv> roles);
        Task StreamRolesJsonAsync(StreamWriter streamWriter);

        Task ClearAlxAsync();
    }
}

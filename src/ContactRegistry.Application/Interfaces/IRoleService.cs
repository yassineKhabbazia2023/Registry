// <copyright file="IRoleService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;

namespace Application.Interfaces;

public interface IRoleService
{
    Task ProcessRoleAsync(IEnumerable<RoleCsv> roles);

    Task StreamRolesJsonAsync(StreamWriter streamWriter);

    Task ClearAlxAsync();

    /// <summary>
    /// Inserts all roles and operations into [ref].[Role] table.
    /// </summary>
    /// <param name="roles">Roles inserted.</param>
    /// <returns>A <see cref="Task"/> representing the async operation.</returns>
    Task InsertRolesAsync(IEnumerable<RefRoleCsv> roles);
}

// <copyright file="IRoleRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;
using Application.Models.Accounts;
using Domain.Entities.Accounts;

namespace Application.Interfaces;

public interface IRoleRepository
{ 

    Task AddRolesAsync(IEnumerable<RefRoleCsv> roles);

    Task AddRoleAsync(RoleEntity role);

    Task DeleteRoleAsync(RoleEntity role);
}

// <copyright file="IRoleRegistryService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;

namespace Application.Interfaces;

public interface IRoleRegistryService
{
    Task<bool> DoesRoleExistAsync(RoleRegistry role);

    Task CreateRoleAsync(RoleRegistry role);

    Task UpdateRoleAsync(RoleRegistry role);
}

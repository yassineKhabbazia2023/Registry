// <copyright file="IRoleRegistryProvider.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;

namespace Application.Interfaces;

public interface IRoleRegistryProvider
{
    Task CreateRoleAsync(RoleRegistry role);

    Task UpdateRoleAsync(RoleRegistry role);
}

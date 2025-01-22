// <copyright file="RoleService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Models;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public class RoleService : IRoleService
{
    private readonly IRoleRepository roleRepository;
    private readonly ILogger<RoleService> logger;

    public RoleService(ILogger<RoleService> logger, IRoleRepository roleRepository)
    {
        this.roleRepository = roleRepository;
        this.logger = logger;
    }

    public async Task InsertRolesAsync(IEnumerable<RefRoleCsv> roles)
    {
        await roleRepository.AddRolesAsync(roles);
    }
}

// <copyright file="RoleService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Interfaces.RuleValidators;
using Application.Models;
using Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pulse.Registry.Domain.Entities;
using System.Windows.Markup;

namespace Application.Services;

public class RoleService : IRoleService
{
    private readonly IRoleRepository roleRepository;
    private readonly ILogger<RoleService> logger;
    private readonly IRoleDeepValidatorFactory roleDeepValidatorFactory;
    private readonly IOptions<BackGroundJobOptions> backGroundJobOptions;

    public RoleService(ILogger<RoleService> logger, IRoleRepository roleRepository, IRoleDeepValidatorFactory roleDeepValidatorFactory, IOptions<BackGroundJobOptions> backGroundJobOptions)
    {
        this.roleRepository = roleRepository;
        this.logger = logger;
        this.roleDeepValidatorFactory = roleDeepValidatorFactory;
        this.backGroundJobOptions = backGroundJobOptions;
    }

    public async Task InsertRolesAsync(IEnumerable<RefRoleCsv> roles)
    {
        await roleRepository.AddRolesAsync(roles);
    }

    private async Task<bool> ValidateRoleOperationOfTypeInsert(RefRoleEntity refRoleEntity, IRoleDeepValidator validator)
    {
        var result = await (await (await (await (await (await validator
            .Instantiate(refRoleEntity))
            .ContactShouldExistInPulseOrOperations())
            .AccountShouldExistInPulseOrOperations())
            .RoleShouldShouldNotExistInPulseOrOperations())
            .TryAddOperation())
            .Validate();
        return result; 
    }

    private async Task<bool> ValidateRoleOperationOfTypeDelete(RefRoleEntity refRoleEntity , IRoleDeepValidator validator)
    {
        var result = await (await (await (await (await (await validator
            .Instantiate(refRoleEntity))
            .ContactShouldExistInPulse())
            .AccountShouldExistInPulse())
            .RoleShouldExistInPulse())
            .TryAddOperation())
            .Validate();
        return result;
    }


    public async Task<IList<bool>> CreateValidRolesOperationsAsync()
    {
        var roles = this.roleRepository.GetUnprocessedRoles();
        var results = new List<bool>();
        foreach (var role in roles)
        {
            role.ValidationDate = DateTime.UtcNow;
            await this.roleRepository.UpdateRefRoleAsync(role);

            var validator = this.roleDeepValidatorFactory.Create();
            switch (role.OperationType)
            {
                case "INSERT":
                    results.Add(await ValidateRoleOperationOfTypeInsert(role, validator));
                    break;
                case "DELETE":
                    results.Add(await ValidateRoleOperationOfTypeDelete(role, validator));
                    break;
                default:
                    break;
            }
        }
        return results;
    }

    public async Task<IList<bool>> ReviewFailedRolesOperationsAsync()
    {
        int numberOfDays = backGroundJobOptions.Value.NumberOfDaysToRetryFailedRoles;
        var roles = await this.roleRepository.GetDeepValidationFailedRoles(numberOfDays);
        var results = new List<bool>();
        foreach (var role in roles)
        {
            var validator = this.roleDeepValidatorFactory.Create();
            switch (role.OperationType)
            {
                case "INSERT":
                    results.Add(await ValidateRoleOperationOfTypeInsert(role, validator));
                    break;
                case "DELETE":
                    results.Add(await ValidateRoleOperationOfTypeDelete(role, validator));
                    break;
                default:
                    break;
            }
        }
        return results;
    }
}

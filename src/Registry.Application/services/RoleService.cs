// <copyright file="RoleService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Interfaces.RuleValidators;
using Application.Models;
using Microsoft.Extensions.Logging;
using Pulse.Registry.Domain.Entities;
using System.Windows.Markup;

namespace Application.Services;

public class RoleService : IRoleService
{
    private readonly IRoleRepository roleRepository;
    private readonly ILogger<RoleService> logger;
    private readonly IRoleDeepValidatorFactory roleDeepValidatorFactory;
    public RoleService(ILogger<RoleService> logger, IRoleRepository roleRepository, IRoleDeepValidatorFactory roleDeepValidatorFactory)
    {
        this.roleRepository = roleRepository;
        this.logger = logger;
        this.roleDeepValidatorFactory = roleDeepValidatorFactory;
    }

    public async Task InsertRolesAsync(IEnumerable<RefRoleCsv> roles)
    {
        await roleRepository.AddRolesAsync(roles);
    }


    // unfortunately I could not work in parallel programming 
    // the EntityFramework refuse to use it in parallel processes. 
    // in the future i need to find a way to make it work in parallel. 
    //public async Task<IEnumerable<bool>> CreateValidRolesOperationsAsync()
    //{
    //    var roleList = this.roleRepository.GetUnprocessedRoles();
    //    var tasks = roleList.Select(async role =>
    //    {
    //        var validator = this.roleDeepValidatorFactory.Create();
    //        return role.OperationType switch
    //        {
    //            "INSERT" => await ValidateRoleOperationOfTypeInsert(role, validator),
    //            "DELETE" => await ValidateRoleOperationOfTypeDelete(role, validator),
    //            _ => false
    //        };
    //    });

    //    return await Task.WhenAll(tasks);
    //}


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
        var roles = await this.roleRepository.GetDeepValidationFailedRoles();
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

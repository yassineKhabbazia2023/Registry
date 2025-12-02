// <copyright file="RoleService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Consts;
using Application.Interfaces;
using Application.Interfaces.RuleValidators;
using Application.Models;
using Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pulse.Registry.Domain.Entities;
using System.Windows.Markup;
using Domain.Entities.Accounts;

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

    public async Task InsertRolesAsync(IEnumerable<RefRoleCsv> roles, string? source = null)
    {
        await roleRepository.AddRolesAsync(roles, source);
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

    private async Task<bool> ValidateRoleOperationOfTypeDelete(RefRoleEntity refRoleEntity, IRoleDeepValidator validator)
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
                case OperationAction.Insert:
                    results.Add(await ValidateRoleOperationOfTypeInsert(role, validator));
                    break;
                case OperationAction.Delete:
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
    
    public async Task RestRoleDuplicateCounter(IEnumerable<string>? accountNumbers, string email)
    {
        if (accountNumbers == null || !accountNumbers.Any())
        {
            logger.LogWarning("[{Handler}] No account numbers provided for user {Email}. Skipping reset.", nameof(RoleService), email);
            return;
        }

        logger.LogInformation("[{Handler}] Starting reset of RoleDuplicatesCounter for user {Email} on {Count} accounts.", nameof(RoleService), email, accountNumbers.Count());

        foreach (var accountNumber in accountNumbers)
        {
            try
            {
                RoleEntity? roleEntity = await roleRepository.GetPulseRole(email, accountNumber);
                if (roleEntity == null)
                {
                    logger.LogWarning("[{Handler}] No role found for user {Email} on account {Account}. Skipping.", nameof(RoleService), email, accountNumber);
                    continue;
                }

                if (roleEntity.RoleDuplicatesCounter > 0)
                {
                    roleEntity.RoleDuplicatesCounter = 0;
                    var updated = await roleRepository.UpdatePulseRole(roleEntity);

                    if (updated)
                    {
                        logger.LogInformation("[{Handler}] Successfully reset RoleDuplicatesCounter for user {Email} on account {Account}.", nameof(RoleService), email, accountNumber);
                    }
                    else
                    {
                        logger.LogError("[{Handler}] Failed to update RoleDuplicatesCounter for user {Email} on account {Account}.", nameof(RoleService), email, accountNumber);
                    }
                }
                else
                {
                    logger.LogDebug("[{Handler}] RoleDuplicatesCounter already zero for user {Email} on account {Account}. No update needed.", nameof(RoleService), email, accountNumber);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[{Handler}] Exception resetting RoleDuplicatesCounter for user {Email} on account {Account}.", nameof(RoleService), email, accountNumber);
            }
        }

        logger.LogInformation("[{Handler}] Completed reset of RoleDuplicatesCounter for user {Email}.", nameof(RoleService), email);
    }
}

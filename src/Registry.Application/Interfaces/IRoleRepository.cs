// <copyright file="IRoleRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Consts;
using Application.Models;
using Application.Models.Accounts;
using Azure;
using Domain.Entities.Accounts;
using Pulse.Registry.Domain.Entities;

namespace Application.Interfaces;

public interface IRoleRepository
{ 

    Task AddRolesAsync(IEnumerable<RefRoleCsv> roles);

    Task AddRoleAsync(RoleEntity role);

    Task DeleteRoleAsync(RoleEntity role);

    Task<RefRoleEntity?> GetRefRoleAsync(string accountNumber, string emailAddress);

    Task<bool> AddRefRoleAsync(RefRoleEntity refRoleEntity);

    IEnumerable<RefRoleEntity> GetUnprocessedRoles();

    bool DoesRoleExistInPulse(string accountNumber, string contactEmail);
    Task<bool> DoesRoleExistInPulse(int accountId, int contactId);
    Task<bool> DoesRoleExistInOperations(string accountNumber, string contactEmail, string operation, string processStatus);
    Task<RoleEntity?> GetPulseRole(string email, string accountNumber);
    Task<bool> UpdatePulseRole(RoleEntity updatedRole);

    Task<IEnumerable<RoleEntity>?> GetRolesForContactAsync(string email);

    Task<IList<RefRoleEntity>> GetDeepValidationFailedRoles(int numberOfDays = -7);

    Task UpdateRefRoleAsync(RefRoleEntity refRoleEntity);
}

// <copyright file="IRoleRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;
using Pulse.Registry.Domain.Entities;
using Pulse.Registry.Domain.Entities.Accounts;

namespace Application.Interfaces;

public interface IRoleRepository
{ 

    Task AddRolesAsync(IEnumerable<RefRoleCsv> roles, string? source = null);

    Task AddRoleAsync(RoleEntity role);

    Task DeleteRoleAsync(RoleEntity role);

    Task<RefRoleEntity?> GetRefRoleAsync(string accountNumber, string emailAddress);

    /// <summary>
    /// Determines whether an equivalent INSERT role exists in the Registry reference table.
    /// </summary>
    /// <param name="accountNumber">The account number associated with the role.</param>
    /// <param name="emailAddress">The contact email associated with the role.</param>
    /// <returns><see langword="true"/> when the equivalent INSERT role exists; otherwise, <see langword="false"/>.</returns>
    Task<bool> HasInsertRefRoleAsync(string accountNumber, string emailAddress);

    Task<bool> AddRefRoleAsync(RefRoleEntity refRoleEntity);

    IEnumerable<RefRoleEntity> GetUnprocessedRoles();

    bool DoesRoleExistInPulse(string accountNumber, string contactEmail);

    Task<bool> DoesRoleExistInPulse(int accountId, int contactId);

    Task<RoleEntity?> GetPulseRole(string email, string accountNumber);

    Task<bool> UpdatePulseRole(RoleEntity updatedRole);

    Task<IEnumerable<RoleEntity>?> GetRolesForContactAsync(string email);

    Task<IList<RefRoleEntity>> GetDeepValidationFailedRoles(int numberOfDays = -7);

    Task UpdateRefRoleAsync(RefRoleEntity refRoleEntity);
}

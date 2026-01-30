// <copyright file="DeepValidationRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pulse.Registry.Domain.Context;
using Pulse.Registry.Domain.Entities.Audits;

namespace Infrastructure.Repository;

public class DeepValidationRepository : IDeepValidationRepository
{
    private readonly RefContext _refContext;
    private readonly ILogger<DeepValidationRepository> _logger;


    public DeepValidationRepository(RefContext refContext, ILogger<DeepValidationRepository> logger)
    {
        _refContext = refContext;
        _logger = logger;
    }


    public async Task<bool> AddDeepValidationAsync(DeepValidationEntity deepValidation)
    {
        return await TryReposAction<DeepValidationEntity>(async (deepValidation) =>
        {
            await _refContext.AddAsync(deepValidation);
            await _refContext.SaveChangesAsync();
            return true;
        }, deepValidation);
    }

    public async Task<bool> DoesDeepValidationLineExistsAsync(Guid entityId, string operationType)
    {
        // Check if a deep validation record exists with the same EntityId and matching operation type.
        bool existsByEntityId = await _refContext.DeepValidationEntity
            .AnyAsync(x => x.EntityId == entityId && x.Type == operationType);
        if (existsByEntityId)
        {
            return true;
        }

        // Retrieve the role record from the ref.Role table using the provided EntityId.
        var role = await _refContext.RefRoleEntity
            .FirstOrDefaultAsync(x => x.EntityId == entityId);

        // If no role is found, then there’s nothing to compare against.
        if (role == null)
        {
            return false;
        }

        // Check if a deep validation record exists that,
        // when joined with the ref.Role table, has the same AccountNumber and ContactEmail.
        bool existsByAccountAndEmail = await _refContext.DeepValidationEntity
            .Join(
                _refContext.RefRoleEntity,
                deep => deep.EntityId,
                r => r.EntityId,
                (deep, r) => new { Deep = deep, Role = r }
            )
            .AnyAsync(x => x.Deep.Type == operationType &&
                           x.Role.AccountNumber == role.AccountNumber &&
                           x.Role.ContactEmail == role.ContactEmail);

        return existsByAccountAndEmail;
    }

    private async Task<bool> TryReposAction<T>(Func<T, Task<bool>> functionExecution, T t)
    {
        try
        {
            return await functionExecution(t);
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError($"[Method]: {nameof(functionExecution)}; [Error]: {dbEx.Message}");
            return false;
        }
        catch (InvalidOperationException ioEx)
        {
            _logger.LogError($"[Method]: {nameof(functionExecution)}; [Error]: {ioEx.Message}");
            return false;
        }
    }

}

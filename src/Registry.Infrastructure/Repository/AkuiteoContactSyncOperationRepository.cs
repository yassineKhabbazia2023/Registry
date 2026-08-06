// <copyright file="AkuiteoContactSyncOperationRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Consts;
using Application.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Pulse.Registry.Domain.Context;
using Pulse.Registry.Domain.Entities.Audits;

namespace Infrastructure.Repository;

public class AkuiteoContactSyncOperationRepository(RefContext refContext)
    : IAkuiteoContactSyncOperationRepository
{
    private const int LastErrorMaxLength = 2000;

    public async Task<bool> AddIfNotExistsAsync(AkuiteoContactSyncOperationEntity operation)
    {
        if (await refContext.AkuiteoContactSyncOperationEntity
            .AsNoTracking()
            .AnyAsync(existingOperation => existingOperation.SourceEventId == operation.SourceEventId))
        {
            return false;
        }

        refContext.AkuiteoContactSyncOperationEntity.Add(operation);

        try
        {
            await refContext.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            refContext.Entry(operation).State = EntityState.Detached;
            return false;
        }
    }

    public async Task<IReadOnlyCollection<AkuiteoContactSyncOperationEntity>> GetEligiblePendingAsync()
    {
        return await (
            from operation in refContext.AkuiteoContactSyncOperationEntity
            join account in refContext.AccountEntity
                on operation.AccountId equals account.AccountId
            join contact in refContext.ContactEntity
                on operation.ContactId equals contact.ContactId
            join role in refContext.RoleEntity
                on new { operation.AccountId, operation.ContactId }
                equals new { role.AccountId, role.ContactId }
            where operation.Status == AkuiteoContactSyncOperationStatus.Pending
            orderby operation.CreatedAt, operation.Id
            select operation)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<bool> TryMarkProcessingAsync(long id)
    {
        var updatedOperations = await refContext.AkuiteoContactSyncOperationEntity
            .Where(operation =>
                operation.Id == id
                && operation.Status == AkuiteoContactSyncOperationStatus.Pending)
            .ExecuteUpdateAsync(properties => properties
                .SetProperty(
                    operation => operation.Status,
                    AkuiteoContactSyncOperationStatus.Processing)
                .SetProperty(operation => operation.UpdatedAt, DateTime.UtcNow));

        return updatedOperations == 1;
    }

    public async Task MarkSentAsync(long id, string akuiteoContactId)
    {
        var utcNow = DateTime.UtcNow;
        var operation = await GetRequiredOperationAsync(id);
        operation.Status = AkuiteoContactSyncOperationStatus.Sent;
        operation.AkuiteoContactId = akuiteoContactId;
        operation.SentAt = utcNow;
        operation.LastError = null;
        operation.UpdatedAt = utcNow;
        await refContext.SaveChangesAsync();
    }

    public async Task MarkFailedAsync(long id, string error)
    {
        var operation = await GetRequiredOperationAsync(id);
        operation.Status = AkuiteoContactSyncOperationStatus.Failed;
        operation.LastError = error[..Math.Min(error.Length, LastErrorMaxLength)];
        operation.UpdatedAt = DateTime.UtcNow;
        await refContext.SaveChangesAsync();
    }

    private async Task<AkuiteoContactSyncOperationEntity> GetRequiredOperationAsync(long id)
    {
        return await refContext.AkuiteoContactSyncOperationEntity.SingleAsync(operation => operation.Id == id);
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        return exception.InnerException is SqlException { Number: 2601 or 2627 };
    }
}

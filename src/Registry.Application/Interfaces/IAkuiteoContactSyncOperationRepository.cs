// <copyright file="IAkuiteoContactSyncOperationRepository.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Pulse.Registry.Domain.Entities.Audits;

namespace Application.Interfaces;

public interface IAkuiteoContactSyncOperationRepository
{
    Task<bool> AddIfNotExistsAsync(AkuiteoContactSyncOperationEntity operation);

    Task<IReadOnlyCollection<AkuiteoContactSyncOperationEntity>> GetEligiblePendingAsync();

    Task<bool> TryMarkProcessingAsync(long id);

    Task MarkSentAsync(long id, string akuiteoContactId);

    Task MarkFailedAsync(long id, string error);
}

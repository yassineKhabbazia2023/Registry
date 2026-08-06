// <copyright file="AkuiteoContactSyncOperationService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Consts;
using Application.Enums;
using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Pulse.Back.Events.IntegrationEvents;
using Pulse.Registry.Domain.Entities.Audits;

namespace Application.Services;

public class AkuiteoContactSyncOperationService(
    IAkuiteoContactSyncOperationRepository operationRepository,
    IContactAkuiteoSynchronizer contactAkuiteoSynchronizer,
    IFeatureFlagService featureFlagService,
    ILogger<AkuiteoContactSyncOperationService> logger)
    : IAkuiteoContactSyncOperationService
{
    /// <inheritdoc />
    public async Task<bool> EnqueueAsync(RoleCreatedEvent roleEvent, Guid sourceEventId)
    {
        return await EnqueueAsync(roleEvent, sourceEventId, isRoleFromAkuiteo: false);
    }

    /// <inheritdoc />
    public async Task<bool> EnqueueAsync(
        RoleCreatedEvent roleEvent,
        Guid sourceEventId,
        bool isRoleFromAkuiteo)
    {
        ArgumentNullException.ThrowIfNull(roleEvent);
        ArgumentNullException.ThrowIfNull(roleEvent.Data);

        if (!featureFlagService.IsEnabled(FeatureFlagKeys.IsContactAkuiteoSynchronizationEnabled))
        {
            logger.LogInformation(
                "Skipping Akuiteo contact synchronization operation because the feature flag is disabled. EventId: {EventId}, ContactId: {ContactId}, AccountId: {AccountId}",
                sourceEventId,
                roleEvent.Data.ContactId,
                roleEvent.Data.AccountId);
            return false;
        }

        var utcNow = DateTime.UtcNow;
        await operationRepository.AddIfNotExistsAsync(new AkuiteoContactSyncOperationEntity
        {
            SourceEventId = sourceEventId,
            AccountId = roleEvent.Data.AccountId,
            AccountNumber = roleEvent.Data.AccountNumber,
            AccountType = roleEvent.AccountType ?? string.Empty,
            ContactId = roleEvent.Data.ContactId,
            ContactEmail = roleEvent.Data.ContactEmail,
            ContactFlagPortailFactures = roleEvent.Data.ContactFlagPortailFactures,
            IsSignatory = roleEvent.Data.IsSignatory,
            Reason = isRoleFromAkuiteo
                ? AkuiteoContactSyncReason.RoleFromAkuiteo
                : AkuiteoContactSyncReason.RoleCreatedFromPulse,
            Status = isRoleFromAkuiteo
                ? AkuiteoContactSyncOperationStatus.Skipped
                : AkuiteoContactSyncOperationStatus.Pending,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        });

        return true;
    }

    public async Task ProcessPendingOperationsAsync()
    {
        var operations = await operationRepository.GetEligiblePendingAsync();
        foreach (var operation in operations)
        {
            if (await operationRepository.TryMarkProcessingAsync(operation.Id) == false)
            {
                continue;
            }

            await ProcessOperationAsync(operation);
        }
    }

    private async Task ProcessOperationAsync(AkuiteoContactSyncOperationEntity operation)
    {
        try
        {
            var synchronizationResult = await contactAkuiteoSynchronizer.SynchronizeAsync(new()
            {
                AccountId = operation.AccountId,
                ContactId = operation.ContactId,
                AccountNumber = operation.AccountNumber,
                ContactEmail = operation.ContactEmail,
                ContactFlagPortailFactures = operation.ContactFlagPortailFactures,
                IsSignatory = operation.IsSignatory
            });

            if (synchronizationResult.Outcome == ContactAkuiteoSynchronizationOutcome.Sent
                && string.IsNullOrWhiteSpace(synchronizationResult.AkuiteoContactId) == false)
            {
                await operationRepository.MarkSentAsync(operation.Id, synchronizationResult.AkuiteoContactId);
                return;
            }

            await operationRepository.MarkFailedAsync(
                operation.Id,
                synchronizationResult.Error ?? "Akuiteo contact synchronization failed.");
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Unexpected error while processing Akuiteo contact synchronization operation {OperationId}.",
                operation.Id);
            await operationRepository.MarkFailedAsync(operation.Id, exception.Message);
        }
    }
}

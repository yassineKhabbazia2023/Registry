// <copyright file="AkuiteoContactSyncOperationServiceTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Consts;
using Application.Enums;
using Application.Interfaces;
using Application.Models.Results;
using Application.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Pulse.Back.Events.IntegrationEvents;
using Pulse.Back.Events.IntegrationEvents.EventsData;
using Pulse.Registry.Domain.Entities.Audits;

namespace Registry.Application.Tests.Services;

public class AkuiteoContactSyncOperationServiceTests
{
    private readonly Mock<IAkuiteoContactSyncOperationRepository> operationRepositoryMock = new();
    private readonly Mock<IContactAkuiteoSynchronizer> contactAkuiteoSynchronizerMock = new();
    private readonly Mock<IFeatureFlagService> featureFlagServiceMock = new();
    private readonly Mock<ILogger<AkuiteoContactSyncOperationService>> loggerMock = new();

    [Fact]
    public async Task EnqueueAsync_WhenFeatureIsEnabled_ShouldPersistPendingOperation()
    {
        AkuiteoContactSyncOperationEntity? capturedOperation = null;
        var roleEvent = CreateRoleEvent();
        featureFlagServiceMock
            .Setup(service => service.IsEnabled(FeatureFlagKeys.IsContactAkuiteoSynchronizationEnabled))
            .Returns(true);
        operationRepositoryMock
            .Setup(repository => repository.AddIfNotExistsAsync(It.IsAny<AkuiteoContactSyncOperationEntity>()))
            .Callback<AkuiteoContactSyncOperationEntity>(operation => capturedOperation = operation)
            .ReturnsAsync(true);

        var queued = await CreateService().EnqueueAsync(roleEvent, roleEvent.EventId);

        Assert.True(queued);
        Assert.NotNull(capturedOperation);
        Assert.Equal(roleEvent.EventId, capturedOperation!.SourceEventId);
        Assert.Equal(roleEvent.Data.AccountId, capturedOperation.AccountId);
        Assert.Equal(roleEvent.Data.AccountNumber, capturedOperation.AccountNumber);
        Assert.Equal(roleEvent.AccountType, capturedOperation.AccountType);
        Assert.Equal(roleEvent.Data.ContactId, capturedOperation.ContactId);
        Assert.Equal(roleEvent.Data.ContactEmail, capturedOperation.ContactEmail);
        Assert.Equal(AkuiteoContactSyncReason.RoleCreatedFromPulse, capturedOperation.Reason);
        Assert.Equal(AkuiteoContactSyncOperationStatus.Pending, capturedOperation.Status);
    }

    [Fact]
    public async Task EnqueueAsync_WithRoleFromAkuiteo_ShouldPersistSkippedOperation()
    {
        AkuiteoContactSyncOperationEntity? capturedOperation = null;
        var roleEvent = CreateRoleEvent();
        featureFlagServiceMock
            .Setup(service => service.IsEnabled(FeatureFlagKeys.IsContactAkuiteoSynchronizationEnabled))
            .Returns(true);
        operationRepositoryMock
            .Setup(repository => repository.AddIfNotExistsAsync(It.IsAny<AkuiteoContactSyncOperationEntity>()))
            .Callback<AkuiteoContactSyncOperationEntity>(operation => capturedOperation = operation)
            .ReturnsAsync(true);

        var queued = await CreateService().EnqueueAsync(
            roleEvent,
            roleEvent.EventId,
            isRoleFromAkuiteo: true);

        Assert.True(queued);
        Assert.NotNull(capturedOperation);
        Assert.Equal(AkuiteoContactSyncReason.RoleFromAkuiteo, capturedOperation!.Reason);
        Assert.Equal(AkuiteoContactSyncOperationStatus.Skipped, capturedOperation.Status);
    }

    [Fact]
    public async Task EnqueueAsync_WhenFeatureIsDisabled_ShouldNotPersistOperation()
    {
        featureFlagServiceMock
            .Setup(service => service.IsEnabled(FeatureFlagKeys.IsContactAkuiteoSynchronizationEnabled))
            .Returns(false);

        var roleEvent = CreateRoleEvent();
        var queued = await CreateService().EnqueueAsync(roleEvent, roleEvent.EventId);

        Assert.False(queued);
        operationRepositoryMock.Verify(
            repository => repository.AddIfNotExistsAsync(It.IsAny<AkuiteoContactSyncOperationEntity>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessPendingOperationsAsync_WithSuccessfulSynchronization_ShouldMarkSent()
    {
        var operation = CreateOperation();
        SetupProcessing(operation);
        contactAkuiteoSynchronizerMock
            .Setup(synchronizer => synchronizer.SynchronizeAsync(
                It.IsAny<RoleCreatedEventData>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContactAkuiteoSynchronizationResult
            {
                Outcome = ContactAkuiteoSynchronizationOutcome.Sent,
                AkuiteoContactId = "500145940"
            });

        await CreateService().ProcessPendingOperationsAsync();

        contactAkuiteoSynchronizerMock.Verify(
            synchronizer => synchronizer.SynchronizeAsync(
                It.Is<RoleCreatedEventData>(role =>
                    role.AccountId == operation.AccountId
                    && role.AccountNumber == operation.AccountNumber
                    && role.ContactId == operation.ContactId
                    && role.ContactEmail == operation.ContactEmail),
                It.IsAny<CancellationToken>()),
            Times.Once);
        operationRepositoryMock.Verify(
            repository => repository.TryMarkProcessingAsync(operation.Id),
            Times.Once);
        operationRepositoryMock.Verify(
            repository => repository.MarkSentAsync(operation.Id, "500145940"),
            Times.Once);
        operationRepositoryMock.Verify(
            repository => repository.MarkFailedAsync(It.IsAny<long>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessPendingOperationsAsync_WithFailedSynchronization_ShouldMarkFailedOnce()
    {
        var operation = CreateOperation();
        SetupProcessing(operation);
        contactAkuiteoSynchronizerMock
            .Setup(synchronizer => synchronizer.SynchronizeAsync(
                It.IsAny<RoleCreatedEventData>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContactAkuiteoSynchronizationResult
            {
                Outcome = ContactAkuiteoSynchronizationOutcome.Failed,
                Error = "Akuiteo is unavailable."
            });

        await CreateService().ProcessPendingOperationsAsync();

        contactAkuiteoSynchronizerMock.Verify(
            synchronizer => synchronizer.SynchronizeAsync(
                It.IsAny<RoleCreatedEventData>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        operationRepositoryMock.Verify(
            repository => repository.MarkFailedAsync(operation.Id, "Akuiteo is unavailable."),
            Times.Once);
        operationRepositoryMock.Verify(
            repository => repository.MarkSentAsync(It.IsAny<long>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessPendingOperationsAsync_WhenSynchronizerThrows_ShouldMarkFailedAndContinue()
    {
        var operation = CreateOperation();
        SetupProcessing(operation);
        contactAkuiteoSynchronizerMock
            .Setup(synchronizer => synchronizer.SynchronizeAsync(
                It.IsAny<RoleCreatedEventData>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Unexpected failure."));

        await CreateService().ProcessPendingOperationsAsync();

        operationRepositoryMock.Verify(
            repository => repository.MarkFailedAsync(operation.Id, "Unexpected failure."),
            Times.Once);
        operationRepositoryMock.Verify(
            repository => repository.GetEligiblePendingAsync(),
            Times.Once);
    }

    [Fact]
    public async Task ProcessPendingOperationsAsync_WhenOperationWasAlreadyClaimed_ShouldSkipSynchronization()
    {
        var operation = CreateOperation();
        operationRepositoryMock
            .Setup(repository => repository.GetEligiblePendingAsync())
            .ReturnsAsync([operation]);
        operationRepositoryMock
            .Setup(repository => repository.TryMarkProcessingAsync(operation.Id))
            .ReturnsAsync(false);

        await CreateService().ProcessPendingOperationsAsync();

        contactAkuiteoSynchronizerMock.Verify(
            synchronizer => synchronizer.SynchronizeAsync(
                It.IsAny<RoleCreatedEventData>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private void SetupProcessing(AkuiteoContactSyncOperationEntity operation)
    {
        operationRepositoryMock
            .Setup(repository => repository.GetEligiblePendingAsync())
            .ReturnsAsync([operation]);
        operationRepositoryMock
            .Setup(repository => repository.TryMarkProcessingAsync(operation.Id))
            .ReturnsAsync(true);
    }

    private AkuiteoContactSyncOperationService CreateService()
    {
        return new AkuiteoContactSyncOperationService(
            operationRepositoryMock.Object,
            contactAkuiteoSynchronizerMock.Object,
            featureFlagServiceMock.Object,
            loggerMock.Object);
    }

    private static RoleCreatedEvent CreateRoleEvent()
    {
        return new RoleCreatedEvent(new RoleCreatedEventData
        {
            AccountId = 792480503,
            AccountNumber = "9010001710",
            ContactId = 123,
            ContactEmail = "contact@example.com",
            IsSignatory = true,
            ContactFlagPortailFactures = true
        })
        {
            AccountType = AccountTypes.Prospect
        };
    }

    private static AkuiteoContactSyncOperationEntity CreateOperation()
    {
        return new AkuiteoContactSyncOperationEntity
        {
            Id = 42,
            SourceEventId = Guid.NewGuid(),
            AccountId = 792480503,
            AccountNumber = "9010001710",
            AccountType = AccountTypes.Prospect,
            ContactId = 123,
            ContactEmail = "contact@example.com",
            ContactFlagPortailFactures = true,
            IsSignatory = true,
            Reason = AkuiteoContactSyncReason.RoleCreatedFromPulse,
            Status = AkuiteoContactSyncOperationStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}

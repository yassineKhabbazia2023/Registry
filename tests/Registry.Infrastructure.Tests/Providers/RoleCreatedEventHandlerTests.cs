// <copyright file="RoleCreatedEventHandlerTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Consts;
using Application.Enums;
using Application.Interfaces;
using Application.Providers;
using Application.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Pulse.Back.Events.IntegrationEvents;
using Pulse.Back.Events.IntegrationEvents.EventsData;
using Pulse.Registry.Domain.Entities;
using Pulse.Registry.Domain.Entities.Accounts;

namespace Registry.Infrastructure.Tests.Providers;

public class RoleCreatedEventHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithValidMessage_ShouldQueueAkuiteoOperationAndPersistRole()
    {
        var roleRepositoryMock = new Mock<IRoleRepository>();
        var operationRepositoryMock = new Mock<IOperationRepository>();
        var syncOperationServiceMock = new Mock<IAkuiteoContactSyncOperationService>();
        var roleEvent = CreateRoleEvent();
        roleRepositoryMock
            .Setup(repository => repository.GetPulseRole(
                roleEvent.Data.ContactEmail,
                roleEvent.Data.AccountNumber))
            .ReturnsAsync((RoleEntity?)null);
        roleRepositoryMock
            .Setup(repository => repository.HasInsertRefRoleAsync(
                roleEvent.Data.AccountNumber,
                roleEvent.Data.ContactEmail))
            .ReturnsAsync(false);
        roleRepositoryMock
            .Setup(repository => repository.AddRoleAsync(It.IsAny<RoleEntity>()))
            .Returns(Task.CompletedTask);
        operationRepositoryMock
            .Setup(repository => repository.FetchOperationsByCriteriaAsync(
                It.IsAny<OperationSearchCriteria>(),
                OperationStrategyType.ROLE,
                It.IsAny<string>(),
                It.IsAny<bool?>(),
                It.IsAny<string?>()))
            .ReturnsAsync([]);
        syncOperationServiceMock
            .Setup(service => service.EnqueueAsync(
                It.IsAny<RoleCreatedEvent>(),
                It.IsAny<Guid>(),
                It.IsAny<bool>()))
            .ReturnsAsync(true);

        await CreateHandler(
            roleRepositoryMock,
            operationRepositoryMock,
            syncOperationServiceMock)
            .HandleAsync(JsonConvert.SerializeObject(roleEvent));

        syncOperationServiceMock.Verify(
            service => service.EnqueueAsync(
                It.Is<RoleCreatedEvent>(queuedEvent =>
                    queuedEvent.Data.AccountId == roleEvent.Data.AccountId
                    && queuedEvent.Data.ContactId == roleEvent.Data.ContactId),
                roleEvent.EventId,
                false),
            Times.Once);
        roleRepositoryMock.Verify(
            repository => repository.AddRoleAsync(It.Is<RoleEntity>(role =>
                role.AccountId == roleEvent.Data.AccountId
                && role.ContactId == roleEvent.Data.ContactId
                && role.AccountNumber == roleEvent.Data.AccountNumber
                && role.ContactEmail == roleEvent.Data.ContactEmail)),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithExistingRoleAndChangedFlags_ShouldQueueAndUpdateRole()
    {
        var roleRepositoryMock = new Mock<IRoleRepository>();
        var operationRepositoryMock = new Mock<IOperationRepository>();
        var syncOperationServiceMock = new Mock<IAkuiteoContactSyncOperationService>();
        var roleEvent = CreateRoleEvent();
        var existingRole = new RoleEntity
        {
            AccountId = roleEvent.Data.AccountId,
            ContactId = roleEvent.Data.ContactId,
            AccountNumber = roleEvent.Data.AccountNumber,
            ContactEmail = roleEvent.Data.ContactEmail,
            ContactFlagPortailFactures = false,
            ContactFlagMainContact = false
        };
        roleRepositoryMock
            .Setup(repository => repository.GetPulseRole(
                roleEvent.Data.ContactEmail,
                roleEvent.Data.AccountNumber))
            .ReturnsAsync(existingRole);
        roleRepositoryMock
            .Setup(repository => repository.HasInsertRefRoleAsync(
                roleEvent.Data.AccountNumber,
                roleEvent.Data.ContactEmail))
            .ReturnsAsync(false);
        roleRepositoryMock
            .Setup(repository => repository.UpdatePulseRole(existingRole))
            .ReturnsAsync(true);
        operationRepositoryMock
            .Setup(repository => repository.FetchOperationsByCriteriaAsync(
                It.IsAny<OperationSearchCriteria>(),
                OperationStrategyType.ROLE,
                It.IsAny<string>(),
                It.IsAny<bool?>(),
                It.IsAny<string?>()))
            .ReturnsAsync([]);
        syncOperationServiceMock
            .Setup(service => service.EnqueueAsync(
                It.IsAny<RoleCreatedEvent>(),
                It.IsAny<Guid>(),
                It.IsAny<bool>()))
            .ReturnsAsync(true);

        await CreateHandler(
            roleRepositoryMock,
            operationRepositoryMock,
            syncOperationServiceMock)
            .HandleAsync(JsonConvert.SerializeObject(roleEvent));

        syncOperationServiceMock.Verify(
            service => service.EnqueueAsync(It.IsAny<RoleCreatedEvent>(), roleEvent.EventId, false),
            Times.Once);
        roleRepositoryMock.Verify(repository => repository.UpdatePulseRole(existingRole), Times.Once);
        roleRepositoryMock.Verify(
            repository => repository.AddRoleAsync(It.IsAny<RoleEntity>()),
            Times.Never);
        Assert.Equal(roleEvent.Data.ContactFlagPortailFactures, existingRole.ContactFlagPortailFactures);
        Assert.Equal(roleEvent.Data.IsSignatory, existingRole.ContactFlagMainContact);
    }

    [Fact]
    public async Task HandleAsync_WithEquivalentInsertRefRole_ShouldRecordSkippedAkuiteoOperationAndPersistRole()
    {
        var roleRepositoryMock = new Mock<IRoleRepository>();
        var operationRepositoryMock = new Mock<IOperationRepository>();
        var syncOperationServiceMock = new Mock<IAkuiteoContactSyncOperationService>();
        var roleEvent = CreateRoleEvent();
        roleRepositoryMock
            .Setup(repository => repository.HasInsertRefRoleAsync(
                roleEvent.Data.AccountNumber,
                roleEvent.Data.ContactEmail))
            .ReturnsAsync(true);
        roleRepositoryMock
            .Setup(repository => repository.GetPulseRole(
                roleEvent.Data.ContactEmail,
                roleEvent.Data.AccountNumber))
            .ReturnsAsync((RoleEntity?)null);
        operationRepositoryMock
            .Setup(repository => repository.FetchOperationsByCriteriaAsync(
                It.IsAny<OperationSearchCriteria>(),
                OperationStrategyType.ROLE,
                It.IsAny<string>(),
                It.IsAny<bool?>(),
                It.IsAny<string?>()))
            .ReturnsAsync([]);
        syncOperationServiceMock
            .Setup(service => service.EnqueueAsync(
                It.IsAny<RoleCreatedEvent>(),
                roleEvent.EventId,
                true))
            .ReturnsAsync(true);

        await CreateHandler(
            roleRepositoryMock,
            operationRepositoryMock,
            syncOperationServiceMock)
            .HandleAsync(JsonConvert.SerializeObject(roleEvent));

        syncOperationServiceMock.Verify(
            service => service.EnqueueAsync(
                It.Is<RoleCreatedEvent>(receivedEvent =>
                    receivedEvent.Data.AccountId == roleEvent.Data.AccountId
                    && receivedEvent.Data.ContactId == roleEvent.Data.ContactId),
                roleEvent.EventId,
                true),
            Times.Once);
        roleRepositoryMock.Verify(
            repository => repository.AddRoleAsync(It.IsAny<RoleEntity>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenRolePersistenceFails_ShouldPropagateExceptionForServiceBusRetry()
    {
        var roleRepositoryMock = new Mock<IRoleRepository>();
        var operationRepositoryMock = new Mock<IOperationRepository>();
        var syncOperationServiceMock = new Mock<IAkuiteoContactSyncOperationService>();
        var roleEvent = CreateRoleEvent();
        var expectedException = new DbUpdateException("Role dependency is not available.");
        roleRepositoryMock
            .Setup(repository => repository.HasInsertRefRoleAsync(
                roleEvent.Data.AccountNumber,
                roleEvent.Data.ContactEmail))
            .ReturnsAsync(false);
        roleRepositoryMock
            .Setup(repository => repository.GetPulseRole(
                roleEvent.Data.ContactEmail,
                roleEvent.Data.AccountNumber))
            .ReturnsAsync((RoleEntity?)null);
        roleRepositoryMock
            .Setup(repository => repository.AddRoleAsync(It.IsAny<RoleEntity>()))
            .ThrowsAsync(expectedException);
        syncOperationServiceMock
            .Setup(service => service.EnqueueAsync(
                It.IsAny<RoleCreatedEvent>(),
                roleEvent.EventId,
                false))
            .ReturnsAsync(true);

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => CreateHandler(
            roleRepositoryMock,
            operationRepositoryMock,
            syncOperationServiceMock)
            .HandleAsync(JsonConvert.SerializeObject(roleEvent)));

        Assert.Same(expectedException, exception);
        operationRepositoryMock.Verify(
            repository => repository.FetchOperationsByCriteriaAsync(
                It.IsAny<OperationSearchCriteria>(),
                It.IsAny<OperationStrategyType>(),
                It.IsAny<string>(),
                It.IsAny<bool?>(),
                It.IsAny<string?>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenAkuiteoAuditFails_ShouldContinueExistingRoleProcessing()
    {
        var roleRepositoryMock = new Mock<IRoleRepository>();
        var operationRepositoryMock = new Mock<IOperationRepository>();
        var syncOperationServiceMock = new Mock<IAkuiteoContactSyncOperationService>();
        var roleEvent = CreateRoleEvent();
        roleRepositoryMock
            .Setup(repository => repository.HasInsertRefRoleAsync(
                roleEvent.Data.AccountNumber,
                roleEvent.Data.ContactEmail))
            .ReturnsAsync(false);
        roleRepositoryMock
            .Setup(repository => repository.GetPulseRole(
                roleEvent.Data.ContactEmail,
                roleEvent.Data.AccountNumber))
            .ReturnsAsync((RoleEntity?)null);
        operationRepositoryMock
            .Setup(repository => repository.FetchOperationsByCriteriaAsync(
                It.IsAny<OperationSearchCriteria>(),
                OperationStrategyType.ROLE,
                It.IsAny<string>(),
                It.IsAny<bool?>(),
                It.IsAny<string?>()))
            .ReturnsAsync([]);
        syncOperationServiceMock
            .Setup(service => service.EnqueueAsync(
                It.IsAny<RoleCreatedEvent>(),
                roleEvent.EventId,
                false))
            .ThrowsAsync(new InvalidOperationException("Audit is unavailable."));

        await CreateHandler(
            roleRepositoryMock,
            operationRepositoryMock,
            syncOperationServiceMock)
            .HandleAsync(JsonConvert.SerializeObject(roleEvent));

        roleRepositoryMock.Verify(
            repository => repository.AddRoleAsync(It.IsAny<RoleEntity>()),
            Times.Once);
        operationRepositoryMock.Verify(
            repository => repository.FetchOperationsByCriteriaAsync(
                It.IsAny<OperationSearchCriteria>(),
                OperationStrategyType.ROLE,
                roleEvent.Data.ContactEmail,
                It.IsAny<bool?>(),
                roleEvent.Data.AccountNumber),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithNullMessage_ShouldNotQueueOrPersistRole()
    {
        var roleRepositoryMock = new Mock<IRoleRepository>();
        var syncOperationServiceMock = new Mock<IAkuiteoContactSyncOperationService>();

        await CreateHandler(
            roleRepositoryMock,
            new Mock<IOperationRepository>(),
            syncOperationServiceMock)
            .HandleAsync(null!);

        syncOperationServiceMock.Verify(
            service => service.EnqueueAsync(
                It.IsAny<RoleCreatedEvent>(),
                It.IsAny<Guid>(),
                It.IsAny<bool>()),
            Times.Never);
        roleRepositoryMock.Verify(
            repository => repository.AddRoleAsync(It.IsAny<RoleEntity>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidData_ShouldNotQueueOrPersistRole()
    {
        var roleRepositoryMock = new Mock<IRoleRepository>();
        var syncOperationServiceMock = new Mock<IAkuiteoContactSyncOperationService>();
        var roleEvent = CreateRoleEvent();
        roleEvent.Data.ContactId = 0;

        await CreateHandler(
            roleRepositoryMock,
            new Mock<IOperationRepository>(),
            syncOperationServiceMock)
            .HandleAsync(JsonConvert.SerializeObject(roleEvent));

        syncOperationServiceMock.Verify(
            service => service.EnqueueAsync(
                It.IsAny<RoleCreatedEvent>(),
                It.IsAny<Guid>(),
                It.IsAny<bool>()),
            Times.Never);
        roleRepositoryMock.Verify(
            repository => repository.AddRoleAsync(It.IsAny<RoleEntity>()),
            Times.Never);
    }

    private static RoleCreatedEventHandler CreateHandler(
        Mock<IRoleRepository> roleRepositoryMock,
        Mock<IOperationRepository> operationRepositoryMock,
        Mock<IAkuiteoContactSyncOperationService> syncOperationServiceMock)
    {
        return new RoleCreatedEventHandler(
            Mock.Of<ILogger<RoleCreatedEventHandler>>(),
            Mock.Of<IRoleRegistryProvider>(),
            roleRepositoryMock.Object,
            operationRepositoryMock.Object,
            syncOperationServiceMock.Object);
    }

    private static RoleCreatedEvent CreateRoleEvent()
    {
        return new RoleCreatedEvent(new RoleCreatedEventData
        {
            AccountId = 792480503,
            AccountGlobalUniqueId = Guid.NewGuid(),
            AccountNumber = "9010001710",
            ContactId = 123,
            ContactGlobalUniqueId = Guid.NewGuid(),
            ContactEmail = "contact@example.com",
            ContactFlagPortailFactures = true,
            IsSignatory = true,
            IsFavorite = false,
            IsDelegation = false
        })
        {
            AccountType = AccountTypes.Prospect
        };
    }
}

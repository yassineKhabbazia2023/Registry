using Application.Consts;
using Application.Interfaces;
using Application.Options;
using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Pulse.Back.Events.Abstractions;
using Pulse.Back.Events.IntegrationEvents;
using Pulse.Registry.Domain.Context;
using Pulse.Registry.Domain.Entities;
using Registry.Application.Consts;
using Infrastructure.Orchestrators;
using Application.Models;
using Registry.Infrastructure.Managers;
using Application.Enums;
using Pulse.Registry.Domain.Entities.Accounts;
using Pulse.Registry.Domain.Entities.Contacts;

namespace Registry.Infrastructure.Tests.Orchestrators;

public class RoleOrchestratorTests
{
    // Arrange: Create unique in-memory DbContextOptions for RefContext.
    private DbContextOptions<RefContext> CreateInMemoryOptions(string databaseName)
    {
        return new DbContextOptionsBuilder<RefContext>()
            .UseInMemoryDatabase($"{databaseName}_{Guid.NewGuid()}")
            .Options;
    }

    // Helper: Create an instance of RoleOrchestrator with mocked dependencies.
    private RoleOrchestrator CreateOrchestrator(
        RefContext context,
        IOperationService operationService,
        INotificationManager notificationManager,
        IServiceBusMessageFactory messageFactory,
        IOptions<BackGroundJobOptions> bgJobOptions)
    {
        var loggerMock = new Mock<ILogger<RoleOrchestrator>>();
        return new RoleOrchestrator(
            loggerMock.Object,
            context,
            notificationManager,
            bgJobOptions,
            messageFactory,
            operationService);
    }


    #region ProcessRolePublishAsync Tests

    [Fact]
    public async Task ProcessRolePublishAsync_InsertBranch_Should_CreateCreatedEventAndUpdateStatus()
    {
        // Arrange
        var options = CreateInMemoryOptions(nameof(ProcessRolePublishAsync_InsertBranch_Should_CreateCreatedEventAndUpdateStatus));
        using var context = new RefContext(options);

        // Seed an INSERT operation for roles.
        var opEntity = new RegOperationEntity
        {
            Id = 1,
            Operation = OperationAction.Insert,
            CreationDate = DateTime.UtcNow,
            EntityId = Guid.NewGuid(),
            LastStatusApprovalDate = DateTime.UtcNow,
            LastStatusApprovalBy = "insert@test.com",
            PublishedAt = null,
            ApprovalStatus = ApprovalStatus.Approved,
            Type = OperationCategory.ROLE,
            ProcessStatus = ProcessStatus.Ready
        };
        context.RegOperationEntity.Add(opEntity);

        // Seed a corresponding RefRoleEntity.
        var refRole = new RefRoleEntity
        {
            EntityId = opEntity.EntityId,
            AccountNumber = "ROLE001",
            ContactEmail = "rolecreated@test.com",
            Description = "CLP",
            OperationType = OperationAction.Insert,
            SubRole = "executive",
            ContactFlagPortailFactures = true,
        };
        context.RefRoleEntity.Add(refRole);

        // Seed related AccountEntity and ContactEntity for lookup in CreateRegistryRoleEvent.
        context.AccountEntity.Add(new AccountEntity
        {
            AccountId = 1,
            AccountNumber = "ROLE001",
            AccountGlobalUniqueId = Guid.NewGuid(),
            LegalName = "legal",
        });
        context.ContactEntity.Add(new ContactEntity
        {
            ContactId = 1,
            Email = "rolecreated@test.com",
            FirstName = "Role",
            LastName = "Created",
            Type = "User",
            ContactGlobalUniqueId = Guid.NewGuid()
        });
        await context.SaveChangesAsync();

        var bgJobOptions = Microsoft.Extensions.Options.Options.Create(new BackGroundJobOptions { Chunk = 1 });

        // Setup the IOperationService mock.
        var opServiceMock = new Mock<IOperationService>();
        var sequence = new MockSequence();
        opServiceMock.InSequence(sequence).Setup(s => s.GetRoleOperationRecordsAsync(
                It.Is<string>(op => op == OperationAction.Insert),
                It.IsAny<int>(),
                false))
            .ReturnsAsync(() => new List<RoleOperationRecord>
            {
                    new RoleOperationRecord { Operation = opEntity, Role = refRole }
            });

        opServiceMock.Setup(s => s.UpdateOperationStatusListASync(ProcessStatus.Sent, It.IsAny<List<RegOperationEntity>>()))
            .Returns(Task.CompletedTask);
        opServiceMock.Setup(s => s.TryToProceedUntilTimeoutAsync(OperationCategory.ROLE, OperationAction.Insert))
            .Returns(Task.CompletedTask);

        // Setup the INotificationManager mock.
        var notificationManagerMock = new Mock<INotificationManager>();
        notificationManagerMock.Setup(nm => nm.BulkPublishAsync(It.IsAny<List<ServiceBusMessage>>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

            // Setup the IServiceBusMessageFactory mock with a callback to verify event data.
            var dummyMessage = new ServiceBusMessage("dummy");
            var messageFactoryMock = new Mock<IServiceBusMessageFactory>();
            messageFactoryMock.Setup(mf => mf.CreateMessage(
                    It.Is<RegistryRoleCreatedEvent>(e =>
                        e.Data.AccountNumber == refRole.AccountNumber &&
                        e.Data.Email == refRole.ContactEmail &&
                        e.Data.IsCustomerRelation == true &&
                        e.Data.Description == "CLP" &&
                        e.Data.ContactFlagPortailFactures == refRole.ContactFlagPortailFactures),
                    It.IsAny<string>()))
                .Returns(dummyMessage);

        var orchestrator = CreateOrchestrator(context, opServiceMock.Object, notificationManagerMock.Object, messageFactoryMock.Object, bgJobOptions);

        // Act
        await orchestrator.ProcessRolePublishAsync(OperationAction.Insert);

        // Assert
        messageFactoryMock.Verify(mf => mf.CreateMessage(
            It.IsAny<RegistryRoleCreatedEvent>(), It.IsAny<string>()), Times.AtLeastOnce);
        opServiceMock.Verify(s => s.GetRoleOperationRecordsAsync(OperationAction.Insert, bgJobOptions.Value.Chunk, It.IsAny<bool?>()), Times.AtLeastOnce);
        opServiceMock.Verify(s => s.UpdateOperationStatusListASync(ProcessStatus.Sent, It.IsAny<List<RegOperationEntity>>()), Times.Once);
        opServiceMock.Verify(s => s.TryToProceedUntilTimeoutAsync(OperationCategory.ROLE, OperationAction.Insert), Times.Once);
        notificationManagerMock.Verify(nm => nm.BulkPublishAsync(It.IsAny<List<ServiceBusMessage>>(), It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessRolePublishAsync_DeleteBranch_Should_CreateRemovedEventAndUpdateStatus()
    {
        // Arrange
        var options = CreateInMemoryOptions(nameof(ProcessRolePublishAsync_DeleteBranch_Should_CreateRemovedEventAndUpdateStatus));
        using var context = new RefContext(options);

        // Seed a DELETE operation for roles.
        var opEntity = new RegOperationEntity
        {
            Id = 2,
            Operation = OperationAction.Delete,
            CreationDate = DateTime.UtcNow,
            EntityId = Guid.NewGuid(),
            LastStatusApprovalDate = DateTime.UtcNow,
            LastStatusApprovalBy = "delete@test.com",
            PublishedAt = null,
            ApprovalStatus = ApprovalStatus.Approved,
            Type = OperationCategory.ROLE,
            ProcessStatus = ProcessStatus.Ready
        };
        context.RegOperationEntity.Add(opEntity);

        // Seed a corresponding RefRoleEntity.
        var refRole = new RefRoleEntity
        {
            EntityId = opEntity.EntityId,
            AccountNumber = "ROLE002",
            ContactEmail = "roleremoved@test.com",
            OperationType = OperationAction.Delete
        };
        context.RefRoleEntity.Add(refRole);

        // Seed matching AccountEntity and ContactEntity.
        context.AccountEntity.Add(new AccountEntity
        {
            AccountId = 2,
            AccountNumber = "ROLE002",
            AccountGlobalUniqueId = Guid.NewGuid(),
            LegalName = "legal",
        });
        context.ContactEntity.Add(new ContactEntity
        {
            ContactId = 2,
            Email = "roleremoved@test.com",
            FirstName = "Role",
            LastName = "Removed",
            Type = "User",
            ContactGlobalUniqueId = Guid.NewGuid()
        });
        await context.SaveChangesAsync();

        var bgJobOptions = Microsoft.Extensions.Options.Options.Create(new BackGroundJobOptions { Chunk = 1 });

        // Setup the IOperationService mock.
        var opServiceMock = new Mock<IOperationService>();
        var sequence = new MockSequence();
        opServiceMock.InSequence(sequence).Setup(s => s.GetRoleOperationRecordsAsync(
                It.Is<string>(op => op == OperationAction.Delete),
                It.IsAny<int>(),
                It.IsAny<bool?>()))
            .ReturnsAsync(() => new List<RoleOperationRecord>
            {
                    new RoleOperationRecord { Operation = opEntity, Role = refRole }
            });

        opServiceMock.Setup(s => s.UpdateOperationStatusListASync(ProcessStatus.Sent, It.IsAny<List<RegOperationEntity>>()))
            .Returns(Task.CompletedTask);
        opServiceMock.Setup(s => s.TryToProceedUntilTimeoutAsync(OperationCategory.ROLE, OperationAction.Delete))
            .Returns(Task.CompletedTask);

        // Setup the INotificationManager mock.
        var notificationManagerMock = new Mock<INotificationManager>();
        notificationManagerMock.Setup(nm => nm.BulkPublishAsync(It.IsAny<List<ServiceBusMessage>>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Setup the IServiceBusMessageFactory mock with a callback to verify event data.
        var dummyMessage = new ServiceBusMessage("dummy");
        var messageFactoryMock = new Mock<IServiceBusMessageFactory>();
        messageFactoryMock.Setup(mf => mf.CreateMessage(
                It.Is<RegistryRoleRemovedEvent>(e =>
                    e.Data.AccountNumber == refRole.AccountNumber &&
                    e.Data.Email == refRole.ContactEmail),
                It.IsAny<string>()))
            .Returns(dummyMessage);

        var orchestrator = CreateOrchestrator(context, opServiceMock.Object, notificationManagerMock.Object, messageFactoryMock.Object, bgJobOptions);

        // Act
        await orchestrator.ProcessRolePublishAsync(OperationAction.Delete);

        // Assert
        messageFactoryMock.Verify(mf => mf.CreateMessage(
            It.IsAny<RegistryRoleRemovedEvent>(), It.IsAny<string>()), Times.Exactly(1));
        opServiceMock.Verify(s => s.GetRoleOperationRecordsAsync(OperationAction.Delete, bgJobOptions.Value.Chunk, It.IsAny<bool?>()), Times.AtLeastOnce);
        opServiceMock.Verify(s => s.UpdateOperationStatusListASync(ProcessStatus.Sent, It.IsAny<List<RegOperationEntity>>()), Times.Once);
        opServiceMock.Verify(s => s.TryToProceedUntilTimeoutAsync(OperationCategory.ROLE, OperationAction.Delete), Times.Once);
        notificationManagerMock.Verify(nm => nm.BulkPublishAsync(It.IsAny<List<ServiceBusMessage>>(), It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessRolePublishAsync_NoBatchFound_Should_CallTimeoutOnly()
    {
        // Arrange
        var options = CreateInMemoryOptions(nameof(ProcessRolePublishAsync_NoBatchFound_Should_CallTimeoutOnly));
        using var context = new RefContext(options);

        var bgJobOptions = Microsoft.Extensions.Options.Options.Create(new BackGroundJobOptions { Chunk = 1 });
        var opServiceMock = new Mock<IOperationService>();
        opServiceMock.Setup(s => s.GetRoleOperationRecordsAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool?>()))
            .ReturnsAsync(new List<RoleOperationRecord>());
        opServiceMock.Setup(s => s.TryToProceedUntilTimeoutAsync(OperationCategory.ROLE, It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var notificationManagerMock = new Mock<INotificationManager>();
        var messageFactoryMock = new Mock<IServiceBusMessageFactory>();

        var orchestrator = CreateOrchestrator(context, opServiceMock.Object, notificationManagerMock.Object, messageFactoryMock.Object, bgJobOptions);

        // Act
        await orchestrator.ProcessRolePublishAsync(OperationAction.Insert);

        // Assert
        opServiceMock.Verify(s => s.TryToProceedUntilTimeoutAsync(OperationCategory.ROLE, OperationAction.Insert), Times.Once);
    }

    [Fact]
    public async Task ProcessRolePublishAsync_DeleteBranch_WithPennylaneFlagTrue_PublishesOnlyPennylaneRoles()
    {
        // Arrange
        var options = CreateInMemoryOptions(nameof(ProcessRolePublishAsync_DeleteBranch_WithPennylaneFlagTrue_PublishesOnlyPennylaneRoles));
        using var context = new RefContext(options);

        var opEntity = new RegOperationEntity
        {
            Id = 42,
            Operation = OperationAction.Delete,
            CreationDate = DateTime.UtcNow,
            EntityId = Guid.NewGuid(),
            LastStatusApprovalDate = DateTime.UtcNow,
            LastStatusApprovalBy = "test@pennylane",
            PublishedAt = null,
            ApprovalStatus = ApprovalStatus.Approved,
            Type = OperationCategory.ROLE,
            ProcessStatus = ProcessStatus.Ready
        };
        context.RegOperationEntity.Add(opEntity);

        var pennylaneRole = new RefRoleEntity
        {
            EntityId = opEntity.EntityId,
            AccountNumber = "ACC1",
            ContactEmail = "user@pennylane",
            OperationType = OperationAction.Delete,
            RoleSource = DataSources.PENNYLANE.ToString()
        };
        var systemRole = new RefRoleEntity
        {
            EntityId = Guid.NewGuid(),
            AccountNumber = "ACC2",
            ContactEmail = "user@system",
            OperationType = OperationAction.Delete,
            RoleSource = "SYSTEM"
        };
        context.RefRoleEntity.AddRange(pennylaneRole, systemRole);

        context.AccountEntity.Add(new AccountEntity { AccountId = 100, AccountNumber = "ACC1", AccountGlobalUniqueId = Guid.NewGuid(), LegalName = "legal" });
        context.AccountEntity.Add(new AccountEntity { AccountId = 200, AccountNumber = "ACC2", AccountGlobalUniqueId = Guid.NewGuid(), LegalName = "legal" });
        context.ContactEntity.Add(new ContactEntity
        {
            ContactId = 10,
            Email = "user@pennylane",
            ContactGlobalUniqueId = Guid.NewGuid(),
            FirstName = "Pennylane",
            LastName = "User",
            Type = "Customer"
        });
        context.ContactEntity.Add(new ContactEntity
        {
            ContactId = 20,
            Email = "user@system",
            ContactGlobalUniqueId = Guid.NewGuid(),
            FirstName = "System",
            LastName = "User",
            Type = "Customer"
        });

        await context.SaveChangesAsync();

        var bgJobOptions = Microsoft.Extensions.Options.Options.Create(new BackGroundJobOptions { Chunk = 1 });

        var opServiceMock = new Mock<IOperationService>();

        opServiceMock.SetupSequence(s =>
            s.GetRoleOperationRecordsAsync(OperationAction.Delete, bgJobOptions.Value.Chunk, false))
            .ReturnsAsync(new List<RoleOperationRecord>
            {
                    new RoleOperationRecord { Operation = opEntity, Role = pennylaneRole, RoleCount = 1 },
                    new RoleOperationRecord { Operation = opEntity, Role = systemRole, RoleCount = 1 }
            })
            .ReturnsAsync(new List<RoleOperationRecord>());

        opServiceMock
            .Setup(s => s.UpdateOperationStatusListASync(ProcessStatus.Sent, It.IsAny<List<RegOperationEntity>>()))
            .Returns(Task.CompletedTask);

        opServiceMock.Setup(s => s.TryToProceedUntilTimeoutAsync(OperationCategory.ROLE, OperationAction.Delete))
            .Returns(Task.CompletedTask);

        var notificationManagerMock = new Mock<INotificationManager>(MockBehavior.Strict);
        notificationManagerMock
            .Setup(nm => nm.BulkPublishAsync(It.IsAny<List<ServiceBusMessage>>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var messageFactoryMock = new Mock<IServiceBusMessageFactory>();
        messageFactoryMock
            .Setup(mf => mf.CreateMessage(It.IsAny<RegistryRoleRemovedEvent>(), It.IsAny<string>()))
            .Returns(new ServiceBusMessage("dummy"))
            .Verifiable();

        var orchestrator = CreateOrchestrator(
            context,
            opServiceMock.Object,
            notificationManagerMock.Object,
            messageFactoryMock.Object,
            bgJobOptions);

        // Act
        await orchestrator.ProcessRolePublishAsync(OperationAction.Delete, true);

        // Assert
        messageFactoryMock.Verify(mf => mf.CreateMessage(
            It.Is<RegistryRoleRemovedEvent>(e => e.Data.AccountNumber == "ACC1"),
            It.IsAny<string>()), Times.Once);
        messageFactoryMock.Verify(mf => mf.CreateMessage(
            It.Is<RegistryRoleRemovedEvent>(e => e.Data.AccountNumber == "ACC2"),
            It.IsAny<string>()), Times.Never);
        notificationManagerMock.Verify(nm => nm.BulkPublishAsync(It.IsAny<List<ServiceBusMessage>>(), It.IsAny<string>()), Times.Once);
        opServiceMock.VerifyAll();
    }
    [Fact]
    public async Task ProcessRolePublishAsync_InsertBranch_WithNonCLPOrAMDescription_Should_SetIsCustomerRelationFalse()
    {
        // Arrange
        var options = CreateInMemoryOptions(nameof(ProcessRolePublishAsync_InsertBranch_WithNonCLPOrAMDescription_Should_SetIsCustomerRelationFalse));
        using var context = new RefContext(options);

        var opEntity = new RegOperationEntity
        {
            Id = 20,
            Operation = OperationAction.Insert,
            CreationDate = DateTime.UtcNow,
            EntityId = Guid.NewGuid(),
            LastStatusApprovalDate = DateTime.UtcNow,
            LastStatusApprovalBy = "other@test.com",
            PublishedAt = null,
            ApprovalStatus = ApprovalStatus.Approved,
            Type = OperationCategory.ROLE,
            ProcessStatus = ProcessStatus.Ready
        };
        context.RegOperationEntity.Add(opEntity);

        var refRole = new RefRoleEntity
        {
            EntityId = opEntity.EntityId,
            AccountNumber = "ROLE020",
            ContactEmail = "other-role@test.com",
            Description = "OTHER",
            OperationType = OperationAction.Insert,
            ContactFlagPortailFactures = false
        };
        context.RefRoleEntity.Add(refRole);

        context.AccountEntity.Add(new AccountEntity
        {
            AccountId = 20,
            AccountNumber = "ROLE020",
            AccountGlobalUniqueId = Guid.NewGuid(),
            LegalName = "legal",
        });
        context.ContactEntity.Add(new ContactEntity
        {
            ContactId = 20,
            Email = "other-role@test.com",
            FirstName = "Other",
            LastName = "Role",
            Type = "User",
            ContactGlobalUniqueId = Guid.NewGuid()
        });
        await context.SaveChangesAsync();

        var bgJobOptions = Microsoft.Extensions.Options.Options.Create(new BackGroundJobOptions { Chunk = 1 });

        var opServiceMock = new Mock<IOperationService>();
        var sequence = new MockSequence();
        opServiceMock.InSequence(sequence).Setup(s => s.GetRoleOperationRecordsAsync(
                It.Is<string>(op => op == OperationAction.Insert),
                It.IsAny<int>(),
                false))
            .ReturnsAsync(() => new List<RoleOperationRecord>
            {
                new RoleOperationRecord { Operation = opEntity, Role = refRole }
            });

        opServiceMock.Setup(s => s.UpdateOperationStatusListASync(ProcessStatus.Sent, It.IsAny<List<RegOperationEntity>>()))
            .Returns(Task.CompletedTask);
        opServiceMock.Setup(s => s.TryToProceedUntilTimeoutAsync(OperationCategory.ROLE, OperationAction.Insert))
            .Returns(Task.CompletedTask);

        var notificationManagerMock = new Mock<INotificationManager>();
        notificationManagerMock.Setup(nm => nm.BulkPublishAsync(It.IsAny<List<ServiceBusMessage>>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var dummyMessage = new ServiceBusMessage("dummy");
        var messageFactoryMock = new Mock<IServiceBusMessageFactory>();
        messageFactoryMock.Setup(mf => mf.CreateMessage(
                It.Is<RegistryRoleCreatedEvent>(e =>
                    e.Data.AccountNumber == refRole.AccountNumber &&
                    e.Data.Email == refRole.ContactEmail &&
                    e.Data.IsCustomerRelation == false &&
                    e.Data.Description == "OTHER"),
                It.IsAny<string>()))
            .Returns(dummyMessage);

        var orchestrator = CreateOrchestrator(context, opServiceMock.Object, notificationManagerMock.Object, messageFactoryMock.Object, bgJobOptions);

        // Act
        await orchestrator.ProcessRolePublishAsync(OperationAction.Insert);

        // Assert
        messageFactoryMock.Verify(mf => mf.CreateMessage(
            It.Is<RegistryRoleCreatedEvent>(e =>
                e.Data.IsCustomerRelation == false &&
                e.Data.Description == "OTHER"),
            It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessRolePublishAsync_InsertBranch_WithAMDescription_Should_SetIsCustomerRelationTrue()
    {
        // Arrange
        var options = CreateInMemoryOptions(nameof(ProcessRolePublishAsync_InsertBranch_WithAMDescription_Should_SetIsCustomerRelationTrue));
        using var context = new RefContext(options);

        var opEntity = new RegOperationEntity
        {
            Id = 10,
            Operation = OperationAction.Insert,
            CreationDate = DateTime.UtcNow,
            EntityId = Guid.NewGuid(),
            LastStatusApprovalDate = DateTime.UtcNow,
            LastStatusApprovalBy = "am@test.com",
            PublishedAt = null,
            ApprovalStatus = ApprovalStatus.Approved,
            Type = OperationCategory.ROLE,
            ProcessStatus = ProcessStatus.Ready
        };
        context.RegOperationEntity.Add(opEntity);

        var refRole = new RefRoleEntity
        {
            EntityId = opEntity.EntityId,
            AccountNumber = "ROLE010",
            ContactEmail = "am-role@test.com",
            Description = "AM",
            OperationType = OperationAction.Insert,
            ContactFlagPortailFactures = false
        };
        context.RefRoleEntity.Add(refRole);

        context.AccountEntity.Add(new AccountEntity
        {
            AccountId = 10,
            AccountNumber = "ROLE010",
            AccountGlobalUniqueId = Guid.NewGuid(),
            LegalName = "legal",
        });
        context.ContactEntity.Add(new ContactEntity
        {
            ContactId = 10,
            Email = "am-role@test.com",
            FirstName = "AM",
            LastName = "Role",
            Type = "User",
            ContactGlobalUniqueId = Guid.NewGuid()
        });
        await context.SaveChangesAsync();

        var bgJobOptions = Microsoft.Extensions.Options.Options.Create(new BackGroundJobOptions { Chunk = 1 });

        var opServiceMock = new Mock<IOperationService>();
        var sequence = new MockSequence();
        opServiceMock.InSequence(sequence).Setup(s => s.GetRoleOperationRecordsAsync(
                It.Is<string>(op => op == OperationAction.Insert),
                It.IsAny<int>(),
                false))
            .ReturnsAsync(() => new List<RoleOperationRecord>
            {
                new RoleOperationRecord { Operation = opEntity, Role = refRole }
            });

        opServiceMock.Setup(s => s.UpdateOperationStatusListASync(ProcessStatus.Sent, It.IsAny<List<RegOperationEntity>>()))
            .Returns(Task.CompletedTask);
        opServiceMock.Setup(s => s.TryToProceedUntilTimeoutAsync(OperationCategory.ROLE, OperationAction.Insert))
            .Returns(Task.CompletedTask);

        var notificationManagerMock = new Mock<INotificationManager>();
        notificationManagerMock.Setup(nm => nm.BulkPublishAsync(It.IsAny<List<ServiceBusMessage>>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var dummyMessage = new ServiceBusMessage("dummy");
        var messageFactoryMock = new Mock<IServiceBusMessageFactory>();
        messageFactoryMock.Setup(mf => mf.CreateMessage(
                It.Is<RegistryRoleCreatedEvent>(e =>
                    e.Data.AccountNumber == refRole.AccountNumber &&
                    e.Data.Email == refRole.ContactEmail &&
                    e.Data.IsCustomerRelation == true &&
                    e.Data.Description == "AM"),
                It.IsAny<string>()))
            .Returns(dummyMessage);

        var orchestrator = CreateOrchestrator(context, opServiceMock.Object, notificationManagerMock.Object, messageFactoryMock.Object, bgJobOptions);

        // Act
        await orchestrator.ProcessRolePublishAsync(OperationAction.Insert);

        // Assert
        messageFactoryMock.Verify(mf => mf.CreateMessage(
            It.Is<RegistryRoleCreatedEvent>(e =>
                e.Data.IsCustomerRelation == true &&
                e.Data.Description == "AM"),
            It.IsAny<string>()), Times.AtLeastOnce);
    }

    #endregion
}

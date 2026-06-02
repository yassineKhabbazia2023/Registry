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
    public async Task PublishApprovedRoleInsertsAsync_Should_PublishApprovedInserts_AndMarkSent_WithoutTimeoutSweep()
    {
        // Arrange
        var options = CreateInMemoryOptions(nameof(PublishApprovedRoleInsertsAsync_Should_PublishApprovedInserts_AndMarkSent_WithoutTimeoutSweep));
        using var context = new RefContext(options);

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

        var notificationManagerMock = new Mock<INotificationManager>();
        notificationManagerMock.Setup(nm => nm.BulkPublishAsync(It.IsAny<List<ServiceBusMessage>>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var dummyMessage = new ServiceBusMessage("dummy");
        var messageFactoryMock = new Mock<IServiceBusMessageFactory>();
        messageFactoryMock.Setup(mf => mf.CreateMessage(
                It.IsAny<RegistryRoleCreatedEvent>(),
                It.IsAny<string>()))
            .Returns(dummyMessage);

        var orchestrator = CreateOrchestrator(context, opServiceMock.Object, notificationManagerMock.Object, messageFactoryMock.Object, bgJobOptions);

        // Act
        await orchestrator.PublishApprovedRoleInsertsAsync();

        // Assert
        messageFactoryMock.Verify(mf => mf.CreateMessage(
            It.IsAny<RegistryRoleCreatedEvent>(), It.IsAny<string>()), Times.AtLeastOnce);
        opServiceMock.Verify(s => s.UpdateOperationStatusListASync(ProcessStatus.Sent, It.IsAny<List<RegOperationEntity>>()), Times.Once);
        notificationManagerMock.Verify(nm => nm.BulkPublishAsync(It.IsAny<List<ServiceBusMessage>>(), It.IsAny<string>()), Times.AtLeastOnce);
        opServiceMock.Verify(s => s.TryToProceedUntilTimeoutAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
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

    [Fact]
    public async Task ProcessRolePublishAsync_InsertBranch_Should_PopulateRoleSignatoryFromContactFlagMainContact()
    {
        // Arrange
        var options = CreateInMemoryOptions(nameof(ProcessRolePublishAsync_InsertBranch_Should_PopulateRoleSignatoryFromContactFlagMainContact));
        using var context = new RefContext(options);

        var opEntity = new RegOperationEntity
        {
            Id = 30,
            Operation = OperationAction.Insert,
            CreationDate = DateTime.UtcNow,
            EntityId = Guid.NewGuid(),
            LastStatusApprovalDate = DateTime.UtcNow,
            LastStatusApprovalBy = "signatory@test.com",
            PublishedAt = null,
            ApprovalStatus = ApprovalStatus.Approved,
            Type = OperationCategory.ROLE,
            ProcessStatus = ProcessStatus.Ready
        };
        context.RegOperationEntity.Add(opEntity);

        var refRole = new RefRoleEntity
        {
            EntityId = opEntity.EntityId,
            AccountNumber = "ROLE030",
            ContactEmail = "signatory@test.com",
            Description = "CLP",
            OperationType = OperationAction.Insert,
            ContactFlagPortailFactures = true,
            ContactFlagMainContact = true,
        };
        context.RefRoleEntity.Add(refRole);

        context.AccountEntity.Add(new AccountEntity
        {
            AccountId = 30,
            AccountNumber = "ROLE030",
            AccountGlobalUniqueId = Guid.NewGuid(),
            LegalName = "legal",
        });
        context.ContactEntity.Add(new ContactEntity
        {
            ContactId = 30,
            Email = "signatory@test.com",
            FirstName = "Signatory",
            LastName = "Test",
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
                    e.Data.RoleSignatory == true),
                It.IsAny<string>()))
            .Returns(dummyMessage);

        var orchestrator = CreateOrchestrator(context, opServiceMock.Object, notificationManagerMock.Object, messageFactoryMock.Object, bgJobOptions);

        // Act
        await orchestrator.ProcessRolePublishAsync(OperationAction.Insert);

        // Assert
        messageFactoryMock.Verify(mf => mf.CreateMessage(
            It.Is<RegistryRoleCreatedEvent>(e => e.Data.RoleSignatory == true),
            It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessRolePublishAsync_InsertBranch_Should_SetRoleSignatoryNullWhenContactFlagMainContactIsNull()
    {
        // Arrange
        var options = CreateInMemoryOptions(nameof(ProcessRolePublishAsync_InsertBranch_Should_SetRoleSignatoryNullWhenContactFlagMainContactIsNull));
        using var context = new RefContext(options);

        var opEntity = new RegOperationEntity
        {
            Id = 31,
            Operation = OperationAction.Insert,
            CreationDate = DateTime.UtcNow,
            EntityId = Guid.NewGuid(),
            LastStatusApprovalDate = DateTime.UtcNow,
            LastStatusApprovalBy = "nullsignatory@test.com",
            PublishedAt = null,
            ApprovalStatus = ApprovalStatus.Approved,
            Type = OperationCategory.ROLE,
            ProcessStatus = ProcessStatus.Ready
        };
        context.RegOperationEntity.Add(opEntity);

        var refRole = new RefRoleEntity
        {
            EntityId = opEntity.EntityId,
            AccountNumber = "ROLE031",
            ContactEmail = "nullsignatory@test.com",
            Description = "CLP",
            OperationType = OperationAction.Insert,
            ContactFlagPortailFactures = false,
            ContactFlagMainContact = null,
        };
        context.RefRoleEntity.Add(refRole);

        context.AccountEntity.Add(new AccountEntity
        {
            AccountId = 31,
            AccountNumber = "ROLE031",
            AccountGlobalUniqueId = Guid.NewGuid(),
            LegalName = "legal",
        });
        context.ContactEntity.Add(new ContactEntity
        {
            ContactId = 31,
            Email = "nullsignatory@test.com",
            FirstName = "NullSignatory",
            LastName = "Test",
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
                    e.Data.RoleSignatory == null),
                It.IsAny<string>()))
            .Returns(dummyMessage);

        var orchestrator = CreateOrchestrator(context, opServiceMock.Object, notificationManagerMock.Object, messageFactoryMock.Object, bgJobOptions);

        // Act
        await orchestrator.ProcessRolePublishAsync(OperationAction.Insert);

        // Assert
        messageFactoryMock.Verify(mf => mf.CreateMessage(
            It.Is<RegistryRoleCreatedEvent>(e => e.Data.RoleSignatory == null),
            It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessRolePublishAsync_InsertBranch_Should_UseLatestRefRoleFlagsWhenNewerInsertExists()
    {
        // Arrange — op points to an older RefRole (flags=null/false). A newer Insert RefRole
        // exists for the same account+contact with flags=true. The published event must use
        // the newer flags so a moderator validating an old pending op doesn't apply stale flags.
        var options = CreateInMemoryOptions(nameof(ProcessRolePublishAsync_InsertBranch_Should_UseLatestRefRoleFlagsWhenNewerInsertExists));
        using var context = new RefContext(options);

        var opEntity = new RegOperationEntity
        {
            Id = 40,
            Operation = OperationAction.Insert,
            CreationDate = DateTime.UtcNow,
            EntityId = Guid.NewGuid(),
            LastStatusApprovalBy = "stale@test.com",
            PublishedAt = null,
            ApprovalStatus = ApprovalStatus.Approved,
            Type = OperationCategory.ROLE,
            ProcessStatus = ProcessStatus.Ready
        };
        context.RegOperationEntity.Add(opEntity);

        var olderRefRole = new RefRoleEntity
        {
            EntityId = opEntity.EntityId,
            AccountNumber = "ROLE040",
            ContactEmail = "stale@test.com",
            Description = "CLP",
            OperationType = OperationAction.Insert,
            OperationDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ContactFlagPortailFactures = false,
            ContactFlagMainContact = null,
        };
        var newerRefRole = new RefRoleEntity
        {
            EntityId = Guid.NewGuid(),
            AccountNumber = "ROLE040",
            ContactEmail = "stale@test.com",
            Description = "CLP",
            OperationType = OperationAction.Insert,
            OperationDate = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            ContactFlagPortailFactures = true,
            ContactFlagMainContact = true,
        };
        context.RefRoleEntity.AddRange(olderRefRole, newerRefRole);

        context.AccountEntity.Add(new AccountEntity
        {
            AccountId = 40,
            AccountNumber = "ROLE040",
            AccountGlobalUniqueId = Guid.NewGuid(),
            LegalName = "legal",
        });
        context.ContactEntity.Add(new ContactEntity
        {
            ContactId = 40,
            Email = "stale@test.com",
            FirstName = "Stale",
            LastName = "Pending",
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
                new RoleOperationRecord { Operation = opEntity, Role = olderRefRole }
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
        messageFactoryMock.Setup(mf => mf.CreateMessage(It.IsAny<RegistryRoleCreatedEvent>(), It.IsAny<string>()))
            .Returns(dummyMessage);

        var orchestrator = CreateOrchestrator(context, opServiceMock.Object, notificationManagerMock.Object, messageFactoryMock.Object, bgJobOptions);

        // Act
        await orchestrator.ProcessRolePublishAsync(OperationAction.Insert);

        // Assert — event must carry the newer flags, not the op's stale flags
        messageFactoryMock.Verify(mf => mf.CreateMessage(
            It.Is<RegistryRoleCreatedEvent>(e =>
                e.Data.RoleSignatory == true &&
                e.Data.ContactFlagPortailFactures == true),
            It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessRolePublishAsync_InsertBranch_Should_UseOpRefRoleFlagsWhenItIsTheLatest()
    {
        // Arrange — op points to the most recent RefRole. An older RefRole with different flags
        // exists for the same account+contact. The published event must use the op's (latest) flags.
        var options = CreateInMemoryOptions(nameof(ProcessRolePublishAsync_InsertBranch_Should_UseOpRefRoleFlagsWhenItIsTheLatest));
        using var context = new RefContext(options);

        var opEntity = new RegOperationEntity
        {
            Id = 41,
            Operation = OperationAction.Insert,
            CreationDate = DateTime.UtcNow,
            EntityId = Guid.NewGuid(),
            LastStatusApprovalBy = "fresh@test.com",
            PublishedAt = null,
            ApprovalStatus = ApprovalStatus.Approved,
            Type = OperationCategory.ROLE,
            ProcessStatus = ProcessStatus.Ready
        };
        context.RegOperationEntity.Add(opEntity);

        var olderRefRole = new RefRoleEntity
        {
            EntityId = Guid.NewGuid(),
            AccountNumber = "ROLE041",
            ContactEmail = "fresh@test.com",
            Description = "CLP",
            OperationType = OperationAction.Insert,
            OperationDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ContactFlagPortailFactures = false,
            ContactFlagMainContact = null,
        };
        var freshRefRole = new RefRoleEntity
        {
            EntityId = opEntity.EntityId,
            AccountNumber = "ROLE041",
            ContactEmail = "fresh@test.com",
            Description = "CLP",
            OperationType = OperationAction.Insert,
            OperationDate = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            ContactFlagPortailFactures = true,
            ContactFlagMainContact = true,
        };
        context.RefRoleEntity.AddRange(olderRefRole, freshRefRole);

        context.AccountEntity.Add(new AccountEntity
        {
            AccountId = 41,
            AccountNumber = "ROLE041",
            AccountGlobalUniqueId = Guid.NewGuid(),
            LegalName = "legal",
        });
        context.ContactEntity.Add(new ContactEntity
        {
            ContactId = 41,
            Email = "fresh@test.com",
            FirstName = "Fresh",
            LastName = "Op",
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
                new RoleOperationRecord { Operation = opEntity, Role = freshRefRole }
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
        messageFactoryMock.Setup(mf => mf.CreateMessage(It.IsAny<RegistryRoleCreatedEvent>(), It.IsAny<string>()))
            .Returns(dummyMessage);

        var orchestrator = CreateOrchestrator(context, opServiceMock.Object, notificationManagerMock.Object, messageFactoryMock.Object, bgJobOptions);

        // Act
        await orchestrator.ProcessRolePublishAsync(OperationAction.Insert);

        // Assert
        messageFactoryMock.Verify(mf => mf.CreateMessage(
            It.Is<RegistryRoleCreatedEvent>(e =>
                e.Data.RoleSignatory == true &&
                e.Data.ContactFlagPortailFactures == true),
            It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessRolePublishAsync_InsertBranch_Should_IgnoreNewerDeleteRefRoleWhenSelectingFlags()
    {
        // Arrange — op points to an older Insert RefRole. A newer DELETE RefRole exists for
        // the same account+contact. Latest-flag selection must ignore DELETE RefRoles since their
        // flags are not meaningful for an Insert event.
        var options = CreateInMemoryOptions(nameof(ProcessRolePublishAsync_InsertBranch_Should_IgnoreNewerDeleteRefRoleWhenSelectingFlags));
        using var context = new RefContext(options);

        var opEntity = new RegOperationEntity
        {
            Id = 42,
            Operation = OperationAction.Insert,
            CreationDate = DateTime.UtcNow,
            EntityId = Guid.NewGuid(),
            LastStatusApprovalBy = "delete-after@test.com",
            PublishedAt = null,
            ApprovalStatus = ApprovalStatus.Approved,
            Type = OperationCategory.ROLE,
            ProcessStatus = ProcessStatus.Ready
        };
        context.RegOperationEntity.Add(opEntity);

        var insertRefRole = new RefRoleEntity
        {
            EntityId = opEntity.EntityId,
            AccountNumber = "ROLE042",
            ContactEmail = "delete-after@test.com",
            Description = "CLP",
            OperationType = OperationAction.Insert,
            OperationDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ContactFlagPortailFactures = false,
            ContactFlagMainContact = null,
        };
        var newerDeleteRefRole = new RefRoleEntity
        {
            EntityId = Guid.NewGuid(),
            AccountNumber = "ROLE042",
            ContactEmail = "delete-after@test.com",
            OperationType = OperationAction.Delete,
            OperationDate = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            ContactFlagPortailFactures = true,
            ContactFlagMainContact = true,
        };
        context.RefRoleEntity.AddRange(insertRefRole, newerDeleteRefRole);

        context.AccountEntity.Add(new AccountEntity
        {
            AccountId = 42,
            AccountNumber = "ROLE042",
            AccountGlobalUniqueId = Guid.NewGuid(),
            LegalName = "legal",
        });
        context.ContactEntity.Add(new ContactEntity
        {
            ContactId = 42,
            Email = "delete-after@test.com",
            FirstName = "Delete",
            LastName = "After",
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
                new RoleOperationRecord { Operation = opEntity, Role = insertRefRole }
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
        messageFactoryMock.Setup(mf => mf.CreateMessage(It.IsAny<RegistryRoleCreatedEvent>(), It.IsAny<string>()))
            .Returns(dummyMessage);

        var orchestrator = CreateOrchestrator(context, opServiceMock.Object, notificationManagerMock.Object, messageFactoryMock.Object, bgJobOptions);

        // Act
        await orchestrator.ProcessRolePublishAsync(OperationAction.Insert);

        // Assert — event must carry the Insert RefRole's flags, ignoring the DELETE
        messageFactoryMock.Verify(mf => mf.CreateMessage(
            It.Is<RegistryRoleCreatedEvent>(e =>
                e.Data.RoleSignatory == null &&
                e.Data.ContactFlagPortailFactures == false),
            It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessRolePublishAsync_InsertBranch_Should_KeepOpDescriptionAndSubRoleEvenWhenLatestRefRoleDiffers()
    {
        // Arrange — op points to RefRole #1 (Description=CLP, SubRole=X, flags=null). A newer
        // RefRole has Description=AM, SubRole=Y, flags=true. Only the flags should be overridden.
        var options = CreateInMemoryOptions(nameof(ProcessRolePublishAsync_InsertBranch_Should_KeepOpDescriptionAndSubRoleEvenWhenLatestRefRoleDiffers));
        using var context = new RefContext(options);

        var opEntity = new RegOperationEntity
        {
            Id = 43,
            Operation = OperationAction.Insert,
            CreationDate = DateTime.UtcNow,
            EntityId = Guid.NewGuid(),
            LastStatusApprovalBy = "desc-keep@test.com",
            PublishedAt = null,
            ApprovalStatus = ApprovalStatus.Approved,
            Type = OperationCategory.ROLE,
            ProcessStatus = ProcessStatus.Ready
        };
        context.RegOperationEntity.Add(opEntity);

        var opRefRole = new RefRoleEntity
        {
            EntityId = opEntity.EntityId,
            AccountNumber = "ROLE043",
            ContactEmail = "desc-keep@test.com",
            Description = "CLP",
            SubRole = "X",
            OperationType = OperationAction.Insert,
            OperationDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ContactFlagPortailFactures = false,
            ContactFlagMainContact = null,
        };
        var newerRefRole = new RefRoleEntity
        {
            EntityId = Guid.NewGuid(),
            AccountNumber = "ROLE043",
            ContactEmail = "desc-keep@test.com",
            Description = "AM",
            SubRole = "Y",
            OperationType = OperationAction.Insert,
            OperationDate = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            ContactFlagPortailFactures = true,
            ContactFlagMainContact = true,
        };
        context.RefRoleEntity.AddRange(opRefRole, newerRefRole);

        context.AccountEntity.Add(new AccountEntity
        {
            AccountId = 43,
            AccountNumber = "ROLE043",
            AccountGlobalUniqueId = Guid.NewGuid(),
            LegalName = "legal",
        });
        context.ContactEntity.Add(new ContactEntity
        {
            ContactId = 43,
            Email = "desc-keep@test.com",
            FirstName = "Desc",
            LastName = "Keep",
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
                new RoleOperationRecord { Operation = opEntity, Role = opRefRole }
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
        messageFactoryMock.Setup(mf => mf.CreateMessage(It.IsAny<RegistryRoleCreatedEvent>(), It.IsAny<string>()))
            .Returns(dummyMessage);

        var orchestrator = CreateOrchestrator(context, opServiceMock.Object, notificationManagerMock.Object, messageFactoryMock.Object, bgJobOptions);

        // Act
        await orchestrator.ProcessRolePublishAsync(OperationAction.Insert);

        // Assert — Description and SubRole stay from op's RefRole, flags come from the newer one
        messageFactoryMock.Verify(mf => mf.CreateMessage(
            It.Is<RegistryRoleCreatedEvent>(e =>
                e.Data.Description == "CLP" &&
                e.Data.SubRole == "X" &&
                e.Data.RoleSignatory == true &&
                e.Data.ContactFlagPortailFactures == true),
            It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessRolePublishAsync_InsertBranch_ForProspectAccount_ShouldKeepClientBehavior()
    {
        var options = CreateInMemoryOptions(nameof(ProcessRolePublishAsync_InsertBranch_ForProspectAccount_ShouldKeepClientBehavior));
        using var context = new RefContext(options);

        var opEntity = new RegOperationEntity
        {
            Id = 70,
            Operation = OperationAction.Insert,
            CreationDate = DateTime.UtcNow,
            EntityId = Guid.NewGuid(),
            LastStatusApprovalDate = DateTime.UtcNow,
            LastStatusApprovalBy = "prospect-insert@test.com",
            ApprovalStatus = ApprovalStatus.Approved,
            Type = OperationCategory.ROLE,
            ProcessStatus = ProcessStatus.Ready
        };
        context.RegOperationEntity.Add(opEntity);

        var refRole = new RefRoleEntity
        {
            EntityId = opEntity.EntityId,
            AccountNumber = "PROSPECTROLE001",
            ContactEmail = "prospect-role-insert@test.com",
            Description = "CLP",
            OperationType = OperationAction.Insert
        };

        context.RefRoleEntity.Add(refRole);
        context.RefAccountEntity.Add(new RefAccountEntity
        {
            EntityId = Guid.NewGuid(),
            AccountNumber = "PROSPECTROLE001",
            AccountType = "PROSPECT",
            OperationType = OperationAction.Insert,
            OperationDate = DateTime.UtcNow
        });
        context.AccountEntity.Add(new AccountEntity
        {
            AccountId = 70,
            AccountNumber = "PROSPECTROLE001",
            AccountGlobalUniqueId = Guid.NewGuid(),
            LegalName = "Prospect Role Insert"
        });
        context.ContactEntity.Add(new ContactEntity
        {
            ContactId = 70,
            Email = "prospect-role-insert@test.com",
            ContactGlobalUniqueId = Guid.NewGuid(),
            FirstName = "Prospect",
            LastName = "Insert",
            Type = "User"
        });
        await context.SaveChangesAsync();

        var bgJobOptions = Microsoft.Extensions.Options.Options.Create(new BackGroundJobOptions { Chunk = 1 });
        var opServiceMock = new Mock<IOperationService>();
        opServiceMock.Setup(s => s.GetRoleOperationRecordsAsync(OperationAction.Insert, bgJobOptions.Value.Chunk, true))
            .ReturnsAsync(new List<RoleOperationRecord>());
        opServiceMock.SetupSequence(s => s.GetRoleOperationRecordsAsync(OperationAction.Insert, bgJobOptions.Value.Chunk, false))
            .ReturnsAsync(new List<RoleOperationRecord> { new() { Operation = opEntity, Role = refRole } })
            .ReturnsAsync(new List<RoleOperationRecord>());
        opServiceMock.Setup(s => s.UpdateOperationStatusListASync(ProcessStatus.Sent, It.IsAny<List<RegOperationEntity>>()))
            .Returns(Task.CompletedTask);
        opServiceMock.Setup(s => s.TryToProceedUntilTimeoutAsync(OperationCategory.ROLE, OperationAction.Insert))
            .Returns(Task.CompletedTask);

        var notificationManagerMock = new Mock<INotificationManager>();
        notificationManagerMock.Setup(nm => nm.BulkPublishAsync(It.IsAny<List<ServiceBusMessage>>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var dummyMessage = new ServiceBusMessage("dummy");
        var messageFactoryMock = new Mock<IServiceBusMessageFactory>();
        messageFactoryMock.Setup(mf => mf.CreateMessage(It.IsAny<RegistryRoleCreatedEvent>(), It.IsAny<string>()))
            .Returns(dummyMessage);

        var orchestrator = CreateOrchestrator(context, opServiceMock.Object, notificationManagerMock.Object, messageFactoryMock.Object, bgJobOptions);

        await orchestrator.ProcessRolePublishAsync(OperationAction.Insert);

        messageFactoryMock.Verify(mf => mf.CreateMessage(It.IsAny<RegistryRoleCreatedEvent>(), It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessRolePublishAsync_DeleteBranch_ForProspectAccount_ShouldKeepClientBehavior()
    {
        var options = CreateInMemoryOptions(nameof(ProcessRolePublishAsync_DeleteBranch_ForProspectAccount_ShouldKeepClientBehavior));
        using var context = new RefContext(options);

        var opEntity = new RegOperationEntity
        {
            Id = 71,
            Operation = OperationAction.Delete,
            CreationDate = DateTime.UtcNow,
            EntityId = Guid.NewGuid(),
            LastStatusApprovalDate = DateTime.UtcNow,
            LastStatusApprovalBy = "prospect-delete@test.com",
            ApprovalStatus = ApprovalStatus.Approved,
            Type = OperationCategory.ROLE,
            ProcessStatus = ProcessStatus.Ready
        };
        context.RegOperationEntity.Add(opEntity);

        var refRole = new RefRoleEntity
        {
            EntityId = opEntity.EntityId,
            AccountNumber = "PROSPECTROLE002",
            ContactEmail = "prospect-role-delete@test.com",
            OperationType = OperationAction.Delete
        };

        context.RefRoleEntity.Add(refRole);
        context.RefAccountEntity.Add(new RefAccountEntity
        {
            EntityId = Guid.NewGuid(),
            AccountNumber = "PROSPECTROLE002",
            AccountType = "PROSPECT",
            OperationType = OperationAction.Delete,
            OperationDate = DateTime.UtcNow
        });
        context.AccountEntity.Add(new AccountEntity
        {
            AccountId = 71,
            AccountNumber = "PROSPECTROLE002",
            AccountGlobalUniqueId = Guid.NewGuid(),
            LegalName = "Prospect Role Delete"
        });
        context.ContactEntity.Add(new ContactEntity
        {
            ContactId = 71,
            Email = "prospect-role-delete@test.com",
            ContactGlobalUniqueId = Guid.NewGuid(),
            FirstName = "Prospect",
            LastName = "Delete",
            Type = "User"
        });
        await context.SaveChangesAsync();

        var bgJobOptions = Microsoft.Extensions.Options.Options.Create(new BackGroundJobOptions { Chunk = 1 });
        var opServiceMock = new Mock<IOperationService>();
        opServiceMock.Setup(s => s.GetRoleOperationRecordsAsync(OperationAction.Delete, bgJobOptions.Value.Chunk, true))
            .ReturnsAsync(new List<RoleOperationRecord>());
        opServiceMock.SetupSequence(s => s.GetRoleOperationRecordsAsync(OperationAction.Delete, bgJobOptions.Value.Chunk, false))
            .ReturnsAsync(new List<RoleOperationRecord> { new() { Operation = opEntity, Role = refRole } })
            .ReturnsAsync(new List<RoleOperationRecord>());
        opServiceMock.Setup(s => s.UpdateOperationStatusListASync(ProcessStatus.Sent, It.IsAny<List<RegOperationEntity>>()))
            .Returns(Task.CompletedTask);
        opServiceMock.Setup(s => s.TryToProceedUntilTimeoutAsync(OperationCategory.ROLE, OperationAction.Delete))
            .Returns(Task.CompletedTask);

        var notificationManagerMock = new Mock<INotificationManager>();
        notificationManagerMock.Setup(nm => nm.BulkPublishAsync(It.IsAny<List<ServiceBusMessage>>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var dummyMessage = new ServiceBusMessage("dummy");
        var messageFactoryMock = new Mock<IServiceBusMessageFactory>();
        messageFactoryMock.Setup(mf => mf.CreateMessage(It.IsAny<RegistryRoleRemovedEvent>(), It.IsAny<string>()))
            .Returns(dummyMessage);

        var orchestrator = CreateOrchestrator(context, opServiceMock.Object, notificationManagerMock.Object, messageFactoryMock.Object, bgJobOptions);

        await orchestrator.ProcessRolePublishAsync(OperationAction.Delete);

        messageFactoryMock.Verify(mf => mf.CreateMessage(It.IsAny<RegistryRoleRemovedEvent>(), It.IsAny<string>()), Times.AtLeastOnce);
    }

    #endregion
}

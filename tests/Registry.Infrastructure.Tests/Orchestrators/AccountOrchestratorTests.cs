using Application.Consts;
using Application.Interfaces;
using Application.Options;
using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Pulse.Registry.Domain.Context;
using Pulse.Registry.Domain.Entities;
using Registry.Application.Consts;
using Infrastructure.Orchestrators;
using Application.Models;
using Pulse.Back.Events.Abstractions;
using Pulse.Back.Events.IntegrationEvents;
using Registry.Infrastructure.Managers;
using Pulse.Back.Events.IntegrationEvents.EventsData;

namespace Registry.Infrastructure.Tests.Orchestrators
{
    public class AccountOrchestratorTests
    {
        private DbContextOptions<RefContext> CreateInMemoryOptions(string databaseName)
        {
            return new DbContextOptionsBuilder<RefContext>()
                .UseInMemoryDatabase($"{databaseName}_{Guid.NewGuid()}")
                .Options;
        }

        private AccountOrchestrator CreateOrchestrator(
            RefContext context,
            IOperationService operationService,
            INotificationManager notificationManager,
            IServiceBusMessageFactory messageFactory,
            IOptions<BackGroundJobOptions> bgJobOptions)
        {
            var loggerMock = new Mock<ILogger<AccountOrchestrator>>();
            return new AccountOrchestrator(
                loggerMock.Object,
                context,
                notificationManager,
                bgJobOptions,
                messageFactory,
                operationService);
        }

        #region ProcessAccountPublishAsync Tests

        [Fact]
        public async Task ProcessAccountPublishAsync_InsertBranch_Should_CreateCreatedEventAndUpdateStatus()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(ProcessAccountPublishAsync_InsertBranch_Should_CreateCreatedEventAndUpdateStatus));
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
                Type = OperationCategory.ACCOUNT,
                ProcessStatus = ProcessStatus.Ready
            };
            context.RegOperationEntity.Add(opEntity);

            var refAccount = new RefAccountEntity
            {
                EntityId = opEntity.EntityId,
                AccountNumber = "ACC001",
                LegalName = "Test Account Insert",
                OperationType = OperationAction.Insert
            };
            context.RefAccountEntity.Add(refAccount);

            context.AccountEntities.Add(new Domain.Entities.Accounts.AccountEntity
            {
                AccountId = 1,
                AccountNumber = "ACC001",
                AccountGlobalUniqueId = Guid.NewGuid()
            });
            await context.SaveChangesAsync();

            var bgJobOptions = Microsoft.Extensions.Options.Options.Create(new BackGroundJobOptions { Chunk = 1 });
            var opServiceMock = new Mock<IOperationService>();
            var sequence = new MockSequence();
            opServiceMock.InSequence(sequence)
                .Setup(s => s.GetAccountOperationRecordsBatch(
                    It.Is<string>(op => op == OperationAction.Insert), It.IsAny<int>()))
                .Callback<string, int>((op, chunkSize) =>
                {
                    Assert.Equal(bgJobOptions.Value.Chunk, chunkSize);
                    Assert.Equal(OperationAction.Insert, op);
                })
                .Returns(() => new List<AccountOperationRecord>
                {
                    new AccountOperationRecord { Operation = opEntity, Account = refAccount }
                })
                .Verifiable();
            opServiceMock.InSequence(sequence)
                .Setup(s => s.GetAccountOperationRecordsBatch(
                    It.Is<string>(op => op == OperationAction.Insert), It.IsAny<int>()))
                .Callback<string, int>((op, chunkSize) =>
                {
                    Assert.Equal(bgJobOptions.Value.Chunk, chunkSize);
                    Assert.Equal(OperationAction.Insert, op);
                })
                .Returns(() => new List<AccountOperationRecord>())
                .Verifiable();

            opServiceMock.Setup(s => s.UpdateOperationStatusListASync(ProcessStatus.Sent, It.IsAny<List<RegOperationEntity>>()))
                .Returns(Task.CompletedTask);
            opServiceMock.Setup(s => s.TryToProceedUntilTimeoutAsync(OperationCategory.ACCOUNT, OperationAction.Insert))
                .Returns(Task.CompletedTask);

            var notificationManagerMock = new Mock<INotificationManager>();
            notificationManagerMock.Setup(nm => nm.BulkPublishAsync(It.IsAny<List<ServiceBusMessage>>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var dummyMessage = new ServiceBusMessage("dummy");
            var messageFactoryMock = new Mock<IServiceBusMessageFactory>();
            messageFactoryMock.Setup(mf => mf.CreateMessage(
                    It.IsAny<RegistryAccountCreatedEvent>(), It.IsAny<string>()))
                .Callback<BaseEvent<RegistryAccountCreatedEventData>, string>((evt, s) =>
                {
                    var data = evt.Data;
                    Assert.Equal(refAccount.AccountNumber, data.AccountNumber);
                    Assert.Equal(refAccount.LegalName, data.AccountLegalName);
                    Assert.Equal(refAccount.AccountType, data.AccountType);
                    Assert.Equal(refAccount.AccountEmail, data.AccountEmail);
                    Assert.Equal(refAccount.AccountNafIdentifier, data.AccountNafIdentifier);
                })
                .Returns(dummyMessage);

            var orchestrator = CreateOrchestrator(context, opServiceMock.Object, notificationManagerMock.Object, messageFactoryMock.Object, bgJobOptions);

            // Act
            await orchestrator.ProcessAccountPublishAsync(OperationAction.Insert);

            // Assert
            messageFactoryMock.Verify(mf => mf.CreateMessage(
                It.IsAny<RegistryAccountCreatedEvent>(), It.IsAny<string>()), Times.AtLeastOnce);
            opServiceMock.Verify(s => s.GetAccountOperationRecordsBatch(OperationAction.Insert, bgJobOptions.Value.Chunk), Times.AtLeastOnce);
            opServiceMock.Verify(s => s.UpdateOperationStatusListASync(ProcessStatus.Sent, It.IsAny<List<RegOperationEntity>>()), Times.Once);
            opServiceMock.Verify(s => s.TryToProceedUntilTimeoutAsync(OperationCategory.ACCOUNT, OperationAction.Insert), Times.Once);
            notificationManagerMock.Verify(nm => nm.BulkPublishAsync(It.IsAny<List<ServiceBusMessage>>(), It.IsAny<string>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task ProcessAccountPublishAsync_DeleteBranch_NoAccountFound_Should_SetOperationFailed()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(ProcessAccountPublishAsync_DeleteBranch_NoAccountFound_Should_SetOperationFailed));
            using var context = new RefContext(options);

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
                Type = OperationCategory.ACCOUNT,
                ProcessStatus = ProcessStatus.Ready
            };
            context.RegOperationEntity.Add(opEntity);

            var refAccount = new RefAccountEntity
            {
                EntityId = opEntity.EntityId,
                AccountNumber = "ACC002",
                LegalName = "Test Account Delete",
                OperationType = OperationAction.Delete
            };
            context.RefAccountEntity.Add(refAccount);
            await context.SaveChangesAsync();

            var bgJobOptions = Microsoft.Extensions.Options.Options.Create(new BackGroundJobOptions { Chunk = 1 });
            var opServiceMock = new Mock<IOperationService>();
            var sequence = new MockSequence();
            opServiceMock.InSequence(sequence)
                .Setup(s => s.GetAccountOperationRecordsBatch(
                    It.Is<string>(op => op == OperationAction.Delete), It.IsAny<int>()))
                .Callback<string, int>((op, chunkSize) =>
                {
                    Assert.Equal(bgJobOptions.Value.Chunk, chunkSize);
                    Assert.Equal(OperationAction.Delete, op);
                })
                .Returns(() => new List<AccountOperationRecord>
                {
                    new AccountOperationRecord { Operation = opEntity, Account = refAccount }
                })
                .Verifiable();
            opServiceMock.InSequence(sequence)
                .Setup(s => s.GetAccountOperationRecordsBatch(
                    It.Is<string>(op => op == OperationAction.Delete), It.IsAny<int>()))
                .Callback<string, int>((op, chunkSize) =>
                {
                    Assert.Equal(bgJobOptions.Value.Chunk, chunkSize);
                    Assert.Equal(OperationAction.Delete, op);
                })
                .Returns(() => new List<AccountOperationRecord>())
                .Verifiable();
            opServiceMock.Setup(s => s.UpdateOperationStatusListASync(It.IsAny<string>(), It.IsAny<List<RegOperationEntity>>()))
                .Returns(Task.CompletedTask);
            opServiceMock.Setup(s => s.TryToProceedUntilTimeoutAsync(OperationCategory.ACCOUNT, OperationAction.Delete))
                .Returns(Task.CompletedTask);

            var notificationManagerMock = new Mock<INotificationManager>();
            var messageFactoryMock = new Mock<IServiceBusMessageFactory>();

            var orchestrator = CreateOrchestrator(context, opServiceMock.Object, notificationManagerMock.Object, messageFactoryMock.Object, bgJobOptions);

            // Act
            await orchestrator.ProcessAccountPublishAsync(OperationAction.Delete);

            // Assert
            var updatedOp = context.RegOperationEntity.First(o => o.Id == opEntity.Id);
            Assert.Equal(ProcessStatus.Failed, updatedOp.ProcessStatus);
        }
       
        [Fact]
        public async Task ProcessAccountPublishAsync_DeleteBranch_AccountFound_Should_CreateDeletedEventAndUpdateStatus()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(ProcessAccountPublishAsync_DeleteBranch_AccountFound_Should_CreateDeletedEventAndUpdateStatus));
            using var context = new RefContext(options);

            var opEntity = new RegOperationEntity
            {
                Id = 10,
                Operation = OperationAction.Delete,
                CreationDate = DateTime.UtcNow,
                EntityId = Guid.NewGuid(),
                LastStatusApprovalDate = DateTime.UtcNow,
                LastStatusApprovalBy = "delete@test.com",
                PublishedAt = null,
                ApprovalStatus = ApprovalStatus.Approved,
                Type = OperationCategory.ACCOUNT,
                ProcessStatus = ProcessStatus.Ready
            };
            context.RegOperationEntity.Add(opEntity);

            var refAccount = new RefAccountEntity
            {
                EntityId = opEntity.EntityId,
                AccountNumber = "ACC010",
                LegalName = "Test Account Delete Found",
                OperationType = OperationAction.Delete
            };
            context.RefAccountEntity.Add(refAccount);

            var accountEntity = new Domain.Entities.Accounts.AccountEntity
            {
                AccountId = 10,
                AccountNumber = "ACC010",
                AccountGlobalUniqueId = Guid.NewGuid()
            };
            context.AccountEntities.Add(accountEntity);
            await context.SaveChangesAsync();

            var bgJobOptions = Microsoft.Extensions.Options.Options.Create(new BackGroundJobOptions { Chunk = 1 });
            var opServiceMock = new Mock<IOperationService>();
            var sequence = new MockSequence();
            opServiceMock.InSequence(sequence)
                .Setup(s => s.GetAccountOperationRecordsBatch(
                    It.Is<string>(op => op == OperationAction.Delete), It.IsAny<int>()))
                .Callback<string, int>((op, chunkSize) =>
                {
                    Assert.Equal(bgJobOptions.Value.Chunk, chunkSize);
                    Assert.Equal(OperationAction.Delete, op);
                })
                .Returns(() => new List<AccountOperationRecord>
                {
                    new AccountOperationRecord { Operation = opEntity, Account = refAccount }
                })
                .Verifiable();
            opServiceMock.InSequence(sequence)
                .Setup(s => s.GetAccountOperationRecordsBatch(
                    It.Is<string>(op => op == OperationAction.Delete), It.IsAny<int>()))
                .Callback<string, int>((op, chunkSize) =>
                {
                    Assert.Equal(bgJobOptions.Value.Chunk, chunkSize);
                    Assert.Equal(OperationAction.Delete, op);
                })
                .Returns(() => new List<AccountOperationRecord>())
                .Verifiable();
            opServiceMock.Setup(s => s.UpdateOperationStatusListASync(It.IsAny<string>(), It.IsAny<List<RegOperationEntity>>()))
                .Returns(Task.CompletedTask);
            opServiceMock.Setup(s => s.TryToProceedUntilTimeoutAsync(OperationCategory.ACCOUNT, OperationAction.Delete))
                .Returns(Task.CompletedTask);

            var notificationManagerMock = new Mock<INotificationManager>();
            notificationManagerMock.Setup(nm => nm.BulkPublishAsync(It.IsAny<List<ServiceBusMessage>>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var dummyMessage = new ServiceBusMessage("dummy");
            var messageFactoryMock = new Mock<IServiceBusMessageFactory>();
            messageFactoryMock.Setup(mf => mf.CreateMessage(
                    It.IsAny<RegistryAccountRemovedEvent>(), It.IsAny<string>()))
                .Callback((BaseEvent<RegistryAccountRemovedEventData> evt, string s) =>
                {
                    var data = evt.Data;
                    Assert.Equal(refAccount.AccountNumber, data.AccountNumber);
                    Assert.Equal(accountEntity.AccountGlobalUniqueId, data.AccountGlobalUniqueIdentifier);
                })
                .Returns(dummyMessage);

            var orchestrator = CreateOrchestrator(context, opServiceMock.Object, notificationManagerMock.Object, messageFactoryMock.Object, bgJobOptions);

            // Act
            await orchestrator.ProcessAccountPublishAsync(OperationAction.Delete);

            // Assert
            messageFactoryMock.Verify(mf => mf.CreateMessage(
                It.IsAny<RegistryAccountRemovedEvent>(), It.IsAny<string>()), Times.AtLeastOnce);
            opServiceMock.Verify(s => s.GetAccountOperationRecordsBatch(OperationAction.Delete, bgJobOptions.Value.Chunk), Times.AtLeastOnce);
            opServiceMock.Verify(s => s.UpdateOperationStatusListASync(ProcessStatus.Sent, It.IsAny<List<RegOperationEntity>>()), Times.Once);
            opServiceMock.Verify(s => s.TryToProceedUntilTimeoutAsync(OperationCategory.ACCOUNT, OperationAction.Delete), Times.Once);
            notificationManagerMock.Verify(nm => nm.BulkPublishAsync(It.IsAny<List<ServiceBusMessage>>(), It.IsAny<string>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task ProcessAccountPublishAsync_UpdateBranch_Should_CreateUpdatedEventAndCallTimeout()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(ProcessAccountPublishAsync_UpdateBranch_Should_CreateUpdatedEventAndCallTimeout));
            using var context = new RefContext(options);

            var opEntity = new RegOperationEntity
            {
                Id = 3,
                Operation = OperationAction.Update,
                CreationDate = DateTime.UtcNow,
                EntityId = Guid.NewGuid(),
                LastStatusApprovalDate = DateTime.UtcNow,
                LastStatusApprovalBy = "update@test.com",
                PublishedAt = null,
                ApprovalStatus = ApprovalStatus.Approved,
                Type = OperationCategory.ACCOUNT,
                ProcessStatus = ProcessStatus.Ready
            };
            context.RegOperationEntity.Add(opEntity);

            var refAccount = new RefAccountEntity
            {
                EntityId = opEntity.EntityId,
                AccountNumber = "ACC003",
                LegalName = "Test Account Update",
                OperationType = OperationAction.Update
            };
            context.RefAccountEntity.Add(refAccount);

            context.AccountEntities.Add(new Domain.Entities.Accounts.AccountEntity
            {
                AccountId = 3,
                AccountNumber = "ACC003",
                AccountGlobalUniqueId = Guid.NewGuid()
            });
            await context.SaveChangesAsync();

            var bgJobOptions = Microsoft.Extensions.Options.Options.Create(new BackGroundJobOptions { Chunk = 1 });
            var opServiceMock = new Mock<IOperationService>();
            var sequence = new MockSequence();
            opServiceMock.InSequence(sequence)
                .Setup(s => s.GetAccountOperationRecordsBatch(
                    It.Is<string>(op => op == OperationAction.Update), It.IsAny<int>()))
                .Callback<string, int>((op, chunkSize) =>
                {
                    Assert.Equal(bgJobOptions.Value.Chunk, chunkSize);
                    Assert.Equal(OperationAction.Update, op);
                })
                .Returns(() => new List<AccountOperationRecord>
                {
                    new AccountOperationRecord { Operation = opEntity, Account = refAccount }
                })
                .Verifiable();
            opServiceMock.InSequence(sequence)
                .Setup(s => s.GetAccountOperationRecordsBatch(
                    It.Is<string>(op => op == OperationAction.Update), It.IsAny<int>()))
                .Callback<string, int>((op, chunkSize) =>
                {
                    Assert.Equal(bgJobOptions.Value.Chunk, chunkSize);
                    Assert.Equal(OperationAction.Update, op);
                })
                .Returns(() => new List<AccountOperationRecord>())
                .Verifiable();
            opServiceMock.Setup(s => s.UpdateOperationStatusListASync(It.IsAny<string>(), It.IsAny<List<RegOperationEntity>>()))
                .Returns(Task.CompletedTask);
            opServiceMock.Setup(s => s.TryToProceedUntilTimeoutAsync(OperationCategory.ACCOUNT, OperationAction.Update))
                .Returns(Task.CompletedTask);

            var notificationManagerMock = new Mock<INotificationManager>();
            var dummyMessage = new ServiceBusMessage("dummy");
            var messageFactoryMock = new Mock<IServiceBusMessageFactory>();
            messageFactoryMock.Setup(mf => mf.CreateMessage(
                It.IsAny<RegistryAccountUpdatedEvent>(), It.IsAny<string>()))
                .Callback<BaseEvent<RegistryAccountUpdatedEventData>, string>((evt, s) =>
                {
                    var data = evt.Data;
                    Assert.Equal(refAccount.AccountNumber, data.AccountNumber);
                    Assert.Equal(refAccount.LegalName, data.AccountLegalName);
                    Assert.Equal(refAccount.AccountType, data.AccountType);
                    Assert.Equal(refAccount.AccountEmail, data.AccountEmail);
                    Assert.Equal(refAccount.AccountNafIdentifier, data.AccountNafIdentifier);
                })
                .Returns(dummyMessage);

            var orchestrator = CreateOrchestrator(context, opServiceMock.Object, notificationManagerMock.Object, messageFactoryMock.Object, bgJobOptions);

            // Act
            await orchestrator.ProcessAccountPublishAsync(OperationAction.Update);

            // Assert
            messageFactoryMock.Verify(mf => mf.CreateMessage(
                It.IsAny<RegistryAccountUpdatedEvent>(), It.IsAny<string>()), Times.AtLeastOnce);
            opServiceMock.Verify(s => s.TryToProceedUntilTimeoutAsync(OperationCategory.ACCOUNT, OperationAction.Update), Times.Once);
        }

        [Fact]
        public async Task ProcessAccountPublishAsync_NoBatchFound_Should_CallTimeoutOnly()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(ProcessAccountPublishAsync_NoBatchFound_Should_CallTimeoutOnly));
            using var context = new RefContext(options);

            var bgJobOptions = Microsoft.Extensions.Options.Options.Create(new BackGroundJobOptions { Chunk = 1 });
            var opServiceMock = new Mock<IOperationService>();
            opServiceMock.Setup(s => s.GetAccountOperationRecordsBatch(It.IsAny<string>(), It.IsAny<int>()))
                .Returns(new List<AccountOperationRecord>());
            opServiceMock.Setup(s => s.TryToProceedUntilTimeoutAsync(OperationCategory.ACCOUNT, It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var notificationManagerMock = new Mock<INotificationManager>();
            var messageFactoryMock = new Mock<IServiceBusMessageFactory>();
            var orchestrator = CreateOrchestrator(context, opServiceMock.Object, notificationManagerMock.Object, messageFactoryMock.Object, bgJobOptions);

            // Act
            await orchestrator.ProcessAccountPublishAsync(OperationAction.Insert);

            // Assert
            opServiceMock.Verify(s => s.TryToProceedUntilTimeoutAsync(OperationCategory.ACCOUNT, OperationAction.Insert), Times.Once);
        }

        #endregion
    }
}

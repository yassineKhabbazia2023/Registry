using Application.Consts;
using Application.Interfaces;
using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Pulse.Registry.Domain.Context;
using Pulse.Registry.Domain.Entities;
using Registry.Application.Consts;
using Infrastructure.Orchestrators;
using Application.Models;
using Pulse.Back.Events.Abstractions;
using Pulse.Back.Events.IntegrationEvents;
using Registry.Infrastructure.Managers;

namespace Registry.Infrastructure.Tests.Orchestrators
{
    public class ContactOrchestratorTests
    {
        private DbContextOptions<RefContext> CreateInMemoryOptions(string databaseName)
        {
            return new DbContextOptionsBuilder<RefContext>()
                .UseInMemoryDatabase($"{databaseName}_{Guid.NewGuid()}")
                .Options;
        }

        private ContactOrchestrator CreateOrchestrator(
            RefContext context,
            IOperationService operationService,
            INotificationManager notificationManager,
            IServiceBusMessageFactory messageFactory)
        {
            var loggerMock = new Mock<ILogger<ContactOrchestrator>>();
            return new ContactOrchestrator(
                loggerMock.Object,
                context,
                notificationManager,
                messageFactory,
                operationService);
        }

        #region ProcessContactPublishAsync Tests

        [Fact]
        public async Task ProcessContactPublishAsync_InsertBranch_Should_CreateCreatedEventAndUpdateStatus()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(ProcessContactPublishAsync_InsertBranch_Should_CreateCreatedEventAndUpdateStatus));
            using var context = new RefContext(options);

            // Seed an operation record for INSERT.
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
                Type = OperationCategory.CONTACT,
                ProcessStatus = ProcessStatus.Ready
            };
            context.RegOperationEntity.Add(opEntity);

            // Seed a corresponding RefContactEntity.
            var refContact = new RefContactEntity
            {
                EntityId = opEntity.EntityId,
                Email = "insertcontact@test.com",
                FirstName = "Insert",
                LastName = "Contact",
                OperationType = OperationAction.Insert,
                IsCustomer = true
            };
            context.RefContactEntity.Add(refContact);

            // Also seed a ContactEntity for delete/update tests (not used in INSERT branch).
            context.ContactEntities.Add(new Domain.Entities.Contacts.ContactEntity
            {
                ContactId = 1,
                Email = "insertcontact@test.com",
                FirstName = "Insert",
                LastName = "Contact",
                Type = "Customer"
            });
            await context.SaveChangesAsync();

            // Setup the operation service mock.
            var opServiceMock = new Mock<IOperationService>();
            opServiceMock.Setup(s => s.GeContactOperationRecords(It.Is<string>(op => op == OperationAction.Insert)))
                .Returns(() => new List<ContactOperationRecord>
                {
                    new ContactOperationRecord { Operation = opEntity, RefContactEntity = refContact }
                });
            opServiceMock.Setup(s => s.UpdateOperationStatusListASync(ProcessStatus.Sent, It.IsAny<List<RegOperationEntity>>()))
                .Returns(Task.CompletedTask);
            opServiceMock.Setup(s => s.TryToProceedUntilTimeoutAsync(OperationCategory.CONTACT, OperationAction.Insert))
                .Returns(Task.CompletedTask);

            var notificationManagerMock = new Mock<INotificationManager>();
            notificationManagerMock.Setup(nm => nm.BulkPublishAsync(It.IsAny<List<ServiceBusMessage>>(),It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var dummyMessage = new ServiceBusMessage("dummy");
            var messageFactoryMock = new Mock<IServiceBusMessageFactory>();
            messageFactoryMock.Setup(mf => mf.CreateMessage(
                    It.Is<RegistryContactCreatedEvent>(e =>
                        e.Data.Email == refContact.Email &&
                        e.Data.FirstName == refContact.FirstName &&
                        e.Data.LastName == refContact.LastName &&
                        e.Data.Source == "AKUITEO"),
                    It.IsAny<string>()))
                .Returns(dummyMessage);

            var orchestrator = CreateOrchestrator(context, opServiceMock.Object, notificationManagerMock.Object, messageFactoryMock.Object);

            // Act
            await orchestrator.ProcessContactPublishAsync(OperationAction.Insert);

            // Assert
            messageFactoryMock.Verify(mf => mf.CreateMessage(
                It.IsAny<RegistryContactCreatedEvent>(), It.IsAny<string>()), Times.AtLeastOnce);
            opServiceMock.Verify(s => s.GeContactOperationRecords(OperationAction.Insert), Times.Once);
            opServiceMock.Verify(s => s.UpdateOperationStatusListASync(ProcessStatus.Sent, It.IsAny<List<RegOperationEntity>>()), Times.Once);
            opServiceMock.Verify(s => s.TryToProceedUntilTimeoutAsync(OperationCategory.CONTACT, OperationAction.Insert), Times.Once);
            notificationManagerMock.Verify(nm => nm.BulkPublishAsync(It.IsAny<List<ServiceBusMessage>>(), It.IsAny<string>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task ProcessContactPublishAsync_DeleteBranch_Should_CreateRemovedEvent_When_ContactExists()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(ProcessContactPublishAsync_DeleteBranch_Should_CreateRemovedEvent_When_ContactExists));
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
                Type = OperationCategory.CONTACT,
                ProcessStatus = ProcessStatus.Ready
            };
            context.RegOperationEntity.Add(opEntity);

            var refContact = new RefContactEntity
            {
                EntityId = opEntity.EntityId,
                Email = "deletecontact@test.com",
                FirstName = "Delete",
                LastName = "Contact",
                OperationType = OperationAction.Delete,
                IsCustomer = false
            };
            context.RefContactEntity.Add(refContact);

            // Seed a matching ContactEntity for deletion.
            context.ContactEntities.Add(new Domain.Entities.Contacts.ContactEntity
            {
                ContactId = 2,
                Email = "deletecontact@test.com",
                FirstName = "Delete",
                LastName = "Contact",
                Type = "Customer",
                ContactGlobalUniqueId = Guid.NewGuid()
            });
            await context.SaveChangesAsync();

            var opServiceMock = new Mock<IOperationService>();
            opServiceMock.Setup(s => s.GeContactOperationRecords(It.Is<string>(op => op == OperationAction.Delete)))
                .Returns(() => new List<ContactOperationRecord>
                {
                    new ContactOperationRecord { Operation = opEntity, RefContactEntity = refContact }
                });
            opServiceMock.Setup(s => s.UpdateOperationStatusListASync(ProcessStatus.Sent, It.IsAny<List<RegOperationEntity>>()))
                .Returns(Task.CompletedTask);
            opServiceMock.Setup(s => s.TryToProceedUntilTimeoutAsync(OperationCategory.CONTACT, OperationAction.Delete))
                .Returns(Task.CompletedTask);

            var notificationManagerMock = new Mock<INotificationManager>();
            var dummyMessage = new ServiceBusMessage("dummy");
            var messageFactoryMock = new Mock<IServiceBusMessageFactory>();
            messageFactoryMock.Setup(mf => mf.CreateMessage(
                    It.Is<RegistryContactRemovedEvent>(e =>
                        e.Data.Email == refContact.Email),
                    It.IsAny<string>()))
                .Returns(dummyMessage);

            var orchestrator = CreateOrchestrator(context, opServiceMock.Object, notificationManagerMock.Object, messageFactoryMock.Object);

            // Act
            await orchestrator.ProcessContactPublishAsync(OperationAction.Delete);

            // Assert
            messageFactoryMock.Verify(mf => mf.CreateMessage(
                It.IsAny<RegistryContactRemovedEvent>(), It.IsAny<string>()), Times.AtLeastOnce);
            opServiceMock.Verify(s => s.GeContactOperationRecords(OperationAction.Delete), Times.Once);
            opServiceMock.Verify(s => s.TryToProceedUntilTimeoutAsync(OperationCategory.CONTACT, OperationAction.Delete), Times.Once);
            notificationManagerMock.Verify(nm => nm.BulkPublishAsync(It.IsAny<List<ServiceBusMessage>>(), It.IsAny<string>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task ProcessContactPublishAsync_UpdateBranch_Should_CreateUpdatedEventAndCallTimeout()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(ProcessContactPublishAsync_UpdateBranch_Should_CreateUpdatedEventAndCallTimeout));
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
                Type = OperationCategory.CONTACT,
                ProcessStatus = ProcessStatus.Ready,
                OldContactEmail = "oldupdate@test.com"
            };
            context.RegOperationEntity.Add(opEntity);

            var refContact = new RefContactEntity
            {
                EntityId = opEntity.EntityId,
                Email = "update@test.com",
                FirstName = "Update",
                LastName = "Contact",
                OperationType = OperationAction.Update,
                IsCustomer = true,
                OfficeCode = "OC123",
                LandPhone = "111-222-3333",
                MobilePhone = "444-555-6666",
                JobDescription = "Tester"
            };
            context.RefContactEntity.Add(refContact);

            // Seed a ContactEntity with the old email (to be used for update lookup).
            context.ContactEntities.Add(new Domain.Entities.Contacts.ContactEntity
            {
                ContactId = 3,
                Email = "oldupdate@test.com",
                FirstName = "OldUpdate",
                LastName = "Contact",
                Type = "Customer",
                ContactGlobalUniqueId = Guid.NewGuid()
            });
            await context.SaveChangesAsync();

            var opServiceMock = new Mock<IOperationService>();
            opServiceMock.Setup(s => s.GeContactOperationRecords(It.Is<string>(op => op == OperationAction.Update)))
                .Returns(() => new List<ContactOperationRecord>
                {
                    new ContactOperationRecord { Operation = opEntity, RefContactEntity = refContact }
                });
            opServiceMock.Setup(s => s.UpdateOperationStatusListASync(ProcessStatus.Sent, It.IsAny<List<RegOperationEntity>>()))
                .Returns(Task.CompletedTask);
            opServiceMock.Setup(s => s.TryToProceedUntilTimeoutAsync(OperationCategory.CONTACT, OperationAction.Update))
                .Returns(Task.CompletedTask);

            var notificationManagerMock = new Mock<INotificationManager>();
            var dummyMessage = new ServiceBusMessage("dummy");
            var messageFactoryMock = new Mock<IServiceBusMessageFactory>();
            messageFactoryMock.Setup(mf => mf.CreateMessage(
                    It.Is<RegistryContactUpdatedEvent>(e =>
                        e.Data.Email == refContact.Email &&
                        e.Data.FirstName == refContact.FirstName &&
                        e.Data.LastName == refContact.LastName &&
                        e.Data.OfficeCode == refContact.OfficeCode &&
                        e.Data.JobDescription == refContact.JobDescription &&
                        e.Data.IsCustomer == (refContact.IsCustomer ?? false)),
                    It.IsAny<string>()))
                .Returns(dummyMessage);

            var orchestrator = CreateOrchestrator(context, opServiceMock.Object, notificationManagerMock.Object, messageFactoryMock.Object);

            // Act
            await orchestrator.ProcessContactPublishAsync(OperationAction.Update);

            // Assert
            messageFactoryMock.Verify(mf => mf.CreateMessage(
                It.IsAny<RegistryContactUpdatedEvent>(), It.IsAny<string>()), Times.AtLeastOnce);
            opServiceMock.Verify(s => s.TryToProceedUntilTimeoutAsync(OperationCategory.CONTACT, OperationAction.Update), Times.Once);
        }

        [Fact]
        public async Task ProcessContactPublishAsync_UpdateBranch_ContactNotFound_Should_Not_CreateUpdatedEvent()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(ProcessContactPublishAsync_UpdateBranch_ContactNotFound_Should_Not_CreateUpdatedEvent));
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
                Type = OperationCategory.CONTACT,
                ProcessStatus = ProcessStatus.Ready,
                OldContactEmail = "oldupdate@test.com"
            };
            context.RegOperationEntity.Add(opEntity);

            var refContact = new RefContactEntity
            {
                EntityId = opEntity.EntityId,
                Email = "update@test.com",
                FirstName = "Update",
                LastName = "Contact",
                OperationType = OperationAction.Update,
                IsCustomer = true,
                OfficeCode = "OC123",
                LandPhone = "111-222-3333",
                MobilePhone = "444-555-6666",
                JobDescription = "Tester"
            };
            context.RefContactEntity.Add(refContact);

            var opServiceMock = new Mock<IOperationService>();
            opServiceMock.Setup(s => s.GeContactOperationRecords(It.Is<string>(op => op == OperationAction.Update)))
                .Returns(() => new List<ContactOperationRecord>
                {
                    new ContactOperationRecord { Operation = opEntity, RefContactEntity = refContact }
                });
            opServiceMock.Setup(s => s.UpdateOperationStatusListASync(ProcessStatus.Sent, It.IsAny<List<RegOperationEntity>>()))
                .Returns(Task.CompletedTask);
            opServiceMock.Setup(s => s.TryToProceedUntilTimeoutAsync(OperationCategory.CONTACT, OperationAction.Update))
                .Returns(Task.CompletedTask);

            var notificationManagerMock = new Mock<INotificationManager>();
            var dummyMessage = new ServiceBusMessage("dummy");
            var messageFactoryMock = new Mock<IServiceBusMessageFactory>();
            messageFactoryMock.Setup(mf => mf.CreateMessage(
                    It.Is<RegistryContactUpdatedEvent>(e =>
                        e.Data.Email == refContact.Email &&
                        e.Data.FirstName == refContact.FirstName &&
                        e.Data.LastName == refContact.LastName &&
                        e.Data.OfficeCode == refContact.OfficeCode &&
                        e.Data.JobDescription == refContact.JobDescription &&
                        e.Data.IsCustomer == (refContact.IsCustomer ?? false)),
                    It.IsAny<string>()))
                .Returns(dummyMessage);

            var orchestrator = CreateOrchestrator(context, opServiceMock.Object, notificationManagerMock.Object, messageFactoryMock.Object);

            // Act
            await orchestrator.ProcessContactPublishAsync(OperationAction.Update);

            // Assert
            messageFactoryMock.Verify(mf => mf.CreateMessage(
                It.IsAny<RegistryContactUpdatedEvent>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ProcessContactPublishAsync_NoBatchFound_Should_CallTimeoutOnly()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(ProcessContactPublishAsync_NoBatchFound_Should_CallTimeoutOnly));
            using var context = new RefContext(options);

            var opServiceMock = new Mock<IOperationService>();
            opServiceMock.Setup(s => s.GeContactOperationRecords(It.IsAny<string>()))
                .Returns(() => new List<ContactOperationRecord>());
            opServiceMock.Setup(s => s.TryToProceedUntilTimeoutAsync(OperationCategory.CONTACT, It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var notificationManagerMock = new Mock<INotificationManager>();
            var messageFactoryMock = new Mock<IServiceBusMessageFactory>();

            var orchestrator = CreateOrchestrator(context, opServiceMock.Object, notificationManagerMock.Object, messageFactoryMock.Object);

            // Act
            await orchestrator.ProcessContactPublishAsync(OperationAction.Insert);

            // Assert
            opServiceMock.Verify(s => s.TryToProceedUntilTimeoutAsync(OperationCategory.CONTACT, OperationAction.Insert), Times.Once);
        }

        #endregion

        #region ProcessContactOperation Tests

        [Fact]
        public void ProcessContactOperation_Insert_Should_ReturnCreatedEventMessage()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(ProcessContactOperation_Insert_Should_ReturnCreatedEventMessage));
            using var context = new RefContext(options);

            var opEntity = new RegOperationEntity
            {
                Id = 10,
                Operation = OperationAction.Insert,
                CreationDate = DateTime.UtcNow,
                EntityId = Guid.NewGuid(),
                ApprovalStatus = ApprovalStatus.Approved,
                Type = OperationCategory.CONTACT,
                ProcessStatus = ProcessStatus.Ready
            };

            var refContact = new RefContactEntity
            {
                EntityId = opEntity.EntityId,
                Email = "insertOp@test.com",
                FirstName = "InsertFirst",
                LastName = "InsertLast",
                OperationType = OperationAction.Insert,
                IsCustomer = true,
                OfficeCode = "OC001",
                LandPhone = "123-456",
                MobilePhone = "789-012",
                JobDescription = "Tester"
            };

            // Setup the message factory mock with callback to check event data properties.
            var dummyMessage = new ServiceBusMessage("dummy");
            var messageFactoryMock = new Mock<IServiceBusMessageFactory>();
            messageFactoryMock.Setup(mf => mf.CreateMessage(
                    It.Is<RegistryContactCreatedEvent>(e =>
                        e.Data.Email == refContact.Email &&
                        e.Data.FirstName == refContact.FirstName &&
                        e.Data.LastName == refContact.LastName &&
                        e.Data.Source == "AKUITEO"),
                    It.IsAny<string>()))
                .Returns(dummyMessage);

            var orchestrator = new ContactOrchestrator(
                new Mock<ILogger<ContactOrchestrator>>().Object,
                context,
                Mock.Of<INotificationManager>(),
                messageFactoryMock.Object,
                Mock.Of<IOperationService>());

            // Act
            var result = orchestrator.ProcessContactOperation(opEntity, refContact);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dummyMessage, result);
        }

        [Fact]
        public void ProcessContactOperation_Delete_Should_ReturnRemovedEventMessage_When_ContactExists()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(ProcessContactOperation_Delete_Should_ReturnRemovedEventMessage_When_ContactExists));
            using var context = new RefContext(options);

            var opEntity = new RegOperationEntity
            {
                Id = 20,
                Operation = OperationAction.Delete,
                CreationDate = DateTime.UtcNow,
                EntityId = Guid.NewGuid(),
                ApprovalStatus = ApprovalStatus.Approved,
                Type = OperationCategory.CONTACT,
                ProcessStatus = ProcessStatus.Ready
            };
            context.RegOperationEntity.Add(opEntity);

            var refContact = new RefContactEntity
            {
                EntityId = opEntity.EntityId,
                Email = "deleteOp@test.com",
                FirstName = "DeleteFirst",
                LastName = "DeleteLast",
                OperationType = OperationAction.Delete,
                IsCustomer = false
            };
            context.RefContactEntity.Add(refContact);

            // Seed a matching ContactEntity.
            var contactEntity = new Domain.Entities.Contacts.ContactEntity
            {
                ContactId = 30,
                Email = "deleteOp@test.com",
                FirstName = "DeleteFirst",
                LastName = "DeleteLast",
                Type = "Customer",
                ContactGlobalUniqueId = Guid.NewGuid()
            };
            context.ContactEntities.Add(contactEntity);
            context.SaveChanges();

            var dummyMessage = new ServiceBusMessage("dummy");
            var messageFactoryMock = new Mock<IServiceBusMessageFactory>();
            messageFactoryMock.Setup(mf => mf.CreateMessage(
                    It.Is<RegistryContactRemovedEvent>(e =>
                        e.Data.Email == refContact.Email),
                    It.IsAny<string>()))
                .Returns(dummyMessage);

            var orchestrator = new ContactOrchestrator(
                new Mock<ILogger<ContactOrchestrator>>().Object,
                context,
                Mock.Of<INotificationManager>(),
                messageFactoryMock.Object,
                Mock.Of<IOperationService>());

            // Act
            var result = orchestrator.ProcessContactOperation(opEntity, refContact);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dummyMessage, result);
        }

        [Fact]
        public void ProcessContactOperation_Delete_Should_ReturnNull_When_ContactDoesNotExist()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(ProcessContactOperation_Delete_Should_ReturnNull_When_ContactDoesNotExist));
            using var context = new RefContext(options);

            var opEntity = new RegOperationEntity
            {
                Id = 21,
                Operation = OperationAction.Delete,
                CreationDate = DateTime.UtcNow,
                EntityId = Guid.NewGuid(),
                ApprovalStatus = ApprovalStatus.Approved,
                Type = OperationCategory.CONTACT,
                ProcessStatus = ProcessStatus.Ready
            };
            context.RegOperationEntity.Add(opEntity);

            var refContact = new RefContactEntity
            {
                EntityId = opEntity.EntityId,
                Email = "nonexistent@test.com",
                FirstName = "Non",
                LastName = "Existent",
                OperationType = OperationAction.Delete,
                IsCustomer = false
            };
            context.RefContactEntity.Add(refContact);
            context.SaveChanges();

            var messageFactoryMock = new Mock<IServiceBusMessageFactory>();
            var orchestrator = new ContactOrchestrator(
                new Mock<ILogger<ContactOrchestrator>>().Object,
                context,
                Mock.Of<INotificationManager>(),
                messageFactoryMock.Object,
                Mock.Of<IOperationService>());

            // Act
            var result = orchestrator.ProcessContactOperation(opEntity, refContact);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ProcessContactOperation_Update_Should_ReturnUpdatedEventMessage()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(ProcessContactOperation_Update_Should_ReturnUpdatedEventMessage));
            using var context = new RefContext(options);

            var opEntity = new RegOperationEntity
            {
                Id = 31,
                Operation = OperationAction.Update,
                CreationDate = DateTime.UtcNow,
                EntityId = Guid.NewGuid(),
                ApprovalStatus = ApprovalStatus.Approved,
                Type = OperationCategory.CONTACT,
                ProcessStatus = ProcessStatus.Ready,
                OldContactEmail = "oldupdate@test.com"
            };
            context.RegOperationEntity.Add(opEntity);

            var refContact = new RefContactEntity
            {
                EntityId = opEntity.EntityId,
                Email = "update@test.com",
                FirstName = "UpdateFirst",
                LastName = "UpdateLast",
                OperationType = OperationAction.Update,
                IsCustomer = true,
                OfficeCode = "OCUPD",
                LandPhone = "222-333",
                MobilePhone = "444-555",
                JobDescription = "UpdateDesc"
            };
            context.RefContactEntity.Add(refContact);

            // Seed a matching ContactEntity with the old email.
            var contactEntity = new Domain.Entities.Contacts.ContactEntity
            {
                ContactId = 40,
                Email = "oldupdate@test.com",
                FirstName = "OldFirst",
                LastName = "OldLast",
                Type = "Customer",
                ContactGlobalUniqueId = Guid.NewGuid()
            };
            context.ContactEntities.Add(contactEntity);
            context.SaveChanges();

            var dummyMessage = new ServiceBusMessage("dummy");
            var messageFactoryMock = new Mock<IServiceBusMessageFactory>();
            messageFactoryMock.Setup(mf => mf.CreateMessage(
                    It.Is<RegistryContactUpdatedEvent>(e =>
                        e.Data.Email == refContact.Email &&
                        e.Data.FirstName == refContact.FirstName &&
                        e.Data.LastName == refContact.LastName &&
                        e.Data.OfficeCode == refContact.OfficeCode &&
                        e.Data.JobDescription == refContact.JobDescription &&
                        e.Data.IsCustomer == (refContact.IsCustomer ?? false) &&
                        e.Data.IsActive),
                    It.IsAny<string>()))
                .Returns(dummyMessage);

            var orchestrator = new ContactOrchestrator(
                new Mock<ILogger<ContactOrchestrator>>().Object,
                context,
                Mock.Of<INotificationManager>(),
                messageFactoryMock.Object,
                Mock.Of<IOperationService>());

            // Act
            var result = orchestrator.ProcessContactOperation(opEntity, refContact);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dummyMessage, result);
        }

        #endregion
    }
}

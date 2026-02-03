using Application.Consts;
using Application.Interfaces;
using Application.Requests;
using Application.Providers;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Pulse.Back.Events.IntegrationEvents;
using Pulse.Back.Events.IntegrationEvents.EventsData;
using Pulse.Registry.Domain.Entities;
using Application.Enums;
using Registry.Application.Consts;
using Application.Models;
using Pulse.Registry.Domain.Entities.Accounts;

namespace Registry.Infrastructure.Tests.Providers
{
    public class RoleCreatedEventHandlerTests
    {
        [Fact]
        public async Task HandleAsync_WithValidMessage_ShouldAddRoleAndUpdateOperationStatus()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<RoleCreatedEventHandler>>();
            var roleRegistryProviderMock = new Mock<IRoleRegistryProvider>(MockBehavior.Strict);
            var roleRepositoryMock = new Mock<IRoleRepository>();
            var operationRepositoryMock = new Mock<IOperationRepository>();

            roleRepositoryMock.Setup(r => r.AddRoleAsync(It.IsAny<RoleEntity>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var dummyOperations = new List<RegOperationEntity>
            {
                new RegOperationEntity { EntityId = Guid.NewGuid(), ApprovalStatus = ApprovalStatus.Approved, ProcessStatus = ProcessStatus.Sent }
            };
            operationRepositoryMock.Setup(op => op.FetchOperationsByCriteriaAsync(
                It.IsAny<OperationSearchCriteria>(),
                OperationStrategyType.ROLE,
                It.IsAny<string>(),
                It.IsAny<bool?>(),
                It.IsAny<string?>()))
                .ReturnsAsync(dummyOperations)
                .Verifiable();

            operationRepositoryMock.Setup(op => op.BulkUpdateOperationsStatusAsync(
                ProcessStatus.Succeeded.ToString(),
                It.IsAny<IEnumerable<RegOperationEntity>>()))
                .ReturnsAsync(true)
                .Verifiable();

            var eventData = new RoleCreatedEventData
            {
                AccountId = 22,
                AccountGlobalUniqueId = Guid.NewGuid(),
                ContactId = 123,
                ContactGlobalUniqueId = Guid.NewGuid(),
                IsSignatory = true,
                IsFavorite = false,
                IsDelegation = false,
                DelegatorContactId = null,
                AccountNumber = "199099090",
                ContactEmail = "email@test.fr",
                ContactFlagPortailFactures = true
            };
            var roleCreatedEvent = new RoleCreatedEvent(eventData);
            var message = JsonConvert.SerializeObject(roleCreatedEvent);

            var handler = new RoleCreatedEventHandler(
                loggerMock.Object,
                roleRegistryProviderMock.Object,
                roleRepositoryMock.Object,
                operationRepositoryMock.Object);

            // Act
            await handler.HandleAsync(message);

            // Assert
            roleRepositoryMock.Verify(r => r.AddRoleAsync(It.Is<RoleEntity>(r =>
                r.AccountId == eventData.AccountId &&
                r.AccountGlobalUniqueId == eventData.AccountGlobalUniqueId &&
                r.AccountNumber == eventData.AccountNumber &&
                r.ContactId == eventData.ContactId &&
                r.ContactGlobalUniqueId == eventData.ContactGlobalUniqueId &&
                r.ContactEmail == eventData.ContactEmail &&
                r.ContactFlagPortailFactures == eventData.ContactFlagPortailFactures
            )), Times.Once);

            operationRepositoryMock.Verify(op => op.FetchOperationsByCriteriaAsync(
                It.Is<OperationSearchCriteria>(c => c.OperationName == OperationAction.Insert),
                OperationStrategyType.ROLE,
                It.IsAny<string>(),
                It.IsAny<bool?>(),
                It.IsAny<string?>()), Times.Once);
            operationRepositoryMock.Verify(op => op.BulkUpdateOperationsStatusAsync(
                ProcessStatus.Succeeded.ToString(),
                It.IsAny<IEnumerable<RegOperationEntity>>()), Times.Once);
            roleRegistryProviderMock.Verify(x => x.CreateRoleAsync(It.IsAny<RoleRegistry>()), Times.Never);
        }

        [Fact]
        public async Task HandleAsync_WithExistingRoleAndDifferentContactFlag_ShouldUpdatePulseRole()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<RoleCreatedEventHandler>>();
            var roleRegistryProviderMock = new Mock<IRoleRegistryProvider>(MockBehavior.Strict);
            var roleRepositoryMock = new Mock<IRoleRepository>();
            var operationRepositoryMock = new Mock<IOperationRepository>();

            roleRepositoryMock.Setup(r => r.AddRoleAsync(It.IsAny<RoleEntity>()))
                .Returns(Task.CompletedTask);

            var existingRole = new RoleEntity
            {
                AccountId = 22,
                ContactId = 123,
                AccountNumber = "199099090",
                ContactEmail = "email@test.fr",
                ContactFlagPortailFactures = false
            };

            roleRepositoryMock.Setup(r => r.GetPulseRole(existingRole.ContactEmail!, existingRole.AccountNumber!))
                .ReturnsAsync(existingRole);

            roleRepositoryMock.Setup(r => r.UpdatePulseRole(It.IsAny<RoleEntity>()))
                .ReturnsAsync(true);

            operationRepositoryMock.Setup(op => op.FetchOperationsByCriteriaAsync(
                    It.IsAny<OperationSearchCriteria>(),
                    OperationStrategyType.ROLE,
                    It.IsAny<string>(),
                    It.IsAny<bool?>(),
                    It.IsAny<string?>()))
                .ReturnsAsync(new List<RegOperationEntity>
                {
                    new()
                    {
                        EntityId = Guid.NewGuid(),
                        ApprovalStatus = ApprovalStatus.Approved,
                        ProcessStatus = ProcessStatus.Ready,
                        Operation = OperationAction.Insert,
                    }
                });

            operationRepositoryMock.Setup(op => op.BulkUpdateOperationsStatusAsync(
                    ProcessStatus.Succeeded.ToString(),
                    It.IsAny<IEnumerable<RegOperationEntity>>()))
                .ReturnsAsync(true);

            var eventData = new RoleCreatedEventData
            {
                AccountId = 22,
                AccountGlobalUniqueId = Guid.NewGuid(),
                ContactId = 123,
                ContactGlobalUniqueId = Guid.NewGuid(),
                IsSignatory = true,
                IsFavorite = false,
                IsDelegation = false,
                DelegatorContactId = null,
                AccountNumber = "199099090",
                ContactEmail = "email@test.fr",
                ContactFlagPortailFactures = true
            };
            var roleCreatedEvent = new RoleCreatedEvent(eventData);
            var message = JsonConvert.SerializeObject(roleCreatedEvent);

            var handler = new RoleCreatedEventHandler(
                loggerMock.Object,
                roleRegistryProviderMock.Object,
                roleRepositoryMock.Object,
                operationRepositoryMock.Object);

            // Act
            await handler.HandleAsync(message);

            // Assert
            roleRepositoryMock.Verify(r => r.UpdatePulseRole(It.Is<RoleEntity>(r =>
                r.AccountId == existingRole.AccountId &&
                r.ContactId == existingRole.ContactId &&
                r.ContactFlagPortailFactures == true
            )), Times.Once);
            roleRepositoryMock.Verify(r => r.AddRoleAsync(It.IsAny<RoleEntity>()), Times.Never);
            operationRepositoryMock.Verify(op => op.FetchOperationsByCriteriaAsync(
                It.Is<OperationSearchCriteria>(c => c.OperationName == OperationAction.Insert),
                OperationStrategyType.ROLE,
                It.IsAny<string>(),
                It.IsAny<bool?>(),
                It.IsAny<string?>()), Times.Once);
            operationRepositoryMock.Verify(op => op.BulkUpdateOperationsStatusAsync(
                ProcessStatus.Succeeded.ToString(),
                It.IsAny<IEnumerable<RegOperationEntity>>()), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_WithNullMessage_ShouldLogErrorAndNotAddRole()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<RoleCreatedEventHandler>>();
            var roleRegistryProviderMock = new Mock<IRoleRegistryProvider>(MockBehavior.Strict);
            var roleRepositoryMock = new Mock<IRoleRepository>();
            var operationRepositoryMock = new Mock<IOperationRepository>();

            var handler = new RoleCreatedEventHandler(
                loggerMock.Object,
                roleRegistryProviderMock.Object,
                roleRepositoryMock.Object,
                operationRepositoryMock.Object);

            // Act
            await handler.HandleAsync(null!);

            // Assert
            roleRepositoryMock.Verify(r => r.AddRoleAsync(It.IsAny<RoleEntity>()), Times.Never);
            operationRepositoryMock.Verify(op => op.FetchOperationsByCriteriaAsync(
                It.IsAny<OperationSearchCriteria>(),
                It.IsAny<OperationStrategyType>(),
                It.IsAny<string>(),
                It.IsAny<bool?>(),
                It.IsAny<string?>()), Times.Never);
        }

        [Fact]
        public async Task HandleAsync_WithInvalidData_ShouldLogErrorAndNotAddRole()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<RoleCreatedEventHandler>>();
            var roleRegistryProviderMock = new Mock<IRoleRegistryProvider>(MockBehavior.Strict);
            var roleRepositoryMock = new Mock<IRoleRepository>();
            var operationRepositoryMock = new Mock<IOperationRepository>();

            var invalidEventData = new RoleCreatedEventData
            {
                AccountId = 22,
                AccountGlobalUniqueId = Guid.NewGuid(),
                ContactId = 0, // Invalid
                ContactGlobalUniqueId = Guid.NewGuid(),
                IsSignatory = true,
                IsFavorite = false,
                IsDelegation = false,
                DelegatorContactId = null,
                AccountNumber = "199099090",
                ContactEmail = "email@test.fr",
                ContactFlagPortailFactures = true
            };
            var invalidEvent = new RoleCreatedEvent(invalidEventData);
            var message = JsonConvert.SerializeObject(invalidEvent);

            var handler = new RoleCreatedEventHandler(
                loggerMock.Object,
                roleRegistryProviderMock.Object,
                roleRepositoryMock.Object,
                operationRepositoryMock.Object);

            // Act
            await handler.HandleAsync(message);

            // Assert
            roleRepositoryMock.Verify(r => r.AddRoleAsync(It.IsAny<RoleEntity>()), Times.Never);
            operationRepositoryMock.Verify(op => op.FetchOperationsByCriteriaAsync(
                It.IsAny<OperationSearchCriteria>(),
                It.IsAny<OperationStrategyType>(),
                It.IsAny<string>(),
                It.IsAny<bool?>(),
                It.IsAny<string?>()), Times.Never);
        }

        [Fact]
        public async Task HandleAsync_WithNoOperationsFound_DoesNotCallBulkUpdate()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<RoleCreatedEventHandler>>();
            var roleRegistryProviderMock = new Mock<IRoleRegistryProvider>(MockBehavior.Strict);
            var roleRepositoryMock = new Mock<IRoleRepository>();
            var operationRepositoryMock = new Mock<IOperationRepository>();

            roleRepositoryMock.Setup(r => r.AddRoleAsync(It.IsAny<RoleEntity>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            operationRepositoryMock.Setup(op => op.FetchOperationsByCriteriaAsync(
                It.IsAny<OperationSearchCriteria>(),
                OperationStrategyType.ROLE,
                It.IsAny<string>(),
                It.IsAny<bool?>(),
                It.IsAny<string?>()))
                .ReturnsAsync(new List<RegOperationEntity>())
                .Verifiable();

            var validEventData = new RoleCreatedEventData
            {
                AccountId = 22,
                AccountGlobalUniqueId = Guid.NewGuid(),
                ContactId = 123,
                ContactGlobalUniqueId = Guid.NewGuid(),
                IsSignatory = true,
                IsFavorite = false,
                IsDelegation = false,
                DelegatorContactId = null,
                AccountNumber = "199099090",
                ContactEmail = "email@test.fr",
                ContactFlagPortailFactures = true
            };
            var validEvent = new RoleCreatedEvent(validEventData);
            var message = JsonConvert.SerializeObject(validEvent);

            var handler = new RoleCreatedEventHandler(
                loggerMock.Object,
                roleRegistryProviderMock.Object,
                roleRepositoryMock.Object,
                operationRepositoryMock.Object);

            // Act
            await handler.HandleAsync(message);

            // Assert
            roleRepositoryMock.Verify(r => r.AddRoleAsync(It.IsAny<RoleEntity>()), Times.Once);
            operationRepositoryMock.Verify(op => op.FetchOperationsByCriteriaAsync(
                It.IsAny<OperationSearchCriteria>(),
                OperationStrategyType.ROLE,
                It.IsAny<string>(),
                It.IsAny<bool?>(),
                It.IsAny<string?>()), Times.Once);
            operationRepositoryMock.Verify(op => op.BulkUpdateOperationsStatusAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<RegOperationEntity>>()), Times.Never);
        }
    }
}

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
                operationRepositoryMock.Object,
                Mock.Of<IFeatureFlagService>(),
                Mock.Of<IContactAkuiteoSynchronizer>());

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
                operationRepositoryMock.Object,
                Mock.Of<IFeatureFlagService>(),
                Mock.Of<IContactAkuiteoSynchronizer>());

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
                operationRepositoryMock.Object,
                Mock.Of<IFeatureFlagService>(),
                Mock.Of<IContactAkuiteoSynchronizer>());

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
                operationRepositoryMock.Object,
                Mock.Of<IFeatureFlagService>(),
                Mock.Of<IContactAkuiteoSynchronizer>());

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
                operationRepositoryMock.Object,
                Mock.Of<IFeatureFlagService>(),
                Mock.Of<IContactAkuiteoSynchronizer>());

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

        /// <summary>
        /// Ensures role events invoke Akuiteo synchronization when the feature is enabled.
        /// </summary>
        /// <param name="accountType">The account type carried by the event.</param>
        [Theory]
        [InlineData(AccountTypes.Client)]
        [InlineData("prospect")]
        public async Task HandleAsync_WithEnabledFlag_ShouldSynchronizeContact(
            string accountType)
        {
            // Arrange
            var roleRepositoryMock = new Mock<IRoleRepository>();
            var operationRepositoryMock = new Mock<IOperationRepository>();
            var featureFlagServiceMock = new Mock<IFeatureFlagService>();
            var contactAkuiteoSynchronizerMock = new Mock<IContactAkuiteoSynchronizer>();
            var eventData = CreateValidEventData();
            var roleEvent = new RoleCreatedEvent(eventData) { AccountType = accountType };

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
            featureFlagServiceMock
                .Setup(service => service.IsEnabled(FeatureFlagKeys.IsContactAkuiteoSynchronizationEnabled))
                .Returns(true);

            var handler = CreateHandler(
                roleRepositoryMock,
                operationRepositoryMock,
                featureFlagServiceMock,
                contactAkuiteoSynchronizerMock);

            // Act
            await handler.HandleAsync(JsonConvert.SerializeObject(roleEvent));

            // Assert
            contactAkuiteoSynchronizerMock.Verify(
                synchronizer => synchronizer.SynchronizeAsync(
                    It.Is<RoleCreatedEventData>(data =>
                        data.ContactId == eventData.ContactId
                        && data.AccountNumber == eventData.AccountNumber
                        && data.ContactEmail == eventData.ContactEmail),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        /// <summary>
        /// Ensures a disabled feature flag preserves role processing without calling Akuiteo.
        /// </summary>
        [Fact]
        public async Task HandleAsync_WithDisabledFlag_ShouldNotSynchronizeContact()
        {
            // Arrange
            var roleRepositoryMock = new Mock<IRoleRepository>();
            var operationRepositoryMock = new Mock<IOperationRepository>();
            var featureFlagServiceMock = new Mock<IFeatureFlagService>();
            var contactAkuiteoSynchronizerMock = new Mock<IContactAkuiteoSynchronizer>();
            var roleEvent = new RoleCreatedEvent(CreateValidEventData())
            {
                AccountType = AccountTypes.Client
            };

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
            featureFlagServiceMock
                .Setup(service => service.IsEnabled(FeatureFlagKeys.IsContactAkuiteoSynchronizationEnabled))
                .Returns(false);

            var handler = CreateHandler(
                roleRepositoryMock,
                operationRepositoryMock,
                featureFlagServiceMock,
                contactAkuiteoSynchronizerMock);

            // Act
            await handler.HandleAsync(JsonConvert.SerializeObject(roleEvent));

            // Assert
            roleRepositoryMock.Verify(
                repository => repository.AddRoleAsync(It.IsAny<RoleEntity>()),
                Times.Once);
            contactAkuiteoSynchronizerMock.Verify(
                synchronizer => synchronizer.SynchronizeAsync(
                    It.IsAny<RoleCreatedEventData>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// Ensures the existing-role update path does not bypass Akuiteo synchronization.
        /// </summary>
        [Fact]
        public async Task HandleAsync_WithExistingRoleUpdate_ShouldSynchronizeContact()
        {
            // Arrange
            var roleRepositoryMock = new Mock<IRoleRepository>();
            var operationRepositoryMock = new Mock<IOperationRepository>();
            var featureFlagServiceMock = new Mock<IFeatureFlagService>();
            var contactAkuiteoSynchronizerMock = new Mock<IContactAkuiteoSynchronizer>();
            var eventData = CreateValidEventData();
            eventData.ContactFlagPortailFactures = true;
            var roleEvent = new RoleCreatedEvent(eventData)
            {
                AccountType = AccountTypes.Prospect
            };
            var existingRole = new RoleEntity
            {
                ContactId = eventData.ContactId,
                AccountId = eventData.AccountId,
                ContactEmail = eventData.ContactEmail,
                AccountNumber = eventData.AccountNumber,
                ContactFlagPortailFactures = false
            };

            roleRepositoryMock
                .Setup(repository => repository.GetPulseRole(eventData.ContactEmail, eventData.AccountNumber))
                .ReturnsAsync(existingRole);
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
            featureFlagServiceMock
                .Setup(service => service.IsEnabled(FeatureFlagKeys.IsContactAkuiteoSynchronizationEnabled))
                .Returns(true);

            var handler = CreateHandler(
                roleRepositoryMock,
                operationRepositoryMock,
                featureFlagServiceMock,
                contactAkuiteoSynchronizerMock);

            // Act
            await handler.HandleAsync(JsonConvert.SerializeObject(roleEvent));

            // Assert
            roleRepositoryMock.Verify(
                repository => repository.UpdatePulseRole(existingRole),
                Times.Once);
            roleRepositoryMock.Verify(
                repository => repository.AddRoleAsync(It.IsAny<RoleEntity>()),
                Times.Never);
            contactAkuiteoSynchronizerMock.Verify(
                synchronizer => synchronizer.SynchronizeAsync(
                    It.IsAny<RoleCreatedEventData>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        /// <summary>
        /// Creates a handler with the dependencies required by the synchronization flow.
        /// </summary>
        /// <param name="roleRepositoryMock">The role repository mock.</param>
        /// <param name="operationRepositoryMock">The operation repository mock.</param>
        /// <param name="featureFlagServiceMock">The feature flag service mock.</param>
        /// <param name="contactAkuiteoSynchronizerMock">The contact synchronizer mock.</param>
        /// <returns>The configured handler.</returns>
        private static RoleCreatedEventHandler CreateHandler(
            Mock<IRoleRepository> roleRepositoryMock,
            Mock<IOperationRepository> operationRepositoryMock,
            Mock<IFeatureFlagService> featureFlagServiceMock,
            Mock<IContactAkuiteoSynchronizer> contactAkuiteoSynchronizerMock)
        {
            return new RoleCreatedEventHandler(
                Mock.Of<ILogger<RoleCreatedEventHandler>>(),
                Mock.Of<IRoleRegistryProvider>(),
                roleRepositoryMock.Object,
                operationRepositoryMock.Object,
                featureFlagServiceMock.Object,
                contactAkuiteoSynchronizerMock.Object);
        }

        /// <summary>
        /// Creates valid role event data for handler tests.
        /// </summary>
        /// <returns>The valid event data.</returns>
        private static RoleCreatedEventData CreateValidEventData()
        {
            return new RoleCreatedEventData
            {
                AccountId = 22,
                AccountGlobalUniqueId = Guid.NewGuid(),
                ContactId = 123,
                ContactGlobalUniqueId = Guid.NewGuid(),
                IsSignatory = true,
                IsFavorite = false,
                IsDelegation = false,
                AccountNumber = "199099090",
                ContactEmail = "email@test.fr",
                ContactFlagPortailFactures = true
            };
        }
    }
}

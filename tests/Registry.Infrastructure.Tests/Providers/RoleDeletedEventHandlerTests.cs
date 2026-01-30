using Application.Consts;
using Application.Enums;
using Application.Interfaces;
using Application.Requests;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Pulse.Back.Events.IntegrationEvents;
using Pulse.Back.Events.IntegrationEvents.EventsData;
using Pulse.Registry.Domain.Entities;
using Registry.Application.Consts;
using Infrastructure.Providers;
using Pulse.Registry.Domain.Entities.Accounts;

namespace Registry.Infrastructure.Tests.Providers;

public class RoleDeletedEventHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithValidMessage_ShouldDeleteRoleAndUpdateOperationStatus()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<RoleDeletedEventHandler>>();
        var roleRegistryProviderMock = new Mock<IRoleRegistryProvider>(MockBehavior.Strict);
        var roleRepositoryMock = new Mock<IRoleRepository>();
        var operationRepositoryMock = new Mock<IOperationRepository>();

        roleRepositoryMock.Setup(r => r.DeleteRoleAsync(It.IsAny<RoleEntity>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        operationRepositoryMock.Setup(op => op.FetchOperationsByCriteriaAsync(
            It.Is<OperationSearchCriteria>(c => c.FetchSystemGeneratedOperation == true),
            OperationStrategyType.ROLE,
            It.IsAny<string>(),
            It.IsAny<bool?>(),
            It.IsAny<string?>()))
            .ReturnsAsync(new List<RegOperationEntity>
            {
                    new RegOperationEntity { EntityId = Guid.NewGuid(), ApprovalStatus = ApprovalStatus.Approved, ProcessStatus = ProcessStatus.Sent }
            })
            .Verifiable();

        operationRepositoryMock.Setup(op => op.FetchOperationsByCriteriaAsync(
            It.Is<OperationSearchCriteria>(c => c.FetchSystemGeneratedOperation == false),
            OperationStrategyType.ROLE,
            It.IsAny<string>(),
            It.IsAny<bool?>(),
            It.IsAny<string?>()))
            .ReturnsAsync(new List<RegOperationEntity>())
            .Verifiable();

        operationRepositoryMock.Setup(op => op.BulkUpdateOperationsStatusAsync(
            ProcessStatus.Succeeded.ToString(),
            It.IsAny<IEnumerable<RegOperationEntity>>()))
            .ReturnsAsync(true)
            .Verifiable();

        // Remove check for UpdateRoleAsync since the corresponding code is commented.
        // roleRegistryProviderMock.Setup(rp => rp.UpdateRoleAsync(It.IsAny<RoleRegistry>()))
        //     .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK))
        //     .Verifiable();

        var eventData = new RoleDeletedEventData
        {
            AccountId = 22,
            AccountGlobalUniqueId = Guid.NewGuid(),
            ContactId = 123,
            ContactGlobalUniqueId = Guid.NewGuid(),
            AccountNumber = "199099090",
            ContactEmail = "email@test.fr"
        };
        var roleDeletedEvent = new RoleDeletedEvent(eventData);
        var message = JsonConvert.SerializeObject(roleDeletedEvent);

        var handler = new RoleDeletedEventHandler(
            loggerMock.Object,
            roleRegistryProviderMock.Object,
            roleRepositoryMock.Object,
            operationRepositoryMock.Object);

        // Act
        await handler.HandleAsync(message);

        // Assert
        roleRepositoryMock.Verify(r => r.DeleteRoleAsync(It.Is<RoleEntity>(r =>
            r.AccountId == eventData.AccountId &&
            r.AccountGlobalUniqueId == eventData.AccountGlobalUniqueId &&
            r.AccountNumber == eventData.AccountNumber &&
            r.ContactId == eventData.ContactId &&
            r.ContactEmail == eventData.ContactEmail
        )), Times.Once);
        operationRepositoryMock.Verify(op => op.FetchOperationsByCriteriaAsync(
            It.Is<OperationSearchCriteria>(c => c.OperationName == OperationAction.Delete),
            OperationStrategyType.ROLE,
            It.IsAny<string>(),
            It.IsAny<bool?>(),
            It.IsAny<string?>()), Times.Exactly(2));
        operationRepositoryMock.Verify(op => op.BulkUpdateOperationsStatusAsync(
            ProcessStatus.Succeeded.ToString(),
            It.IsAny<IEnumerable<RegOperationEntity>>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithNullMessage_ShouldLogErrorAndNotDeleteRole()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<RoleDeletedEventHandler>>();
        var roleRegistryProviderMock = new Mock<IRoleRegistryProvider>(MockBehavior.Strict);
        var roleRepositoryMock = new Mock<IRoleRepository>();
        var operationRepositoryMock = new Mock<IOperationRepository>();

        var handler = new RoleDeletedEventHandler(
            loggerMock.Object,
            roleRegistryProviderMock.Object,
            roleRepositoryMock.Object,
            operationRepositoryMock.Object);

        // Act
        await handler.HandleAsync(null!);

        // Assert
        roleRepositoryMock.Verify(r => r.DeleteRoleAsync(It.IsAny<RoleEntity>()), Times.Never);
        operationRepositoryMock.Verify(op => op.FetchOperationsByCriteriaAsync(
            It.IsAny<OperationSearchCriteria>(),
            It.IsAny<OperationStrategyType>(),
            It.IsAny<string>(),
            It.IsAny<bool?>(),
            It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidData_ShouldLogErrorAndNotDeleteRole()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<RoleDeletedEventHandler>>();
        var roleRegistryProviderMock = new Mock<IRoleRegistryProvider>(MockBehavior.Strict);
        var roleRepositoryMock = new Mock<IRoleRepository>();
        var operationRepositoryMock = new Mock<IOperationRepository>();

        var invalidEventData = new RoleDeletedEventData
        {
            AccountId = 22,
            AccountGlobalUniqueId = Guid.NewGuid(),
            ContactId = 0, // Invalid
            ContactGlobalUniqueId = Guid.NewGuid(),
            AccountNumber = "199099090",
            ContactEmail = "email@test.fr"
        };
        var invalidEvent = new RoleDeletedEvent(invalidEventData);
        var message = JsonConvert.SerializeObject(invalidEvent);

        var handler = new RoleDeletedEventHandler(
            loggerMock.Object,
            roleRegistryProviderMock.Object,
            roleRepositoryMock.Object,
            operationRepositoryMock.Object);

        // Act
        await handler.HandleAsync(message);

        // Assert
        roleRepositoryMock.Verify(r => r.DeleteRoleAsync(It.IsAny<RoleEntity>()), Times.Never);
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
        var loggerMock = new Mock<ILogger<RoleDeletedEventHandler>>();
        var roleRegistryProviderMock = new Mock<IRoleRegistryProvider>(MockBehavior.Strict);
        var roleRepositoryMock = new Mock<IRoleRepository>();
        var operationRepositoryMock = new Mock<IOperationRepository>();

        roleRepositoryMock.Setup(r => r.DeleteRoleAsync(It.IsAny<RoleEntity>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        operationRepositoryMock.Setup(op => op.FetchOperationsByCriteriaAsync(
            It.Is<OperationSearchCriteria>(c => c.FetchSystemGeneratedOperation == true),
            OperationStrategyType.ROLE,
            It.IsAny<string>(),
            It.IsAny<bool?>(),
            It.IsAny<string?>()))
            .ReturnsAsync(new List<RegOperationEntity>())
            .Verifiable();

        operationRepositoryMock.Setup(op => op.FetchOperationsByCriteriaAsync(
            It.Is<OperationSearchCriteria>(c => c.FetchSystemGeneratedOperation == false),
            OperationStrategyType.ROLE,
            It.IsAny<string>(),
            It.IsAny<bool?>(),
            It.IsAny<string?>()))
            .ReturnsAsync(new List<RegOperationEntity>())
            .Verifiable();

        var validEventData = new RoleDeletedEventData
        {
            AccountId = 22,
            AccountGlobalUniqueId = Guid.NewGuid(),
            ContactId = 123,
            ContactGlobalUniqueId = Guid.NewGuid(),
            AccountNumber = "199099090",
            ContactEmail = "email@test.fr"
        };
        var validEvent = new RoleDeletedEvent(validEventData);
        var message = JsonConvert.SerializeObject(validEvent);

        var handler = new RoleDeletedEventHandler(
            loggerMock.Object,
            roleRegistryProviderMock.Object,
            roleRepositoryMock.Object,
            operationRepositoryMock.Object);

        // Act
        await handler.HandleAsync(message);

        // Assert
        roleRepositoryMock.Verify(r => r.DeleteRoleAsync(It.IsAny<RoleEntity>()), Times.Once);
        operationRepositoryMock.Verify(op => op.FetchOperationsByCriteriaAsync(
            It.IsAny<OperationSearchCriteria>(),
            OperationStrategyType.ROLE,
            It.IsAny<string>(),
            It.IsAny<bool?>(),
            It.IsAny<string?>()), Times.Exactly(2));
        operationRepositoryMock.Verify(op => op.BulkUpdateOperationsStatusAsync(
            It.IsAny<string>(),
            It.IsAny<IEnumerable<RegOperationEntity>>()), Times.Never);
    }
}

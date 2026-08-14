// <copyright file="MissionCreatedEventHandlerTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Consts;
using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Pulse.Back.Events.IntegrationEvents;
using Pulse.Back.Events.IntegrationEvents.EventsData;
using Registry.AzureFuctions.Functions;

namespace Registry.AzureFunctions.Tests.Functions;

public class MissionCreatedEventHandlerTests
{
    private readonly Mock<IMissionConfirmationService> _confirmationServiceMock;
    private readonly MissionCreatedEventHandler _function;

    public MissionCreatedEventHandlerTests()
    {
        _confirmationServiceMock = new Mock<IMissionConfirmationService>();

        _function = new MissionCreatedEventHandler(new Mock<ILogger<MissionCreatedEventHandler>>().Object, _confirmationServiceMock.Object);
    }

    [Fact]
    public async Task ProcessMissionCreated_ConfirmsTheInsertLine()
    {
        // Arrange - the payload carries the code only, the operation comes from the event type
        var @event = new MissionCreatedEvent(new MissionCreatedEventData { EngagementCode = "E363660" });

        // Act
        await _function.ProcessMissionCreatedAsync(@event, CancellationToken.None);

        // Assert
        _confirmationServiceMock.Verify(
            s => s.ConfirmAsync("E363660", OperationAction.Insert, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessMissionCreated_NeverConfirmsADeleteLine()
    {
        // Arrange - the same code exists as an INSERT and as a DELETE; only the first is settled here
        var @event = new MissionCreatedEvent(new MissionCreatedEventData { EngagementCode = "E363660" });

        // Act
        await _function.ProcessMissionCreatedAsync(@event, CancellationToken.None);

        // Assert
        _confirmationServiceMock.Verify(
            s => s.ConfirmAsync(It.IsAny<string>(), OperationAction.Delete, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ProcessMissionCreated_WithoutEngagementCode_ConfirmsNothing(string? engagementCode)
    {
        // Arrange
        var @event = new MissionCreatedEvent(new MissionCreatedEventData { EngagementCode = engagementCode! });

        // Act
        await _function.ProcessMissionCreatedAsync(@event, CancellationToken.None);

        // Assert
        _confirmationServiceMock.Verify(
            s => s.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessMissionCreated_WithNullEvent_Throws()
    {
        // Arrange - an unusable message must be redelivered then dead-lettered, not silently settled
        var act = () => _function.ProcessMissionCreatedAsync(null!, CancellationToken.None);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(act);
    }
}

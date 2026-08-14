// <copyright file="MissionRemovedEventHandlerTests.cs" company="Pulse">
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

public class MissionRemovedEventHandlerTests
{
    private readonly Mock<IMissionConfirmationService> _confirmationServiceMock;
    private readonly MissionRemovedEventHandler _function;

    public MissionRemovedEventHandlerTests()
    {
        _confirmationServiceMock = new Mock<IMissionConfirmationService>();

        _function = new MissionRemovedEventHandler(new Mock<ILogger<MissionRemovedEventHandler>>().Object, _confirmationServiceMock.Object);
    }

    [Fact]
    public async Task ProcessMissionRemoved_ConfirmsTheDeleteLine()
    {
        // Arrange
        var @event = new MissionRemovedEvent(new MissionRemovedEventData { EngagementCode = "E363660" });

        // Act
        await _function.ProcessMissionRemovedAsync(@event, CancellationToken.None);

        // Assert
        _confirmationServiceMock.Verify(
            s => s.ConfirmAsync("E363660", OperationAction.Delete, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessMissionRemoved_NeverConfirmsAnInsertLine()
    {
        // Arrange - the same code can exist as an INSERT settled months earlier
        var @event = new MissionRemovedEvent(new MissionRemovedEventData { EngagementCode = "E363660" });

        // Act
        await _function.ProcessMissionRemovedAsync(@event, CancellationToken.None);

        // Assert
        _confirmationServiceMock.Verify(
            s => s.ConfirmAsync(It.IsAny<string>(), OperationAction.Insert, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessMissionRemoved_OnAnEngagementOfferNeverStored_StillConfirms()
    {
        // Arrange - when the mapping was missing at INSERT time Offer stored nothing, so it answers
        // with nothing to revoke. Treating that as a failure would replay the line for ever.
        var @event = new MissionRemovedEvent(new MissionRemovedEventData { EngagementCode = "E999999" });

        // Act
        await _function.ProcessMissionRemovedAsync(@event, CancellationToken.None);

        // Assert
        _confirmationServiceMock.Verify(
            s => s.ConfirmAsync("E999999", OperationAction.Delete, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ProcessMissionRemoved_WithoutEngagementCode_ConfirmsNothing(string? engagementCode)
    {
        // Arrange
        var @event = new MissionRemovedEvent(new MissionRemovedEventData { EngagementCode = engagementCode! });

        // Act
        await _function.ProcessMissionRemovedAsync(@event, CancellationToken.None);

        // Assert
        _confirmationServiceMock.Verify(
            s => s.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessMissionRemoved_WithNullEvent_Throws()
    {
        // Arrange
        var act = () => _function.ProcessMissionRemovedAsync(null!, CancellationToken.None);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(act);
    }
}

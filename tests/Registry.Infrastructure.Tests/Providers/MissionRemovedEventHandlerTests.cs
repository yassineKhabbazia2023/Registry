// <copyright file="MissionRemovedEventHandlerTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Consts;
using Application.Interfaces;
using Infrastructure.Providers;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Pulse.Back.Events.IntegrationEvents;
using Pulse.Back.Events.IntegrationEvents.EventsData;
using System.Threading;
using System.Threading.Tasks;

namespace Registry.Infrastructure.Tests.Providers;

public class MissionRemovedEventHandlerTests
{
    private readonly Mock<ILogger<MissionRemovedEventHandler>> _loggerMock = new();
    private readonly Mock<IMissionConfirmationService> _confirmationServiceMock = new();
    private readonly MissionRemovedEventHandler _sut;

    public MissionRemovedEventHandlerTests()
    {
        _sut = new MissionRemovedEventHandler(_loggerMock.Object, _confirmationServiceMock.Object);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleAsync_WithEmptyMessage_DoesNotConfirm(string? message)
    {
        // Act
        await _sut.HandleAsync(message!);

        // Assert
        _confirmationServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WithAPublishedPayload_ConfirmsTheDeleteLine()
    {
        // Arrange
        var message = JsonConvert.SerializeObject(
            new MissionRemovedEvent(new MissionRemovedEventData { EngagementCode = "E363660" }));

        // Act
        await _sut.HandleAsync(message);

        // Assert - the same code carries an INSERT line and a DELETE line; the event type says
        // which one to settle.
        _confirmationServiceMock.Verify(
            s => s.ConfirmAsync("E363660", OperationAction.Delete, It.IsAny<CancellationToken>()),
            Times.Once);
        _confirmationServiceMock.Verify(
            s => s.ConfirmAsync(It.IsAny<string>(), OperationAction.Insert, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public void EventType_MatchesTheKeyTheHandlerIsRegisteredUnder()
    {
        // Same invariant as the Created side.
        var @event = new MissionRemovedEvent(new MissionRemovedEventData { EngagementCode = "E363660" });

        Assert.Equal(nameof(MissionRemovedEvent), @event.EventType);
    }

    [Fact]
    public async Task HandleAsync_WithAnEnvelopeWithoutData_DoesNotConfirm()
    {
        // Arrange
        var message = "{\"EventType\":\"MissionRemovedEvent\",\"Version\":\"1.0\",\"Data\":null}";

        // Act
        await _sut.HandleAsync(message);

        // Assert
        _confirmationServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WithAnEmptyEngagementCode_DoesNotConfirm()
    {
        // Arrange
        var message = JsonConvert.SerializeObject(
            new MissionRemovedEvent(new MissionRemovedEventData { EngagementCode = string.Empty }));

        // Act
        await _sut.HandleAsync(message);

        // Assert
        _confirmationServiceMock.VerifyNoOtherCalls();
    }
}

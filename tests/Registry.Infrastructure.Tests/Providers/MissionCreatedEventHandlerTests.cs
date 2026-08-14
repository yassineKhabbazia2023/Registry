// <copyright file="MissionCreatedEventHandlerTests.cs" company="Pulse">
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

public class MissionCreatedEventHandlerTests
{
    private readonly Mock<ILogger<MissionCreatedEventHandler>> _loggerMock = new();
    private readonly Mock<IMissionConfirmationService> _confirmationServiceMock = new();
    private readonly MissionCreatedEventHandler _sut;

    public MissionCreatedEventHandlerTests()
    {
        _sut = new MissionCreatedEventHandler(_loggerMock.Object, _confirmationServiceMock.Object);
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
    public async Task HandleAsync_WithAPublishedPayload_ConfirmsTheInsertLine()
    {
        // Arrange - the payload built by the publishing serializer: deserializing that envelope
        // is exactly what breaks under System.Text.Json.
        var message = JsonConvert.SerializeObject(
            new MissionCreatedEvent(new MissionCreatedEventData { EngagementCode = "E363660" }));

        // Act
        await _sut.HandleAsync(message);

        // Assert
        _confirmationServiceMock.Verify(
            s => s.ConfirmAsync("E363660", OperationAction.Insert, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void EventType_MatchesTheKeyTheHandlerIsRegisteredUnder()
    {
        // The processor resolves the handler with GetKeyedService, keyed on the EventType the
        // publisher writes. Drift between the two drops the confirmation silently, no error.
        var @event = new MissionCreatedEvent(new MissionCreatedEventData { EngagementCode = "E363660" });

        Assert.Equal(nameof(MissionCreatedEvent), @event.EventType);
    }

    [Fact]
    public async Task HandleAsync_WithAnEnvelopeWithoutData_DoesNotConfirm()
    {
        // Arrange
        var message = "{\"EventType\":\"MissionCreatedEvent\",\"Version\":\"1.0\",\"Data\":null}";

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
            new MissionCreatedEvent(new MissionCreatedEventData { EngagementCode = string.Empty }));

        // Act
        await _sut.HandleAsync(message);

        // Assert
        _confirmationServiceMock.VerifyNoOtherCalls();
    }
}

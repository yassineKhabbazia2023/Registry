// <copyright file="ProcessMissionLinesTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Exceptions;
using Application.Interfaces;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Moq;
using Registry.AzureFuctions.Functions;

namespace Registry.AzureFunctions.Tests.Functions;

public class ProcessMissionLinesTests
{
    private readonly Mock<IMissionPublicationService> _missionPublicationServiceMock;
    private readonly Mock<ServiceBusMessageActions> _messageActionsMock;
    private readonly ProcessMissionLines _function;

    public ProcessMissionLinesTests()
    {
        _missionPublicationServiceMock = new Mock<IMissionPublicationService>();
        _messageActionsMock = new Mock<ServiceBusMessageActions>();

        _function = new ProcessMissionLines(new Mock<ILogger<ProcessMissionLines>>().Object, _missionPublicationServiceMock.Object);
    }

    [Fact]
    public async Task OnCsvReceived_ProcessesPendingLinesAndCompletesMessage()
    {
        // Arrange
        var message = CreateMessage("missions-20260814-093000.csv");

        // Act
        await _function.ProcessMissionLinesOnCsvReceivedAsync(message, _messageActionsMock.Object, CancellationToken.None);

        // Assert
        _missionPublicationServiceMock.Verify(s => s.ProcessPendingLinesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _messageActionsMock.Verify(a => a.CompleteMessageAsync(message, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnCsvReceived_WhenProcessingFails_DoesNotCompleteMessage()
    {
        // Arrange - leaving the message unsettled is what gets it redelivered and the lines retried
        var message = CreateMessage("missions-20260814-093000.csv");
        _missionPublicationServiceMock
            .Setup(s => s.ProcessPendingLinesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ServiceBusOperationException("topic unavailable"));

        // Act
        var act = () => _function.ProcessMissionLinesOnCsvReceivedAsync(message, _messageActionsMock.Object, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<ServiceBusOperationException>(act);
        _messageActionsMock.Verify(
            a => a.CompleteMessageAsync(It.IsAny<ServiceBusReceivedMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task OnTimer_ProcessesPendingLines()
    {
        // Arrange - the scheduled pass is what retries a line still waiting for its account
        var timer = new TimerInfo();

        // Act
        await _function.ProcessMissionLinesOnTimerAsync(timer, CancellationToken.None);

        // Assert
        _missionPublicationServiceMock.Verify(s => s.ProcessPendingLinesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnTimer_WhenProcessingFails_Throws()
    {
        // Arrange
        _missionPublicationServiceMock
            .Setup(s => s.ProcessPendingLinesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ServiceBusOperationException("topic unavailable"));

        // Act
        var act = () => _function.ProcessMissionLinesOnTimerAsync(new TimerInfo(), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<ServiceBusOperationException>(act);
    }

    [Fact]
    public void OnCsvReceived_TriggerAttribute_ExplicitlyDisablesAutoCompletion()
    {
        // Only named arguments written on the attribute reach the generated metadata, and a plain
        // bool read defaults to false either way.
        var messageParameter = typeof(ProcessMissionLines)
            .GetMethod(nameof(ProcessMissionLines.ProcessMissionLinesOnCsvReceivedAsync))!
            .GetParameters()[0];

        var trigger = messageParameter.GetCustomAttributesData()
            .Single(a => a.AttributeType == typeof(ServiceBusTriggerAttribute));

        var autoComplete = trigger.NamedArguments
            .SingleOrDefault(n => n.MemberName == nameof(ServiceBusTriggerAttribute.AutoCompleteMessages));

        Assert.NotNull(autoComplete.MemberInfo);
        Assert.Equal(false, autoComplete.TypedValue.Value);
    }

    private static ServiceBusReceivedMessage CreateMessage(string blobName)
    {
        return ServiceBusModelFactory.ServiceBusReceivedMessage(body: BinaryData.FromString(blobName));
    }
}

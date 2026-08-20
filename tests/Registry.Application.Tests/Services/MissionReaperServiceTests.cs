// <copyright file="MissionReaperServiceTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Options;
using Application.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Registry.Application.Tests.Services;

public class MissionReaperServiceTests
{
    private readonly Mock<IMissionProcessingRepository> _missionProcessingRepositoryMock = new();

    [Fact]
    public async Task ReapUnacknowledgedMissionsAsync_PassesTheConfiguredAckTimeoutToTheRepository()
    {
        // Arrange
        var sut = CreateSut(ackTimeoutMinutes: 60);
        _missionProcessingRepositoryMock
            .Setup(r => r.MarkUnacknowledgedAsFailedAsync(60, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        // Act
        await sut.ReapUnacknowledgedMissionsAsync();

        // Assert
        _missionProcessingRepositoryMock.Verify(r => r.MarkUnacknowledgedAsFailedAsync(60, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReapUnacknowledgedMissionsAsync_WithADifferentConfiguredTimeout_UsesIt()
    {
        // Arrange
        var sut = CreateSut(ackTimeoutMinutes: 15);
        _missionProcessingRepositoryMock
            .Setup(r => r.MarkUnacknowledgedAsFailedAsync(15, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        await sut.ReapUnacknowledgedMissionsAsync();

        // Assert
        _missionProcessingRepositoryMock.Verify(r => r.MarkUnacknowledgedAsFailedAsync(15, It.IsAny<CancellationToken>()), Times.Once);
    }

    private MissionReaperService CreateSut(int ackTimeoutMinutes)
    {
        return new MissionReaperService(
            _missionProcessingRepositoryMock.Object,
            Options.Create(new MissionOptions { AckTimeoutMinutes = ackTimeoutMinutes }),
            Mock.Of<ILogger<MissionReaperService>>());
    }
}

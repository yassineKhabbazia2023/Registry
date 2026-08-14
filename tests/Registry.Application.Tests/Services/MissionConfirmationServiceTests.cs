// <copyright file="MissionConfirmationServiceTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Consts;
using Application.Interfaces;
using Application.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Pulse.Registry.Domain.Entities;
using Xunit;

namespace Registry.Application.Tests.Services;

public class MissionConfirmationServiceTests
{
    private readonly Mock<IMissionRepository> _missionRepositoryMock = new();
    private readonly Mock<IMissionProcessingRepository> _missionProcessingRepositoryMock = new();
    private readonly MissionConfirmationService _sut;

    public MissionConfirmationServiceTests()
    {
        _sut = new MissionConfirmationService(
            _missionRepositoryMock.Object,
            _missionProcessingRepositoryMock.Object,
            Mock.Of<ILogger<MissionConfirmationService>>());
    }

    [Theory]
    [InlineData(OperationAction.Insert)]
    [InlineData(OperationAction.Delete)]
    public async Task ConfirmAsync_ResolvesTheLineOnTheEngagementCodeAndTheOperation(string operation)
    {
        // Arrange - the payload carries the code, the operation comes from the event type
        var processing = Processing(ProcessStatus.Sent);
        SetupMission("E363660", operation, processing);

        // Act
        await _sut.ConfirmAsync("E363660", operation);

        // Assert
        _missionRepositoryMock.Verify(r => r.GetByEngagementCodeAndOperationAsync("E363660", operation, It.IsAny<CancellationToken>()), Times.Once);
        processing.Status.Should().Be(ProcessStatus.Succeeded);
        processing.ProcessedOn.Should().NotBeNull();
    }

    [Fact]
    public async Task ConfirmAsync_TheSameCodeInsertedThenDeleted_SettlesTwoDistinctLines()
    {
        // Arrange - E363660 comes back as a DELETE months after its INSERT: two CSV lines,
        // two processing rows, told apart by the operation alone.
        var inserted = Processing(ProcessStatus.Succeeded, registryMissionId: 10);
        var deleted = Processing(ProcessStatus.Sent, registryMissionId: 47);
        SetupMission("E363660", OperationAction.Insert, inserted, registryMissionId: 10);
        SetupMission("E363660", OperationAction.Delete, deleted, registryMissionId: 47);

        // Act
        await _sut.ConfirmAsync("E363660", OperationAction.Delete);

        // Assert
        deleted.Status.Should().Be(ProcessStatus.Succeeded);
        inserted.ProcessedOn.Should().BeNull("la ligne INSERT ne doit pas etre retouchee");
    }

    [Fact]
    public async Task ConfirmAsync_OnAnAlreadySettledLine_ChangesNothing()
    {
        // Arrange - a replayed acknowledgement after a lost message
        var processing = Processing(ProcessStatus.Succeeded);
        SetupMission("E363660", OperationAction.Insert, processing);

        // Act
        await _sut.ConfirmAsync("E363660", OperationAction.Insert);

        // Assert
        processing.ProcessedOn.Should().BeNull();
        _missionProcessingRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<IEnumerable<MissionProcessingEntity>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmAsync_WithNoMatchingLine_ReturnsWithoutThrowing()
    {
        // Arrange - throwing would get the message redelivered for ever over a line that will never exist
        _missionRepositoryMock
            .Setup(r => r.GetByEngagementCodeAndOperationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MissionEntity?)null);

        // Act
        var act = async () => await _sut.ConfirmAsync("UNKNOWN", OperationAction.Insert);

        // Assert
        await act.Should().NotThrowAsync();
        _missionProcessingRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<IEnumerable<MissionProcessingEntity>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private void SetupMission(string engagementCode, string operation, MissionProcessingEntity processing, int registryMissionId = 1)
    {
        _missionRepositoryMock
            .Setup(r => r.GetByEngagementCodeAndOperationAsync(engagementCode, operation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MissionEntity
            {
                RegistryMissionId = registryMissionId,
                AccountNumber = "123456",
                EngagementCode = engagementCode,
                OfferCode = "PennylaneOfferCode",
                Operation = operation,
            });

        _missionProcessingRepositoryMock
            .Setup(r => r.GetByRegistryMissionIdAsync(registryMissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(processing);
    }

    private static MissionProcessingEntity Processing(string status, int registryMissionId = 1) => new()
    {
        RegistryMissionId = registryMissionId,
        Status = status,
    };
}

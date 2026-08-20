// <copyright file="MissionPublicationServiceTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Consts;
using Application.Interfaces;
using Application.Options;
using Application.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Pulse.Back.Events.IntegrationEvents.EventsData;
using Pulse.Registry.Domain.Entities;
using Xunit;

namespace Registry.Application.Tests.Services;

public class MissionPublicationServiceTests
{
    private const int Chunk = 10;

    private static IReadOnlyCollection<string> ReadyAndFailed =>
        It.Is<IReadOnlyCollection<string>>(s => s.SequenceEqual(new[] { ProcessStatus.Ready, ProcessStatus.Failed }));

    private readonly Mock<IMissionProcessingRepository> _missionProcessingRepositoryMock;
    private readonly Mock<IMissionRepository> _missionRepositoryMock;
    private readonly Mock<IAccountRepository> _accountRepositoryMock;
    private readonly Mock<IMissionEventPublisher> _missionEventPublisherMock;
    private readonly Mock<ILogger<MissionPublicationService>> _loggerMock;
    private readonly MissionPublicationService _sut;

    public MissionPublicationServiceTests()
    {
        _missionProcessingRepositoryMock = new Mock<IMissionProcessingRepository>();
        _missionRepositoryMock = new Mock<IMissionRepository>();
        _accountRepositoryMock = new Mock<IAccountRepository>();
        _missionEventPublisherMock = new Mock<IMissionEventPublisher>();
        _loggerMock = new Mock<ILogger<MissionPublicationService>>();

        _sut = new MissionPublicationService(
            _missionProcessingRepositoryMock.Object,
            _missionRepositoryMock.Object,
            _accountRepositoryMock.Object,
            _missionEventPublisherMock.Object,
            Options.Create(new BackGroundJobOptions { Chunk = Chunk }),
            _loggerMock.Object);
    }

    [Fact]
    public async Task ProcessPendingLinesAsync_WithNoLines_DoesNothing()
    {
        // Arrange
        SetupChunks();

        // Act
        await _sut.ProcessPendingLinesAsync();

        // Assert
        _missionProcessingRepositoryMock.Verify(
            r => r.GetByStatusAsync(ReadyAndFailed, 0, Chunk, It.IsAny<CancellationToken>()),
            Times.Once);
        _missionEventPublisherMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ProcessPendingLinesAsync_PublishesTheContractMatchingEachOperation()
    {
        // Arrange
        var insert = Mission(1, "E1", OperationAction.Insert);
        var delete = Mission(2, "E2", OperationAction.Delete);
        SetupChunks([Processing(1), Processing(2)]);
        SetupMissions(insert, delete);
        SetupKnownAccounts(insert.AccountNumber);

        List<RegistryMissionCreatedEventData>? created = null;
        List<RegistryMissionRemovedEventData>? removed = null;
        _missionEventPublisherMock.Setup(p => p.BulkPublishAsync(It.IsAny<List<RegistryMissionCreatedEventData>>()))
            .Callback<List<RegistryMissionCreatedEventData>>(e => created = e).Returns(Task.CompletedTask);
        _missionEventPublisherMock.Setup(p => p.BulkPublishRemovedAsync(It.IsAny<List<RegistryMissionRemovedEventData>>()))
            .Callback<List<RegistryMissionRemovedEventData>>(e => removed = e).Returns(Task.CompletedTask);

        // Act
        await _sut.ProcessPendingLinesAsync();

        // Assert
        created.Should().ContainSingle().Which.EngagementCode.Should().Be("E1");
        removed.Should().ContainSingle().Which.EngagementCode.Should().Be("E2");
    }

    [Fact]
    public async Task ProcessPendingLinesAsync_CarriesNoSurrogateKey_OnlyBusinessData()
    {
        // Arrange
        var mission = Mission(1, "E1", OperationAction.Insert);
        SetupChunks([Processing(1)]);
        SetupMissions(mission);
        SetupKnownAccounts(mission.AccountNumber);

        List<RegistryMissionCreatedEventData>? published = null;
        _missionEventPublisherMock.Setup(p => p.BulkPublishAsync(It.IsAny<List<RegistryMissionCreatedEventData>>()))
            .Callback<List<RegistryMissionCreatedEventData>>(e => published = e).Returns(Task.CompletedTask);

        // Act
        await _sut.ProcessPendingLinesAsync();

        // Assert - RegistryMissionId stays private to Registry
        var data = published.Should().ContainSingle().Subject;
        data.AccountNumber.Should().Be(mission.AccountNumber);
        data.OfferCode.Should().Be(mission.OfferCode);
        typeof(RegistryMissionCreatedEventData).GetProperty("RegistryMissionId").Should().BeNull();
        typeof(RegistryMissionCreatedEventData).GetProperty("MissionId").Should().BeNull();
    }

    [Fact]
    public async Task ProcessPendingLinesAsync_KeepsProductCodeNull_SoTheGenericMappingCanMatch()
    {
        // Arrange
        var mission = Mission(1, "E1", OperationAction.Insert);
        mission.ProductCode = null;
        SetupChunks([Processing(1)]);
        SetupMissions(mission);
        SetupKnownAccounts(mission.AccountNumber);

        List<RegistryMissionCreatedEventData>? published = null;
        _missionEventPublisherMock.Setup(p => p.BulkPublishAsync(It.IsAny<List<RegistryMissionCreatedEventData>>()))
            .Callback<List<RegistryMissionCreatedEventData>>(e => published = e).Returns(Task.CompletedTask);

        // Act
        await _sut.ProcessPendingLinesAsync();

        // Assert - an empty string would never match a NULL AkuiteoProductCode on the Offer side
        published.Should().ContainSingle().Which.ProductCode.Should().BeNull();
    }

    [Fact]
    public async Task ProcessPendingLinesAsync_MovesPublishedLinesToSent()
    {
        // Arrange
        var mission = Mission(1, "E1", OperationAction.Insert);
        var processing = Processing(1);
        SetupChunks([processing]);
        SetupMissions(mission);
        SetupKnownAccounts(mission.AccountNumber);

        // Act
        await _sut.ProcessPendingLinesAsync();

        // Assert
        processing.Status.Should().Be(ProcessStatus.Sent);
        processing.PublishedOn.Should().NotBeNull();
    }

    [Fact]
    public async Task ProcessPendingLinesAsync_WithUnknownAccount_LeavesTheLineReadyWithItsReason()
    {
        // Arrange
        var mission = Mission(1, "E1", OperationAction.Insert);
        var processing = Processing(1);
        SetupChunks([processing]);
        SetupMissions(mission);
        SetupKnownAccounts();

        // Act
        await _sut.ProcessPendingLinesAsync();

        // Assert
        processing.Status.Should().Be(ProcessStatus.Ready);
        processing.Reason.Should().Contain(mission.AccountNumber);
        _missionEventPublisherMock.Verify(p => p.BulkPublishAsync(It.IsAny<List<RegistryMissionCreatedEventData>>()), Times.Never);
    }

    [Fact]
    public async Task ProcessPendingLinesAsync_WithAnUnknownAccount_Terminates()
    {
        // Arrange - the line stays READY on purpose. Paging with a plain Take() would hand back
        // the very same chunk on every turn; the keyset cursor is what ends the loop.
        var mission = Mission(1, "E1", OperationAction.Insert);
        SetupMissions(mission);
        SetupKnownAccounts();

        var calls = 0;
        _missionProcessingRepositoryMock
            .Setup(r => r.GetByStatusAsync(ReadyAndFailed, It.IsAny<int>(), Chunk, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<string> _, int after, int _, CancellationToken _) =>
            {
                calls++;
                return after < 1 ? [Processing(1)] : [];
            });

        // Act
        await _sut.ProcessPendingLinesAsync();

        // Assert - one pass over the line, one empty pass, then out
        calls.Should().Be(2);
    }

    [Fact]
    public async Task ProcessPendingLinesAsync_WalksEveryChunkOnce()
    {
        // Arrange
        var first = Mission(1, "E1", OperationAction.Insert);
        var second = Mission(2, "E2", OperationAction.Insert);
        _missionProcessingRepositoryMock
            .Setup(r => r.GetByStatusAsync(ReadyAndFailed, 0, Chunk, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Processing(1)]);
        _missionProcessingRepositoryMock
            .Setup(r => r.GetByStatusAsync(ReadyAndFailed, 1, Chunk, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Processing(2)]);
        _missionProcessingRepositoryMock
            .Setup(r => r.GetByStatusAsync(ReadyAndFailed, 2, Chunk, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        SetupMissions(first, second);
        SetupKnownAccounts(first.AccountNumber);

        // Act
        await _sut.ProcessPendingLinesAsync();

        // Assert
        _missionEventPublisherMock.Verify(
            p => p.BulkPublishAsync(It.IsAny<List<RegistryMissionCreatedEventData>>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task ProcessPendingLinesAsync_WithADeleteLine_PublishesItEvenWhenTheAccountIsUnknown()
    {
        // Arrange - account gone from the referential since the INSERT was published
        var delete = Mission(1, "E363660", OperationAction.Delete, "999999");
        var processing = Processing(1);
        SetupChunks([processing]);
        SetupMissions(delete);
        SetupKnownAccounts();

        List<RegistryMissionRemovedEventData>? removed = null;
        _missionEventPublisherMock.Setup(p => p.BulkPublishRemovedAsync(It.IsAny<List<RegistryMissionRemovedEventData>>()))
            .Callback<List<RegistryMissionRemovedEventData>>(e => removed = e).Returns(Task.CompletedTask);

        // Act
        await _sut.ProcessPendingLinesAsync();

        // Assert
        removed.Should().ContainSingle().Which.EngagementCode.Should().Be("E363660");
        processing.Status.Should().Be(ProcessStatus.Sent);
        processing.PublishedOn.Should().NotBeNull();
    }

    [Fact]
    public async Task ProcessPendingLinesAsync_WithMixedOperationsAndUnknownAccounts_PostponesOnlyTheInsert()
    {
        // Arrange
        var insert = Mission(1, "E1", OperationAction.Insert, "999999");
        var delete = Mission(2, "E2", OperationAction.Delete, "999999");
        var insertProcessing = Processing(1);
        var deleteProcessing = Processing(2);
        SetupChunks([insertProcessing, deleteProcessing]);
        SetupMissions(insert, delete);
        SetupKnownAccounts();

        // Act
        await _sut.ProcessPendingLinesAsync();

        // Assert
        insertProcessing.Status.Should().Be(ProcessStatus.Ready);
        insertProcessing.Reason.Should().Contain(insert.AccountNumber);
        deleteProcessing.Status.Should().Be(ProcessStatus.Sent);
        deleteProcessing.Reason.Should().BeNull();

        _missionEventPublisherMock.Verify(
            p => p.BulkPublishAsync(It.IsAny<List<RegistryMissionCreatedEventData>>()),
            Times.Never);
        _missionEventPublisherMock.Verify(
            p => p.BulkPublishRemovedAsync(It.Is<List<RegistryMissionRemovedEventData>>(e => e.Count == 1)),
            Times.Once);
    }

    [Fact]
    public async Task ProcessPendingLinesAsync_WithOnlyDeleteLines_DoesNotQueryTheAccounts()
    {
        // Arrange
        SetupChunks([Processing(1), Processing(2)]);
        SetupMissions(
            Mission(1, "E1", OperationAction.Delete),
            Mission(2, "E2", OperationAction.Delete));
        SetupKnownAccounts();

        // Act
        await _sut.ProcessPendingLinesAsync();

        // Assert
        _accountRepositoryMock.Verify(
            r => r.GetExistingAccountNumbersAsync(It.IsAny<IEnumerable<string>>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessPendingLinesAsync_ChecksOnlyTheAccountsOfTheInsertLines()
    {
        // Arrange
        var insert = Mission(1, "E1", OperationAction.Insert, "111111");
        var delete = Mission(2, "E2", OperationAction.Delete, "222222");
        SetupChunks([Processing(1), Processing(2)]);
        SetupMissions(insert, delete);
        SetupKnownAccounts(insert.AccountNumber);

        List<string>? queried = null;
        _accountRepositoryMock
            .Setup(r => r.GetExistingAccountNumbersAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync((IEnumerable<string> numbers) =>
            {
                queried = numbers.ToList();
                return [insert.AccountNumber];
            });

        // Act
        await _sut.ProcessPendingLinesAsync();

        // Assert
        queried.Should().ContainSingle().Which.Should().Be("111111");
    }

    [Fact]
    public async Task ProcessPendingLinesAsync_AlsoRepublishesFailedLines_MovingThemToSent()
    {
        // Arrange - a FAILED line is one the reaper gave up on for lack of acknowledgement;
        // the publication pass must pick it back up exactly like a READY one.
        var mission = Mission(1, "E1", OperationAction.Insert);
        var processing = Processing(1);
        processing.Status = ProcessStatus.Failed;
        _missionProcessingRepositoryMock
            .Setup(r => r.GetByStatusAsync(ReadyAndFailed, 0, Chunk, It.IsAny<CancellationToken>()))
            .ReturnsAsync([processing]);
        _missionProcessingRepositoryMock
            .Setup(r => r.GetByStatusAsync(ReadyAndFailed, 1, Chunk, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        SetupMissions(mission);
        SetupKnownAccounts(mission.AccountNumber);

        // Act
        await _sut.ProcessPendingLinesAsync();

        // Assert
        processing.Status.Should().Be(ProcessStatus.Sent);
        _missionEventPublisherMock.Verify(p => p.BulkPublishAsync(It.IsAny<List<RegistryMissionCreatedEventData>>()), Times.Once);
    }

    [Fact]
    public async Task ProcessPendingLinesAsync_RepublishingAFailedLine_ClearsItsReason()
    {
        // Arrange - a FAILED line carries the reaper's "no acknowledgement" reason. Once
        // republished it goes back to SENT hoping for an ack; keeping the stale reason would
        // make it read as already-failed again, defeating Reason as the supervision signal.
        var mission = Mission(1, "E1", OperationAction.Insert);
        var processing = Processing(1);
        processing.Status = ProcessStatus.Failed;
        processing.Reason = "Aucune confirmation recue dans le delai";
        _missionProcessingRepositoryMock
            .Setup(r => r.GetByStatusAsync(ReadyAndFailed, 0, Chunk, It.IsAny<CancellationToken>()))
            .ReturnsAsync([processing]);
        _missionProcessingRepositoryMock
            .Setup(r => r.GetByStatusAsync(ReadyAndFailed, 1, Chunk, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        SetupMissions(mission);
        SetupKnownAccounts(mission.AccountNumber);

        // Act
        await _sut.ProcessPendingLinesAsync();

        // Assert
        processing.Status.Should().Be(ProcessStatus.Sent);
        processing.Reason.Should().BeNull();
    }

    private void SetupChunks(params List<MissionProcessingEntity>[] chunks)
    {
        var after = 0;
        foreach (var chunk in chunks)
        {
            var current = after;
            _missionProcessingRepositoryMock
                .Setup(r => r.GetByStatusAsync(ReadyAndFailed, current, Chunk, It.IsAny<CancellationToken>()))
                .ReturnsAsync(chunk);
            after = chunk[^1].RegistryMissionId;
        }

        _missionProcessingRepositoryMock
            .Setup(r => r.GetByStatusAsync(ReadyAndFailed, after, Chunk, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    private void SetupMissions(params MissionEntity[] missions)
    {
        _missionRepositoryMock
            .Setup(r => r.GetByRegistryMissionIdsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<int> ids, CancellationToken _) =>
                missions.Where(m => ids.Contains(m.RegistryMissionId)).ToList());
    }

    private void SetupKnownAccounts(params string[] accountNumbers)
    {
        _accountRepositoryMock
            .Setup(r => r.GetExistingAccountNumbersAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(accountNumbers.ToList());
    }

    private static MissionEntity Mission(int registryMissionId, string engagementCode, string operation, string accountNumber = "123456") => new()
    {
        RegistryMissionId = registryMissionId,
        AccountNumber = accountNumber,
        EngagementCode = engagementCode,
        OfferCode = "PennylaneOfferCode",
        ProductCode = "PennylaneProProductCode",
        StartDate = new DateTime(2026, 1, 1),
        EndDate = new DateTime(2026, 6, 30),
        Operation = operation,
        CreatedOn = new DateTime(2026, 1, 1),
    };

    private static MissionProcessingEntity Processing(int registryMissionId) => new()
    {
        RegistryMissionId = registryMissionId,
        Status = ProcessStatus.Ready,
    };
}

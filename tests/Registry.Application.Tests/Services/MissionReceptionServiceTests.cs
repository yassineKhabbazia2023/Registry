//// <copyright file="MissionReceptionServiceTests.cs" company="Pulse">
//// Copyright (c) Pulse. All rights reserved.
//// </copyright>

using Application.Exceptions;
using Application.Helpers;
using Application.Interfaces;
using Application.Models;
using Application.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;

namespace Registry.Application.Tests.Services
{
    public class MissionReceptionServiceTests
    {
        private const string CsvHeader = "AccountNumber;EngagementCode;OfferCode;ProductCode;StartDate;EndDate;Operations";

        private readonly Mock<IBlobStorageManager> _blobStorageManagerMock;
        private readonly Mock<IMissionService> _missionServiceMock;
        private readonly Mock<IMissionEventPublisher> _missionEventPublisherMock;
        private readonly MissionReceptionService _sut;

        public MissionReceptionServiceTests()
        {
            _blobStorageManagerMock = new Mock<IBlobStorageManager>(MockBehavior.Strict);
            _missionServiceMock = new Mock<IMissionService>();
            _missionEventPublisherMock = new Mock<IMissionEventPublisher>();
            var logger = new Mock<ILogger<MissionReceptionService>>();

            _sut = new MissionReceptionService(
                _blobStorageManagerMock.Object,
                _missionServiceMock.Object,
                _missionEventPublisherMock.Object,
                new ValidationHelper<MissionCsv>(),
                logger.Object);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ReceiveAsync_WithEmptyContent_RejectsWithoutStorage(string? data)
        {
            // Act
            var outcome = await _sut.ReceiveAsync(data!);

            // Assert
            outcome.IsAccepted.Should().BeFalse();
            outcome.ErrorMessage.Should().Be("Invalid data: The input data cannot be null or empty.");
            _blobStorageManagerMock.Verify(x => x.SaveFileAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ReceiveAsync_WhenBlobUploadFails_Rejects()
        {
            // Arrange
            _blobStorageManagerMock.Setup(x => x.SaveFileAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new BlobStorageOperationException("fail"));

            // Act
            var outcome = await _sut.ReceiveAsync(BuildCsv("123456;E1;O1;P1;12/01/2025;12/01/2027;INSERT"));

            // Assert
            outcome.IsAccepted.Should().BeFalse();
            outcome.ErrorMessage.Should().Be("Something went wrong when saving received csv ");
            _missionServiceMock.Verify(x => x.SaveMissionsAsync(It.IsAny<IEnumerable<MissionCsv>>()), Times.Never);
        }

        [Fact]
        public async Task ReceiveAsync_WithMissingHeaderColumn_Rejects()
        {
            // Arrange
            SetupBlobSave();
            var csvContent = new StringBuilder();
            csvContent.AppendLine("AccountNumber;EngagementCode;OfferCode;ProductCode;StartDate;EndDate");
            csvContent.AppendLine("123456;E1;O1;P1;12/01/2025;12/01/2027");

            // Act
            var outcome = await _sut.ReceiveAsync(csvContent.ToString());

            // Assert
            outcome.IsAccepted.Should().BeFalse();
            outcome.ErrorMessage.Should().Be("Invalid data: Missing columns in header");
            _missionServiceMock.Verify(x => x.SaveMissionsAsync(It.IsAny<IEnumerable<MissionCsv>>()), Times.Never);
        }

        [Fact]
        public async Task ReceiveAsync_WithLineColumnCountMismatch_Rejects()
        {
            // Arrange
            SetupBlobSave();

            // Act
            var outcome = await _sut.ReceiveAsync(BuildCsv("123456;E1;O1"));

            // Assert
            outcome.IsAccepted.Should().BeFalse();
            outcome.ErrorMessage.Should().StartWith("Invalid data: Column count mismatch.");
            _missionServiceMock.Verify(x => x.SaveMissionsAsync(It.IsAny<IEnumerable<MissionCsv>>()), Times.Never);
        }

        [Fact]
        public async Task ReceiveAsync_WithHeaderOnly_Rejects()
        {
            // Arrange
            SetupBlobSave();

            // Act
            var outcome = await _sut.ReceiveAsync(BuildCsv());

            // Assert
            outcome.IsAccepted.Should().BeFalse();
            outcome.ErrorMessage.Should().Be("Invalid data: The input data does not contain any lines.");
            _missionServiceMock.Verify(x => x.SaveMissionsAsync(It.IsAny<IEnumerable<MissionCsv>>()), Times.Never);
        }

        [Fact]
        public async Task ReceiveAsync_WithValidFile_SavesAllLinesAndReturnsCounts()
        {
            // Arrange
            SetupBlobSave();
            IEnumerable<MissionCsv>? savedMissions = null;
            _missionServiceMock
                .Setup(x => x.SaveMissionsAsync(It.IsAny<IEnumerable<MissionCsv>>()))
                .Callback<IEnumerable<MissionCsv>>(m => savedMissions = m.ToList())
                .Returns(Task.CompletedTask);

            var data = BuildCsv(
                "123456;E1;PennylaneOfferCode;PennylaneProProductCode;12/01/2025;12/01/2027;INSERT",
                "123456;E2;PennylaneOfferCode;PennylaneLiteProductCode;12/01/2025;12/01/2027;INSERT",
                "123456;E0;PennylaneOfferCode;PennylaneLiteProductCode;12/01/2025;12/01/2027;DELETE");

            // Act
            var outcome = await _sut.ReceiveAsync(data);

            // Assert
            _blobStorageManagerMock.Verify(x => x.SaveFileAsync("Mission", data), Times.Once);
            outcome.IsAccepted.Should().BeTrue();
            outcome.Summary.Should().NotBeNull();
            outcome.Summary!.AcceptedLines.Should().Be(3);
            outcome.Summary.RejectedLines.Should().Be(0);
            outcome.Summary.Errors.Should().BeEmpty();
            savedMissions.Should().NotBeNull();
            savedMissions!.Select(m => m.EngagementCode).Should().BeEquivalentTo("E1", "E2", "E0");
        }

        [Fact]
        public async Task ReceiveAsync_WithInvalidLines_RejectsThemAndSavesTheOthers()
        {
            // Arrange
            SetupBlobSave();
            IEnumerable<MissionCsv>? savedMissions = null;
            _missionServiceMock
                .Setup(x => x.SaveMissionsAsync(It.IsAny<IEnumerable<MissionCsv>>()))
                .Callback<IEnumerable<MissionCsv>>(m => savedMissions = m.ToList())
                .Returns(Task.CompletedTask);

            var data = BuildCsv(
                "123456;E1;PennylaneOfferCode;P1;12/01/2025;12/01/2027;INSERT",
                "123456;;PennylaneOfferCode;P1;12/01/2025;12/01/2027;INSERT",
                "123456;E3;PennylaneOfferCode;P1;12/01/2025;12/01/2027;UPDATE",
                "123456;E4;PennylaneOfferCode;P1;12/01/2027;12/01/2025;INSERT");

            // Act
            var outcome = await _sut.ReceiveAsync(data);

            // Assert
            outcome.IsAccepted.Should().BeTrue();
            outcome.Summary!.AcceptedLines.Should().Be(1);
            outcome.Summary.RejectedLines.Should().Be(3);
            outcome.Summary.Errors.Should().HaveCount(3);
            savedMissions.Should().NotBeNull();
            savedMissions!.Select(m => m.EngagementCode).Should().BeEquivalentTo("E1");
        }

        [Fact]
        public async Task ReceiveAsync_WithOnlyInvalidLines_ReturnsCountsWithoutSaving()
        {
            // Arrange
            SetupBlobSave();

            // Act
            var outcome = await _sut.ReceiveAsync(BuildCsv("123456;;PennylaneOfferCode;P1;12/01/2025;12/01/2027;INSERT"));

            // Assert
            outcome.IsAccepted.Should().BeTrue();
            outcome.Summary!.AcceptedLines.Should().Be(0);
            outcome.Summary.RejectedLines.Should().Be(1);
            _missionServiceMock.Verify(x => x.SaveMissionsAsync(It.IsAny<IEnumerable<MissionCsv>>()), Times.Never);
        }

        [Fact]
        public async Task ReceiveAsync_WithValidFile_PostsTheBlobNameOnTheQueue()
        {
            // Arrange - the queue message is what triggers the publication without waiting for
            // the scheduled pass. Nothing else feeds that queue.
            SetupBlobSave();
            _missionServiceMock
                .Setup(x => x.SaveMissionsAsync(It.IsAny<IEnumerable<MissionCsv>>()))
                .Returns(Task.CompletedTask);

            var data = BuildCsv("123456;E1;PennylaneOfferCode;PennylaneProProductCode;12/01/2025;12/01/2027;INSERT");

            // Act
            await _sut.ReceiveAsync(data);

            // Assert
            _missionEventPublisherMock.Verify(
                x => x.SendMissionLinesBatchEvent("Mission_20260101_000000.csv"),
                Times.Once);
        }

        [Fact]
        public async Task ReceiveAsync_WithNoValidLine_DoesNotPostOnTheQueue()
        {
            // Arrange
            SetupBlobSave();
            var data = BuildCsv("123456;E1;PennylaneOfferCode;PennylaneProProductCode;99/99/9999;12/01/2027;INSERT");

            // Act
            await _sut.ReceiveAsync(data);

            // Assert
            _missionEventPublisherMock.Verify(
                x => x.SendMissionLinesBatchEvent(It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task ReceiveAsync_WhenTheQueueIsUnreachable_StillAcceptsTheDeposit()
        {
            // Arrange - the lines are persisted in READY, the scheduled pass will pick them up.
            // Failing the deposit would make the DS2I send the file again for nothing.
            SetupBlobSave();
            _missionServiceMock
                .Setup(x => x.SaveMissionsAsync(It.IsAny<IEnumerable<MissionCsv>>()))
                .Returns(Task.CompletedTask);
            _missionEventPublisherMock
                .Setup(x => x.SendMissionLinesBatchEvent(It.IsAny<string>()))
                .ThrowsAsync(new ServiceBusOperationException("queue unreachable"));

            var data = BuildCsv("123456;E1;PennylaneOfferCode;PennylaneProProductCode;12/01/2025;12/01/2027;INSERT");

            // Act
            var outcome = await _sut.ReceiveAsync(data);

            // Assert
            outcome.IsAccepted.Should().BeTrue();
            outcome.Summary!.AcceptedLines.Should().Be(1);
        }

        [Fact]
        public async Task ReceiveAsync_WithDuplicateEngagementCodes_KeepsTheFirstAndRejectsTheOthers()
        {
            // Arrange - a bulk insert of duplicates would violate UQ_Missions_Operation_EngagementCode
            // and fail the whole deposit: the extra lines must surface as validation errors instead.
            SetupBlobSave();
            IEnumerable<MissionCsv>? savedMissions = null;
            _missionServiceMock
                .Setup(x => x.SaveMissionsAsync(It.IsAny<IEnumerable<MissionCsv>>()))
                .Callback<IEnumerable<MissionCsv>>(m => savedMissions = m.ToList())
                .Returns(Task.CompletedTask);
            _missionServiceMock
                .Setup(x => x.GetExistingEngagementKeysAsync(It.IsAny<IEnumerable<MissionCsv>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            var data = BuildCsv(
                "123456;E1;PennylaneOfferCode;P1;12/01/2025;12/01/2027;INSERT",
                "123456;E2;PennylaneOfferCode;P1;12/01/2025;12/01/2027;INSERT",
                "123456;E1;PennylaneOfferCode;P2;12/01/2025;12/01/2027;INSERT");

            // Act
            var outcome = await _sut.ReceiveAsync(data);

            // Assert
            outcome.IsAccepted.Should().BeTrue();
            outcome.Summary!.AcceptedLines.Should().Be(2);
            outcome.Summary.RejectedLines.Should().Be(1);
            outcome.Summary.Errors.Should().ContainSingle();
            outcome.Summary.Errors[0].LineNumber.Should().Be(3);
            outcome.Summary.Errors[0].Errors.Should().ContainSingle()
                .Which.Should().Be("Duplicate EngagementCode 'E1' for operation 'INSERT'");
            savedMissions!.Select(m => m.EngagementCode).Should().BeEquivalentTo("E1", "E2");
        }

        [Fact]
        public async Task ReceiveAsync_WithSameEngagementCodeOnDifferentOperations_AcceptsBothLines()
        {
            // Arrange - the business key is (Operation, EngagementCode): INSERT then DELETE of the
            // same code are two distinct missions.
            SetupBlobSave();
            IEnumerable<MissionCsv>? savedMissions = null;
            _missionServiceMock
                .Setup(x => x.SaveMissionsAsync(It.IsAny<IEnumerable<MissionCsv>>()))
                .Callback<IEnumerable<MissionCsv>>(m => savedMissions = m.ToList())
                .Returns(Task.CompletedTask);
            _missionServiceMock
                .Setup(x => x.GetExistingEngagementKeysAsync(It.IsAny<IEnumerable<MissionCsv>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            var data = BuildCsv(
                "123456;E1;PennylaneOfferCode;P1;12/01/2025;12/01/2027;INSERT",
                "123456;E1;PennylaneOfferCode;P1;12/01/2025;12/01/2027;DELETE");

            // Act
            var outcome = await _sut.ReceiveAsync(data);

            // Assert
            outcome.IsAccepted.Should().BeTrue();
            outcome.Summary!.AcceptedLines.Should().Be(2);
            outcome.Summary.RejectedLines.Should().Be(0);
            savedMissions.Should().HaveCount(2);
        }

        [Fact]
        public async Task ReceiveAsync_WithEngagementCodeAlreadySaved_RejectsTheLineWithoutSaving()
        {
            // Arrange - typically the same file sent twice: the constraint violation must become a
            // validation error, not an exception.
            SetupBlobSave();
            _missionServiceMock
                .Setup(x => x.GetExistingEngagementKeysAsync(It.IsAny<IEnumerable<MissionCsv>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([("INSERT", "E1")]);

            var data = BuildCsv("123456;E1;PennylaneOfferCode;P1;12/01/2025;12/01/2027;INSERT");

            // Act
            var outcome = await _sut.ReceiveAsync(data);

            // Assert
            outcome.IsAccepted.Should().BeTrue();
            outcome.Summary!.AcceptedLines.Should().Be(0);
            outcome.Summary.RejectedLines.Should().Be(1);
            outcome.Summary.Errors.Should().ContainSingle();
            outcome.Summary.Errors[0].LineNumber.Should().Be(1);
            outcome.Summary.Errors[0].Errors.Should().ContainSingle()
                .Which.Should().Be("EngagementCode 'E1' with operation 'INSERT' already exists");
            _missionServiceMock.Verify(x => x.SaveMissionsAsync(It.IsAny<IEnumerable<MissionCsv>>()), Times.Never);
        }

        [Fact]
        public async Task ReceiveAsync_WithOnlyOneOfTwoLinesAlreadySaved_SavesTheOther()
        {
            // Arrange
            SetupBlobSave();
            IEnumerable<MissionCsv>? savedMissions = null;
            _missionServiceMock
                .Setup(x => x.SaveMissionsAsync(It.IsAny<IEnumerable<MissionCsv>>()))
                .Callback<IEnumerable<MissionCsv>>(m => savedMissions = m.ToList())
                .Returns(Task.CompletedTask);
            _missionServiceMock
                .Setup(x => x.GetExistingEngagementKeysAsync(It.IsAny<IEnumerable<MissionCsv>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([("INSERT", "E1")]);

            var data = BuildCsv(
                "123456;E1;PennylaneOfferCode;P1;12/01/2025;12/01/2027;INSERT",
                "123456;E2;PennylaneOfferCode;P1;12/01/2025;12/01/2027;INSERT");

            // Act
            var outcome = await _sut.ReceiveAsync(data);

            // Assert
            outcome.IsAccepted.Should().BeTrue();
            outcome.Summary!.AcceptedLines.Should().Be(1);
            outcome.Summary.RejectedLines.Should().Be(1);
            savedMissions!.Select(m => m.EngagementCode).Should().BeEquivalentTo("E2");
        }

        private void SetupBlobSave()
        {
            _blobStorageManagerMock.Setup(x => x.SaveFileAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("Mission_20260101_000000.csv");
        }

        private static string BuildCsv(params string[] lines)
        {
            var csvContent = new StringBuilder();
            csvContent.AppendLine(CsvHeader);
            foreach (var line in lines)
            {
                csvContent.AppendLine(line);
            }

            return csvContent.ToString();
        }
    }
}

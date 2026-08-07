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
        private readonly MissionReceptionService _sut;

        public MissionReceptionServiceTests()
        {
            _blobStorageManagerMock = new Mock<IBlobStorageManager>(MockBehavior.Strict);
            _missionServiceMock = new Mock<IMissionService>();
            var logger = new Mock<ILogger<MissionReceptionService>>();

            _sut = new MissionReceptionService(
                _blobStorageManagerMock.Object,
                _missionServiceMock.Object,
                new ValidationHelper<RefMissionCsv>(),
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
            _missionServiceMock.Verify(x => x.SaveMissionsAsync(It.IsAny<IEnumerable<RefMissionCsv>>()), Times.Never);
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
            _missionServiceMock.Verify(x => x.SaveMissionsAsync(It.IsAny<IEnumerable<RefMissionCsv>>()), Times.Never);
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
            _missionServiceMock.Verify(x => x.SaveMissionsAsync(It.IsAny<IEnumerable<RefMissionCsv>>()), Times.Never);
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
            _missionServiceMock.Verify(x => x.SaveMissionsAsync(It.IsAny<IEnumerable<RefMissionCsv>>()), Times.Never);
        }

        [Fact]
        public async Task ReceiveAsync_WithValidFile_SavesAllLinesAndReturnsCounts()
        {
            // Arrange
            SetupBlobSave();
            IEnumerable<RefMissionCsv>? savedMissions = null;
            _missionServiceMock
                .Setup(x => x.SaveMissionsAsync(It.IsAny<IEnumerable<RefMissionCsv>>()))
                .Callback<IEnumerable<RefMissionCsv>>(m => savedMissions = m.ToList())
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
            IEnumerable<RefMissionCsv>? savedMissions = null;
            _missionServiceMock
                .Setup(x => x.SaveMissionsAsync(It.IsAny<IEnumerable<RefMissionCsv>>()))
                .Callback<IEnumerable<RefMissionCsv>>(m => savedMissions = m.ToList())
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
            _missionServiceMock.Verify(x => x.SaveMissionsAsync(It.IsAny<IEnumerable<RefMissionCsv>>()), Times.Never);
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

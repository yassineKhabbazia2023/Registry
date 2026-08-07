//// <copyright file="MissionServiceTests.cs" company="Pulse">
//// Copyright (c) Pulse. All rights reserved.
//// </copyright>

using Application.Interfaces;
using Application.Models;
using Application.Services;
using FluentAssertions;
using Moq;
using Pulse.Registry.Domain.Entities;

namespace Registry.Application.Tests.Services
{
    public class MissionServiceTests
    {
        private readonly Mock<IMissionRepository> _missionRepositoryMock;
        private readonly MissionService _sut;

        public MissionServiceTests()
        {
            _missionRepositoryMock = new Mock<IMissionRepository>();
            _sut = new MissionService(_missionRepositoryMock.Object);
        }

        [Fact]
        public async Task SaveMissionsAsync_WithValidCsvs_AddsMappedEntitiesToRepository()
        {
            // Arrange
            var csvs = new[]
            {
                new RefMissionCsv { AccountNumber = "123456", EngagementCode = "E1", OfferCode = "O1", StartDate = "12/01/2025", EndDate = "12/01/2027", Operation = "INSERT" },
                new RefMissionCsv { AccountNumber = "123456", EngagementCode = "E2", OfferCode = "O1", StartDate = "12/01/2025", EndDate = "12/01/2027", Operation = "DELETE" },
            };
            IEnumerable<RefMissionEntity>? savedMissions = null;
            _missionRepositoryMock
                .Setup(r => r.AddMissionsAsync(It.IsAny<IEnumerable<RefMissionEntity>>()))
                .Callback<IEnumerable<RefMissionEntity>>(m => savedMissions = m)
                .Returns(Task.CompletedTask);

            // Act
            await _sut.SaveMissionsAsync(csvs);

            // Assert
            savedMissions.Should().NotBeNull();
            savedMissions.Should().HaveCount(2);
            savedMissions!.Select(m => m.EngagementCode).Should().BeEquivalentTo("E1", "E2");
        }
    }
}

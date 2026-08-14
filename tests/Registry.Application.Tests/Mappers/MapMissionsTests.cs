//// <copyright file="MapMissionsTests.cs" company="Pulse">
//// Copyright (c) Pulse. All rights reserved.
//// </copyright>

using Application.Mappers;
using Application.Models;
using FluentAssertions;

namespace Registry.Application.Tests.Mappers
{
    public class MapMissionsTests
    {
        [Fact]
        public void MapMissionCsvsToMissionEntities_WithValidCsvs_MapsAllFields()
        {
            // Arrange
            var csv = new MissionCsv
            {
                AccountNumber = "123456",
                EngagementCode = "E1",
                OfferCode = "PennylaneOfferCode",
                ProductCode = "PennylaneProProductCode",
                StartDate = "12/01/2025",
                EndDate = "12/01/2027",
                Operation = "insert",
            };

            // Act
            var results = new[] { csv }.MapMissionCsvsToMissionEntities().ToList();

            // Assert
            results.Should().HaveCount(1);
            var mission = results[0];
            mission.AccountNumber.Should().Be("123456");
            mission.EngagementCode.Should().Be("E1");
            mission.OfferCode.Should().Be("PennylaneOfferCode");
            mission.ProductCode.Should().Be("PennylaneProProductCode");
            mission.StartDate.Should().Be(new DateTime(2025, 1, 12));
            mission.EndDate.Should().Be(new DateTime(2027, 1, 12));
            mission.Operation.Should().Be("INSERT");
            mission.CreatedOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            mission.RegistryMissionId.Should().Be(0, "l'identifiant est genere par la base a l'insertion");
        }

        [Fact]
        public void MapMissionCsvsToMissionEntities_WithEmptyProductCode_MapsItToNull()
        {
            // Arrange
            var csv = new MissionCsv
            {
                AccountNumber = "123456",
                EngagementCode = "E1",
                OfferCode = "PennylaneOfferCode",
                ProductCode = string.Empty,
                StartDate = "12/01/2025",
                EndDate = "12/01/2027",
                Operation = "INSERT",
            };

            // Act
            var results = new[] { csv }.MapMissionCsvsToMissionEntities().ToList();

            // Assert
            results[0].ProductCode.Should().BeNull();
        }

        [Fact]
        public void MapMissionCsvsToMissionEntities_WithBothOperations_KeepsEachOperationOnItsOwnEntity()
        {
            // Arrange : le meme code engagement revient en INSERT puis en DELETE, ce sont deux lignes du CSV
            var csvs = new[]
            {
                new MissionCsv { AccountNumber = "123456", EngagementCode = "E1", OfferCode = "O1", StartDate = "12/01/2025", EndDate = "12/01/2027", Operation = "INSERT" },
                new MissionCsv { AccountNumber = "123456", EngagementCode = "E1", OfferCode = "O1", StartDate = "12/01/2025", EndDate = "12/01/2027", Operation = "DELETE" },
            };

            // Act
            var results = csvs.MapMissionCsvsToMissionEntities().ToList();

            // Assert
            results.Should().HaveCount(2);
            results.Select(m => m.Operation).Should().BeEquivalentTo("INSERT", "DELETE");
        }
    }
}

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
            var csv = new RefMissionCsv
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
            var entities = new[] { csv }.MapMissionCsvsToMissionEntities().ToList();

            // Assert
            entities.Should().HaveCount(1);
            var entity = entities[0];
            entity.AccountNumber.Should().Be("123456");
            entity.EngagementCode.Should().Be("E1");
            entity.OfferCode.Should().Be("PennylaneOfferCode");
            entity.ProductCode.Should().Be("PennylaneProProductCode");
            entity.StartDate.Should().Be(new DateTime(2025, 1, 12));
            entity.EndDate.Should().Be(new DateTime(2027, 1, 12));
            entity.OperationType.Should().Be("INSERT");
            entity.EntityId.Should().NotBeEmpty();
            entity.OperationDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            entity.ValidationDate.Should().BeNull();
        }

        [Fact]
        public void MapMissionCsvsToMissionEntities_WithEmptyProductCode_MapsItToNull()
        {
            // Arrange
            var csv = new RefMissionCsv
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
            var entities = new[] { csv }.MapMissionCsvsToMissionEntities().ToList();

            // Assert
            entities[0].ProductCode.Should().BeNull();
        }

        [Fact]
        public void MapMissionCsvsToMissionEntities_WithSeveralCsvs_GeneratesDistinctEntityIds()
        {
            // Arrange
            var csvs = new[]
            {
                new RefMissionCsv { AccountNumber = "123456", EngagementCode = "E1", OfferCode = "O1", StartDate = "12/01/2025", EndDate = "12/01/2027", Operation = "INSERT" },
                new RefMissionCsv { AccountNumber = "123456", EngagementCode = "E2", OfferCode = "O1", StartDate = "12/01/2025", EndDate = "12/01/2027", Operation = "DELETE" },
            };

            // Act
            var entities = csvs.MapMissionCsvsToMissionEntities().ToList();

            // Assert
            entities.Should().HaveCount(2);
            entities[0].EntityId.Should().NotBe(entities[1].EntityId);
        }
    }
}

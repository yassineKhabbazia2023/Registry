// <copyright file="MissionRepositoryTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using AutoFixture;
using Application.Consts;
using FluentAssertions;
using Infrastructure.Repository;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Pulse.Registry.Domain.Context;
using Pulse.Registry.Domain.Entities;

namespace Registry.Infrastructure.Tests.Repository;

public class MissionRepositoryTests
{
    private readonly Fixture _fixture;

    public MissionRepositoryTests()
    {
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList().ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    private static DbContextOptions<RefContext> CreateSqliteInMemoryOptions()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return new DbContextOptionsBuilder<RefContext>()
            .UseSqlite(connection)
            .Options;
    }

    private MissionEntity NewMission(string engagementCode, string operation)
        => _fixture.Build<MissionEntity>()
            .Without(m => m.RegistryMissionId)
            .With(m => m.AccountNumber, "123456")
            .With(m => m.EngagementCode, engagementCode)
            .With(m => m.OfferCode, "PennylaneOfferCode")
            .With(m => m.Operation, operation)
            .Create();

    [Fact]
    public async Task AddMissionsAsync_WithEntities_BulkInsertsThemAndOpensTheirProcessingLine()
    {
        // Arrange
        var missions = new List<MissionEntity>
        {
            NewMission("E1", OperationAction.Insert),
            NewMission("E2", OperationAction.Insert),
            NewMission("E3", OperationAction.Insert),
        };
        var options = CreateSqliteInMemoryOptions();

        using var context = new TestRefContext(options);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        var repository = new MissionRepository(context);

        // Act
        await repository.AddMissionsAsync(missions);

        // Assert
        var inserted = await context.MissionEntity.ToListAsync();
        inserted.Should().HaveCount(3);
        inserted.Select(m => m.EngagementCode).Should().BeEquivalentTo("E1", "E2", "E3");

        var processings = await context.MissionProcessingEntity.ToListAsync();
        processings.Should().HaveCount(3);
        processings.Should().OnlyContain(p => p.Status == ProcessStatus.Ready);
        processings.Select(p => p.RegistryMissionId).Should().BeEquivalentTo(inserted.Select(m => m.RegistryMissionId));
    }

    [Fact]
    public async Task AddMissionsAsync_WithSameEngagementInsertedThenDeleted_InsertsBothLines()
    {
        // Arrange : le meme code engagement en INSERT puis en DELETE, ce sont deux missions distinctes
        var missions = new List<MissionEntity>
        {
            NewMission("E1", OperationAction.Insert),
            NewMission("E1", OperationAction.Delete),
        };
        var options = CreateSqliteInMemoryOptions();

        using var context = new TestRefContext(options);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        var repository = new MissionRepository(context);

        // Act
        await repository.AddMissionsAsync(missions);

        // Assert
        var inserted = await context.MissionEntity.ToListAsync();
        inserted.Should().HaveCount(2);
        inserted.Select(m => m.Operation).Should().BeEquivalentTo(OperationAction.Insert, OperationAction.Delete);
    }

    [Fact]
    public async Task AddMissionsAsync_WithNoEntity_DoesNothing()
    {
        // Arrange
        var options = CreateSqliteInMemoryOptions();

        using var context = new TestRefContext(options);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        var repository = new MissionRepository(context);

        // Act
        await repository.AddMissionsAsync([]);

        // Assert
        (await context.MissionEntity.ToListAsync()).Should().BeEmpty();
        (await context.MissionProcessingEntity.ToListAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task GetExistingEngagementKeysAsync_ReturnsOnlyThePersistedPairs()
    {
        // Arrange
        var options = CreateSqliteInMemoryOptions();

        using var context = new TestRefContext(options);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        context.MissionEntity.AddRange(
            NewMission("E1", OperationAction.Insert),
            NewMission("E2", OperationAction.Delete));
        await context.SaveChangesAsync();
        var repository = new MissionRepository(context);

        // Act
        var keys = await repository.GetExistingEngagementKeysAsync(["E1", "E2", "E3"]);

        // Assert
        keys.Should().BeEquivalentTo(new List<(string Operation, string EngagementCode)>
        {
            (OperationAction.Insert, "E1"),
            (OperationAction.Delete, "E2"),
        });
    }

    [Fact]
    public async Task GetExistingEngagementKeysAsync_WithNoCode_DoesNotHitTheDatabase()
    {
        // Arrange
        var options = CreateSqliteInMemoryOptions();

        using var context = new TestRefContext(options);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        var repository = new MissionRepository(context);

        // Act
        var keys = await repository.GetExistingEngagementKeysAsync([]);

        // Assert
        keys.Should().BeEmpty();
    }
}

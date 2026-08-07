// <copyright file="MissionRepositoryTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using AutoFixture;
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

    [Fact]
    public async Task AddMissionsAsync_WithEntities_BulkInsertsThem()
    {
        // Arrange
        var missions = _fixture.CreateMany<RefMissionEntity>(3).ToList();
        var options = CreateSqliteInMemoryOptions();

        using var context = new TestRefContext(options);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        var repository = new MissionRepository(context);

        // Act
        await repository.AddMissionsAsync(missions);

        // Assert
        var inserted = await context.RefMissionEntity.ToListAsync();
        inserted.Should().HaveCount(3);
        inserted.Select(m => m.EntityId).Should().BeEquivalentTo(missions.Select(m => m.EntityId));
    }

    [Fact]
    public async Task AddMissionsAsync_WithSameOfferOnTwoEngagements_InsertsBothLines()
    {
        // Arrange
        var missions = new List<RefMissionEntity>
        {
            _fixture.Build<RefMissionEntity>().With(m => m.AccountNumber, "123456").With(m => m.EngagementCode, "E1").With(m => m.OfferCode, "PennylaneOfferCode").Create(),
            _fixture.Build<RefMissionEntity>().With(m => m.AccountNumber, "123456").With(m => m.EngagementCode, "E2").With(m => m.OfferCode, "PennylaneOfferCode").Create(),
        };
        var options = CreateSqliteInMemoryOptions();

        using var context = new TestRefContext(options);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        var repository = new MissionRepository(context);

        // Act
        await repository.AddMissionsAsync(missions);

        // Assert
        var inserted = await context.RefMissionEntity.ToListAsync();
        inserted.Should().HaveCount(2);
        inserted.Select(m => m.EngagementCode).Should().BeEquivalentTo("E1", "E2");
    }
}

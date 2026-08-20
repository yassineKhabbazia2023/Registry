// <copyright file="MissionProcessingRepositoryTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Consts;
using FluentAssertions;
using Infrastructure.Repository;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Pulse.Registry.Domain.Context;
using Pulse.Registry.Domain.Entities;
using Pulse.Registry.Domain.Entities.Accounts;
using Pulse.Registry.Domain.Entities.Audits;
using Pulse.Registry.Domain.Entities.Contacts;

namespace Registry.Infrastructure.Tests.Repository;

public class TestMissionProcessingContext : RefContext
{
    public TestMissionProcessingContext(DbContextOptions<RefContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MissionEntity>(entity =>
        {
            entity.HasKey(e => e.RegistryMissionId).HasName("PK_Missions");
            entity.ToTable("Missions", "mission");
            entity.HasIndex(e => new { e.Operation, e.EngagementCode }, "UQ_Missions_Operation_EngagementCode").IsUnique();
            entity.Property(e => e.RegistryMissionId).ValueGeneratedOnAdd();
            entity.Property(e => e.AccountNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.EngagementCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.OfferCode).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ProductCode).HasMaxLength(100);
            entity.Property(e => e.Operation).IsRequired().HasMaxLength(20);
        });

        modelBuilder.Entity<MissionProcessingEntity>(entity =>
        {
            entity.HasKey(e => e.RegistryMissionId).HasName("PK_Processing");
            entity.ToTable("Processing", "mission");
            entity.HasIndex(e => new { e.Status, e.PublishedOn }, "IX_Processing_Status_PublishedOn");
            entity.Property(e => e.RegistryMissionId).ValueGeneratedNever();
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Reason).HasMaxLength(500);

            entity.HasOne(d => d.Mission).WithMany()
                .HasForeignKey(d => d.RegistryMissionId)
                .HasConstraintName("FK_Processing_Missions");
        });

        modelBuilder.Ignore<InvoiceEntity>();
        modelBuilder.Ignore<ContactEntity>();
        modelBuilder.Ignore<DeepValidationEntity>();
        modelBuilder.Ignore<RegOperationEntity>();
        modelBuilder.Ignore<RefContactEntity>();
        modelBuilder.Ignore<RefAccountEntity>();
        modelBuilder.Ignore<RefRoleEntity>();
        modelBuilder.Ignore<RefOfferEntity>();
        modelBuilder.Ignore<AccountEntity>();
        modelBuilder.Ignore<RoleEntity>();
        modelBuilder.Ignore<HubSpotFormEntity>();
    }
}

public class MissionProcessingRepositoryTests
{
    private static TestMissionProcessingContext CreateSqliteContext()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<RefContext>()
            .UseSqlite(connection)
            .Options;

        var context = new TestMissionProcessingContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task GetByStatusAsync_WithMultipleStatuses_ReturnsRowsMatchingAny()
    {
        // Arrange
        using var context = CreateSqliteContext();
        context.MissionEntity.AddRange(Mission(1, "E1"), Mission(2, "E2"), Mission(3, "E3"));
        context.MissionProcessingEntity.AddRange(
            Processing(1, ProcessStatus.Ready),
            Processing(2, ProcessStatus.Failed),
            Processing(3, ProcessStatus.Succeeded));
        await context.SaveChangesAsync();

        var repository = new MissionProcessingRepository(context);

        // Act
        var result = await repository.GetByStatusAsync([ProcessStatus.Ready, ProcessStatus.Failed], 0, 10);

        // Assert
        result.Select(p => p.RegistryMissionId).Should().BeEquivalentTo([1, 2]);
    }

    [Fact]
    public async Task MarkUnacknowledgedAsFailedAsync_WhenSentPastThreshold_MarksItFailedWithReason()
    {
        // Arrange
        using var context = CreateSqliteContext();
        context.MissionEntity.Add(Mission(1, "E1"));
        context.MissionProcessingEntity.Add(Processing(1, ProcessStatus.Sent, publishedOn: DateTime.UtcNow.AddMinutes(-90)));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repository = new MissionProcessingRepository(context);

        // Act
        var failedCount = await repository.MarkUnacknowledgedAsFailedAsync(60);

        // Assert
        failedCount.Should().Be(1);
        var stored = await context.MissionProcessingEntity.AsNoTracking().SingleAsync(p => p.RegistryMissionId == 1);
        stored.Status.Should().Be(ProcessStatus.Failed);
        stored.Reason.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task MarkUnacknowledgedAsFailedAsync_WhenSentWithinThreshold_LeavesItUnchanged()
    {
        // Arrange
        using var context = CreateSqliteContext();
        context.MissionEntity.Add(Mission(1, "E1"));
        context.MissionProcessingEntity.Add(Processing(1, ProcessStatus.Sent, publishedOn: DateTime.UtcNow.AddMinutes(-10)));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repository = new MissionProcessingRepository(context);

        // Act
        var failedCount = await repository.MarkUnacknowledgedAsFailedAsync(60);

        // Assert
        failedCount.Should().Be(0);
        var stored = await context.MissionProcessingEntity.AsNoTracking().SingleAsync(p => p.RegistryMissionId == 1);
        stored.Status.Should().Be(ProcessStatus.Sent);
    }

    [Fact]
    public async Task MarkUnacknowledgedAsFailedAsync_WhenStatusIsNotSent_LeavesItUnchanged()
    {
        // Arrange
        using var context = CreateSqliteContext();
        context.MissionEntity.Add(Mission(1, "E1"));
        context.MissionProcessingEntity.Add(Processing(1, ProcessStatus.Ready, publishedOn: DateTime.UtcNow.AddMinutes(-90)));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repository = new MissionProcessingRepository(context);

        // Act
        var failedCount = await repository.MarkUnacknowledgedAsFailedAsync(60);

        // Assert
        failedCount.Should().Be(0);
        var stored = await context.MissionProcessingEntity.AsNoTracking().SingleAsync(p => p.RegistryMissionId == 1);
        stored.Status.Should().Be(ProcessStatus.Ready);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task MarkUnacknowledgedAsFailedAsync_WithNonPositiveTimeout_Throws(int ackTimeoutMinutes)
    {
        // Arrange - a timeout of 0 would flip every SENT line ever, including ones sent seconds
        // ago; this must fail fast rather than mass-reap on a misconfigured value
        using var context = CreateSqliteContext();
        var repository = new MissionProcessingRepository(context);

        // Act
        var act = () => repository.MarkUnacknowledgedAsFailedAsync(ackTimeoutMinutes);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    private static MissionEntity Mission(int registryMissionId, string engagementCode) => new()
    {
        RegistryMissionId = registryMissionId,
        AccountNumber = "123456",
        EngagementCode = engagementCode,
        OfferCode = "PennylaneOfferCode",
        StartDate = new DateTime(2026, 1, 1),
        EndDate = new DateTime(2026, 6, 30),
        Operation = OperationAction.Insert,
        CreatedOn = new DateTime(2026, 1, 1),
    };

    private static MissionProcessingEntity Processing(int registryMissionId, string status, DateTime? publishedOn = null) => new()
    {
        RegistryMissionId = registryMissionId,
        Status = status,
        PublishedOn = publishedOn,
    };
}

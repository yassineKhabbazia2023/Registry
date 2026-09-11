// <copyright file="HubSpotFormRepositoryTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

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

public class TestHubSpotFormContext : RefContext
{
    public TestHubSpotFormContext(DbContextOptions<RefContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<HubSpotFormEntity>(entity =>
        {
            entity.HasKey(e => new { e.AccountNumber, e.SubmittedBy });
            entity.ToTable("HubspotForm", "ext");
            entity.Property(e => e.AccountNumber).IsRequired().HasMaxLength(100);
            entity.Property(e => e.SubmittedBy).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FormData).IsRequired();
        });

        modelBuilder.Ignore<ContactEntity>();
        modelBuilder.Ignore<DeepValidationEntity>();
        modelBuilder.Ignore<RegOperationEntity>();
        modelBuilder.Ignore<RefContactEntity>();
        modelBuilder.Ignore<RefAccountEntity>();
        modelBuilder.Ignore<RefRoleEntity>();
        modelBuilder.Ignore<MissionEntity>();
        modelBuilder.Ignore<MissionProcessingEntity>();
        modelBuilder.Ignore<RefOfferEntity>();
        modelBuilder.Ignore<AccountEntity>();
        modelBuilder.Ignore<RoleEntity>();
        modelBuilder.Ignore<InvoiceEntity>();
    }
}

public class HubSpotFormRepositoryTests
{
    private const string AccountNumber = "C000123";

    private static TestHubSpotFormContext CreateSqliteContext()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<RefContext>()
            .UseSqlite(connection)
            .Options;

        var context = new TestHubSpotFormContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static HubSpotFormEntity CreateSubmission(string accountNumber, string submittedBy, bool dispatchState = true)
        => new()
        {
            AccountNumber = accountNumber,
            SubmittedBy = submittedBy,
            SubmittedAt = DateTime.UtcNow,
            HubSpotDispatchState = dispatchState,
            FormData = "{}"
        };

    [Fact]
    public async Task DeleteSubmissionsAsync_WithExistingSubmissions_RemovesOnlyThatAccountRows()
    {
        // Arrange
        using var context = CreateSqliteContext();
        context.HubSpotFormEntity.AddRange(
            CreateSubmission(AccountNumber, "user1@test.fr"),
            CreateSubmission(AccountNumber, "user2@test.fr", dispatchState: false),
            CreateSubmission("C000456", "user3@test.fr"));
        await context.SaveChangesAsync();

        var repository = new HubSpotFormRepository(context);

        // Act
        await repository.DeleteSubmissionsAsync(AccountNumber);

        // Assert
        var remaining = await context.HubSpotFormEntity.ToListAsync();
        remaining.Should().ContainSingle();
        remaining[0].AccountNumber.Should().Be("C000456");
    }

    [Fact]
    public async Task DeleteSubmissionsAsync_WithNoSubmissions_DoesNotThrow()
    {
        // Arrange
        using var context = CreateSqliteContext();
        var repository = new HubSpotFormRepository(context);

        // Act
        var act = () => repository.DeleteSubmissionsAsync(AccountNumber);

        // Assert
        await act.Should().NotThrowAsync();
    }
}

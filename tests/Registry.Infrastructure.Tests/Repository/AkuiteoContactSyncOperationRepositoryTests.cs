// <copyright file="AkuiteoContactSyncOperationRepositoryTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Consts;
using Infrastructure.Repository;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Pulse.Registry.Domain.Context;
using Pulse.Registry.Domain.Entities.Accounts;
using Pulse.Registry.Domain.Entities.Audits;
using Pulse.Registry.Domain.Entities.Contacts;

namespace Registry.Infrastructure.Tests.Repository;

public class AkuiteoContactSyncOperationRepositoryTests
{
    [Fact]
    public async Task AddIfNotExistsAsync_WithSameSourceEvent_ShouldRemainIdempotent()
    {
        await using var context = CreateContext();
        var repository = new AkuiteoContactSyncOperationRepository(context);
        var sourceEventId = Guid.NewGuid();

        var firstAdded = await repository.AddIfNotExistsAsync(CreateOperation(sourceEventId));
        var duplicateAdded = await repository.AddIfNotExistsAsync(CreateOperation(sourceEventId));

        Assert.True(firstAdded);
        Assert.False(duplicateAdded);
        Assert.Single(await context.AkuiteoContactSyncOperationEntity.ToListAsync());
    }

    [Theory]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    public async Task GetEligiblePendingAsync_WithMissingDependency_ShouldNotReturnOperation(
        bool accountExists,
        bool contactExists,
        bool roleExists)
    {
        await using var context = CreateContext();
        var operation = CreateOperation(Guid.NewGuid());
        context.AkuiteoContactSyncOperationEntity.Add(operation);
        AddDependencies(context, operation, accountExists, contactExists, roleExists);
        await context.SaveChangesAsync();

        var result = await new AkuiteoContactSyncOperationRepository(context)
            .GetEligiblePendingAsync();

        Assert.Empty(result);
        Assert.Equal(AkuiteoContactSyncOperationStatus.Pending, operation.Status);
    }

    [Fact]
    public async Task GetEligiblePendingAsync_WithAccountContactAndRole_ShouldReturnOperation()
    {
        await using var context = CreateContext();
        var operation = CreateOperation(Guid.NewGuid());
        context.AkuiteoContactSyncOperationEntity.Add(operation);
        AddDependencies(context, operation, true, true, true);
        await context.SaveChangesAsync();
        var result = await new AkuiteoContactSyncOperationRepository(context)
            .GetEligiblePendingAsync();

        var eligibleOperation = Assert.Single(result);
        Assert.Equal(operation.Id, eligibleOperation.Id);
        Assert.Equal(AkuiteoContactSyncOperationStatus.Pending, eligibleOperation.Status);
    }

    [Fact]
    public async Task GetEligiblePendingAsync_WithSkippedOperation_ShouldNotReturnOperation()
    {
        await using var context = CreateContext();
        var operation = CreateOperation(Guid.NewGuid());
        operation.Status = AkuiteoContactSyncOperationStatus.Skipped;
        context.AkuiteoContactSyncOperationEntity.Add(operation);
        AddDependencies(context, operation, true, true, true);
        await context.SaveChangesAsync();

        var result = await new AkuiteoContactSyncOperationRepository(context)
            .GetEligiblePendingAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetEligiblePendingAsync_WhenDependenciesArriveLater_ShouldReturnPendingOperation()
    {
        await using var context = CreateContext();
        var operation = CreateOperation(Guid.NewGuid());
        context.AkuiteoContactSyncOperationEntity.Add(operation);
        await context.SaveChangesAsync();
        var repository = new AkuiteoContactSyncOperationRepository(context);

        var unavailableResult = await repository.GetEligiblePendingAsync();
        AddDependencies(context, operation, true, true, true);
        await context.SaveChangesAsync();
        var eligibleResult = await repository.GetEligiblePendingAsync();

        Assert.Empty(unavailableResult);
        Assert.Equal(operation.Id, Assert.Single(eligibleResult).Id);
    }

    [Fact]
    public async Task TryMarkProcessingAsync_WithPendingOperation_ShouldUpdateOperationStatus()
    {
        await using var context = CreateProcessingContext();
        var operation = CreateOperation(Guid.NewGuid());
        context.AkuiteoContactSyncOperationEntity.Add(operation);
        await context.SaveChangesAsync();
        var repository = new AkuiteoContactSyncOperationRepository(context);

        var marked = await repository.TryMarkProcessingAsync(operation.Id);
        await context.Entry(operation).ReloadAsync();

        Assert.True(marked);
        Assert.Equal(AkuiteoContactSyncOperationStatus.Processing, operation.Status);
    }

    [Fact]
    public async Task TryMarkProcessingAsync_WithAlreadyClaimedOperation_ShouldReturnFalse()
    {
        await using var context = CreateProcessingContext();
        var operation = CreateOperation(Guid.NewGuid());
        operation.Status = AkuiteoContactSyncOperationStatus.Processing;
        context.AkuiteoContactSyncOperationEntity.Add(operation);
        await context.SaveChangesAsync();
        var repository = new AkuiteoContactSyncOperationRepository(context);

        var marked = await repository.TryMarkProcessingAsync(operation.Id);

        Assert.False(marked);
    }

    [Fact]
    public async Task MarkSentAsync_ShouldStoreTerminalSuccessData()
    {
        await using var context = CreateContext();
        var operation = CreateOperation(Guid.NewGuid());
        context.AkuiteoContactSyncOperationEntity.Add(operation);
        await context.SaveChangesAsync();
        var repository = new AkuiteoContactSyncOperationRepository(context);

        await repository.MarkSentAsync(operation.Id, "500145940");

        Assert.Equal(AkuiteoContactSyncOperationStatus.Sent, operation.Status);
        Assert.Equal("500145940", operation.AkuiteoContactId);
        Assert.NotNull(operation.SentAt);
        Assert.Null(operation.LastError);
    }

    [Fact]
    public async Task MarkFailedAsync_ShouldStoreTerminalError()
    {
        await using var context = CreateContext();
        var operation = CreateOperation(Guid.NewGuid());
        context.AkuiteoContactSyncOperationEntity.Add(operation);
        await context.SaveChangesAsync();
        var repository = new AkuiteoContactSyncOperationRepository(context);

        await repository.MarkFailedAsync(operation.Id, "Akuiteo is unavailable.");

        Assert.Equal(AkuiteoContactSyncOperationStatus.Failed, operation.Status);
        Assert.Equal("Akuiteo is unavailable.", operation.LastError);
        Assert.Null(operation.SentAt);
    }

    private static RefContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<RefContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new RefContext(options);
    }

    private static RefContext CreateProcessingContext()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<RefContext>()
            .UseSqlite(connection, contextOwnsConnection: true)
            .Options;
        var context = new RefContext(options);
        context.Database.ExecuteSqlRaw("""
            CREATE TABLE "AkuiteoContactSyncOperations" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_AkuiteoContactSyncOperations" PRIMARY KEY AUTOINCREMENT,
                "SourceEventId" TEXT NOT NULL,
                "AccountId" INTEGER NOT NULL,
                "AccountNumber" TEXT NOT NULL,
                "AccountType" TEXT NOT NULL,
                "ContactId" INTEGER NOT NULL,
                "ContactEmail" TEXT NOT NULL,
                "ContactFlagPortailFactures" INTEGER NULL,
                "IsSignatory" INTEGER NULL,
                "Reason" TEXT NOT NULL,
                "Status" TEXT NOT NULL,
                "LastError" TEXT NULL,
                "CreatedAt" TEXT NOT NULL,
                "UpdatedAt" TEXT NOT NULL,
                "SentAt" TEXT NULL,
                "AkuiteoContactId" TEXT NULL
            );
            """);
        return context;
    }

    private static AkuiteoContactSyncOperationEntity CreateOperation(Guid sourceEventId)
    {
        return new AkuiteoContactSyncOperationEntity
        {
            SourceEventId = sourceEventId,
            AccountId = 792480503,
            AccountNumber = "9010001710",
            AccountType = "PROSPECT",
            ContactId = 123,
            ContactEmail = "contact@example.com",
            ContactFlagPortailFactures = true,
            IsSignatory = true,
            Reason = AkuiteoContactSyncReason.RoleCreatedFromPulse,
            Status = AkuiteoContactSyncOperationStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private static void AddDependencies(
        RefContext context,
        AkuiteoContactSyncOperationEntity operation,
        bool accountExists,
        bool contactExists,
        bool roleExists)
    {
        if (accountExists)
        {
            context.AccountEntity.Add(new AccountEntity
            {
                AccountId = operation.AccountId,
                AccountNumber = operation.AccountNumber,
                LegalName = "Test account"
            });
        }

        if (contactExists)
        {
            context.ContactEntity.Add(new ContactEntity
            {
                ContactId = operation.ContactId,
                Email = operation.ContactEmail,
                Type = "Customer"
            });
        }

        if (roleExists)
        {
            context.RoleEntity.Add(new RoleEntity
            {
                AccountId = operation.AccountId,
                AccountNumber = operation.AccountNumber,
                ContactId = operation.ContactId,
                ContactEmail = operation.ContactEmail
            });
        }
    }
}

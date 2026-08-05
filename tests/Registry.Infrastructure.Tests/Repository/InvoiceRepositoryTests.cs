// <copyright file="InvoiceRepositoryTests.cs" company="Pulse">
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

public class TestInvoiceContext : RefContext
{
    public TestInvoiceContext(DbContextOptions<RefContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InvoiceEntity>(entity =>
        {
            entity.ToTable("Invoice", "ref");
            entity.HasKey(e => e.InvoiceId);
            entity.Property(e => e.InvoiceId).ValueGeneratedOnAdd();
            entity.HasIndex(e => new { e.Operation, e.AccountNumber, e.InvoiceNumber }).IsUnique();
        });

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

public class InvoiceRepositoryTests
{
    private static TestInvoiceContext CreateSqliteContext()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<RefContext>()
            .UseSqlite(connection)
            .Options;

        var context = new TestInvoiceContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task AddRangeAsync_WithInvoices_PersistsThem()
    {
        // Arrange
        using var context = CreateSqliteContext();
        var repository = new InvoiceRepository(context);
        var invoices = new List<InvoiceEntity>
        {
            CreateInvoice("C000123", "FA-2024-0001"),
            CreateInvoice("C000456", "FA-2024-0002"),
        };

        // Act
        await repository.AddRangeAsync(invoices);

        // Assert
        var stored = await context.InvoiceEntity.ToListAsync();
        stored.Should().HaveCount(2);
        stored.Select(i => i.InvoiceNumber).Should().BeEquivalentTo("FA-2024-0001", "FA-2024-0002");
    }

    [Fact]
    public async Task GetByInvoiceNumbersAsync_WithMatchingNumbers_ReturnsOnlyMatchingInvoices()
    {
        // Arrange
        using var context = CreateSqliteContext();
        context.InvoiceEntity.AddRange(
            CreateInvoice("C000123", "FA-2024-0001"),
            CreateInvoice("C000123", "FA-2024-0002"),
            CreateInvoice("C000456", "FA-2024-0003"));
        await context.SaveChangesAsync();

        var repository = new InvoiceRepository(context);

        // Act
        var result = await repository.GetByInvoiceNumbersAsync(new[] { "FA-2024-0001", "FA-2024-0003", "FA-2024-9999" });

        // Assert
        result.Should().HaveCount(2);
        result.Select(i => i.InvoiceNumber).Should().BeEquivalentTo("FA-2024-0001", "FA-2024-0003");
    }

    [Fact]
    public async Task GetByInvoiceNumbersAsync_WithMoreNumbersThanOneQueryBatch_ReturnsAllMatches()
    {
        // Arrange
        using var context = CreateSqliteContext();
        context.InvoiceEntity.AddRange(
            CreateInvoice("C000123", "FA-2024-0001"),
            CreateInvoice("C000456", "FA-2024-4999"));
        await context.SaveChangesAsync();

        var repository = new InvoiceRepository(context);
        var numbers = Enumerable.Range(0, 5000).Select(i => $"FA-2024-{i:D4}").ToList();

        // Act
        var result = await repository.GetByInvoiceNumbersAsync(numbers);

        // Assert
        result.Should().HaveCount(2);
        result.Select(i => i.InvoiceNumber).Should().BeEquivalentTo("FA-2024-0001", "FA-2024-4999");
    }

    [Fact]
    public async Task GetByInvoiceNumbersAsync_WithNoMatch_ReturnsEmptyList()
    {
        // Arrange
        using var context = CreateSqliteContext();
        context.InvoiceEntity.Add(CreateInvoice("C000123", "FA-2024-0001"));
        await context.SaveChangesAsync();

        var repository = new InvoiceRepository(context);

        // Act
        var result = await repository.GetByInvoiceNumbersAsync(new[] { "FA-2024-9999" });

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByStatusAsync_WithMorePendingThanTake_ReturnsPendingUpToTake()
    {
        // Arrange
        using var context = CreateSqliteContext();
        context.InvoiceEntity.AddRange(
            CreateInvoice("C000123", "FA-2024-0001"),
            CreateInvoice("C000123", "FA-2024-0002"),
            CreateInvoice("C000123", "FA-2024-0003"),
            CreateInvoice("C000123", "FA-2024-0004", status: "Processed"));
        await context.SaveChangesAsync();

        var repository = new InvoiceRepository(context);

        // Act
        var result = await repository.GetByStatusAsync("Pending", 2);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(i => i.Status == "Pending");
    }

    [Fact]
    public async Task GetByStatusAsync_WithoutMatchingStatus_ReturnsEmptyList()
    {
        // Arrange
        using var context = CreateSqliteContext();
        context.InvoiceEntity.Add(CreateInvoice("C000123", "FA-2024-0001", status: "Processed"));
        await context.SaveChangesAsync();

        var repository = new InvoiceRepository(context);

        // Act
        var result = await repository.GetByStatusAsync("Pending", 10);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateStatusAsync_WithTargetIds_UpdatesOnlyThoseLines()
    {
        // Arrange
        using var context = CreateSqliteContext();
        var first = CreateInvoice("C000123", "FA-2024-0001");
        var second = CreateInvoice("C000123", "FA-2024-0002");
        var third = CreateInvoice("C000123", "FA-2024-0003");
        context.InvoiceEntity.AddRange(first, second, third);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repository = new InvoiceRepository(context);

        // Act
        await repository.UpdateStatusAsync(new[] { first.InvoiceId, second.InvoiceId }, "Processed");

        // Assert
        var stored = await context.InvoiceEntity.AsNoTracking().ToListAsync();
        stored.Where(i => i.Status == "Processed").Select(i => i.InvoiceId)
            .Should().BeEquivalentTo(new[] { first.InvoiceId, second.InvoiceId });
        stored.Single(i => i.InvoiceId == third.InvoiceId).Status.Should().Be("Pending");
    }

    [Fact]
    public async Task UpdateStatusAsync_WithMoreIdsThanOneQueryBatch_UpdatesAllTargets()
    {
        // Arrange
        using var context = CreateSqliteContext();
        var first = CreateInvoice("C000123", "FA-2024-0001");
        var second = CreateInvoice("C000123", "FA-2024-0002");
        context.InvoiceEntity.AddRange(first, second);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repository = new InvoiceRepository(context);
        var ids = Enumerable.Range(1, 5000).ToList();

        // Act
        await repository.UpdateStatusAsync(ids, "Processed");

        // Assert
        var stored = await context.InvoiceEntity.AsNoTracking().ToListAsync();
        stored.Should().OnlyContain(i => i.Status == "Processed");
    }

    [Fact]
    public async Task GetInsertedByInvoiceAndAccountNumberAsync_WhenTheNumberExistsForSeveralAccounts_ShouldReturnTheRequestedOne()
    {
        // Arrange
        using var context = CreateSqliteContext();
        context.InvoiceEntity.AddRange(
            CreateInvoiceRow("FAC-1", "INSERT", "ACC-1", new DateTime(2026, 1, 1), "/acc1/a.pdf"),
            CreateInvoiceRow("FAC-1", "INSERT", "ACC-2", new DateTime(2026, 3, 1), "/acc2/b.pdf"));
        await context.SaveChangesAsync();

        var repository = new InvoiceRepository(context);

        // Act
        var result = await repository.GetInsertedByInvoiceAndAccountNumberAsync("FAC-1", "ACC-1");

        // Assert
        result.Should().NotBeNull();
        result!.DocumentPath.Should().Be("/acc1/a.pdf");
    }

    [Fact]
    public async Task GetInsertedByInvoiceAndAccountNumberAsync_WhenTheInvoiceBelongsToAnotherAccount_ShouldReturnNull()
    {
        // Arrange
        using var context = CreateSqliteContext();
        context.InvoiceEntity.Add(CreateInvoiceRow("FAC-1", "INSERT", "ACC-1", new DateTime(2026, 1, 1)));
        await context.SaveChangesAsync();

        var repository = new InvoiceRepository(context);

        // Act
        var result = await repository.GetInsertedByInvoiceAndAccountNumberAsync("FAC-1", "ACC-2");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetInsertedByInvoiceAndAccountNumberAsync_WhenOnlyDeleteRow_ShouldReturnNull()
    {
        // Arrange
        using var context = CreateSqliteContext();
        context.InvoiceEntity.Add(CreateInvoiceRow("FAC-2", "DELETE", "ACC-1", new DateTime(2026, 1, 1)));
        await context.SaveChangesAsync();

        var repository = new InvoiceRepository(context);

        // Act
        var result = await repository.GetInsertedByInvoiceAndAccountNumberAsync("FAC-2", "ACC-1");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetInsertedByInvoiceAndAccountNumberAsync_WhenDeleteIsMoreRecent_ShouldStillReturnInsertRow()
    {
        // Arrange
        using var context = CreateSqliteContext();
        context.InvoiceEntity.AddRange(
            CreateInvoiceRow("FAC-3", "INSERT", "ACC-1", new DateTime(2026, 1, 1), "/kept/c.pdf"),
            CreateInvoiceRow("FAC-3", "DELETE", "ACC-1", new DateTime(2026, 6, 1), "/ignored/d.pdf"));
        await context.SaveChangesAsync();

        var repository = new InvoiceRepository(context);

        // Act
        var result = await repository.GetInsertedByInvoiceAndAccountNumberAsync("FAC-3", "ACC-1");

        // Assert
        result.Should().NotBeNull();
        result!.DocumentPath.Should().Be("/kept/c.pdf");
    }

    [Fact]
    public async Task GetInsertedByInvoiceAndAccountNumberAsync_WhenUnknownNumber_ShouldReturnNull()
    {
        // Arrange
        using var context = CreateSqliteContext();
        context.InvoiceEntity.Add(CreateInvoiceRow("FAC-4", "INSERT", "ACC-1", new DateTime(2026, 1, 1)));
        await context.SaveChangesAsync();

        var repository = new InvoiceRepository(context);

        // Act
        var result = await repository.GetInsertedByInvoiceAndAccountNumberAsync("FAC-UNKNOWN", "ACC-1");

        // Assert
        result.Should().BeNull();
    }

    private static InvoiceEntity CreateInvoice(string accountNumber, string invoiceNumber, string status = "Pending")
    {
        return new InvoiceEntity
        {
            AccountNumber = accountNumber,
            InvoiceNumber = invoiceNumber,
            InvoiceDate = new DateTime(2024, 1, 15),
            DocumentPath = "https://docs.pulse.fr/" + invoiceNumber + ".pdf",
            Type = "Facture RYDGE",
            Operation = "INSERT",
            Status = status,
            CreatedOn = DateTime.UtcNow,
        };
    }

    private static InvoiceEntity CreateInvoiceRow(
        string invoiceNumber,
        string operation,
        string accountNumber,
        DateTime createdOn,
        string documentPath = "/docs/invoice.pdf")
    {
        return new InvoiceEntity
        {
            InvoiceNumber = invoiceNumber,
            Operation = operation,
            AccountNumber = accountNumber,
            InvoiceDate = new DateTime(2026, 1, 15),
            DocumentPath = documentPath,
            Type = "Facture RYDGE",
            Status = "Pending",
            CreatedOn = createdOn,
        };
    }
}

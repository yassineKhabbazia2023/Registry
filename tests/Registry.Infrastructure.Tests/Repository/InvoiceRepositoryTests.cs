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

    private static InvoiceEntity CreateInvoice(string accountNumber, string invoiceNumber)
    {
        return new InvoiceEntity
        {
            AccountNumber = accountNumber,
            InvoiceNumber = invoiceNumber,
            InvoiceDate = new DateTime(2024, 1, 15),
            DocumentPath = "https://docs.pulse.fr/" + invoiceNumber + ".pdf",
            Type = "Facture RYDGE",
            Operation = "INSERT",
            Status = "Pending",
            CreatedOn = DateTime.UtcNow,
        };
    }
}

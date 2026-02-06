using Infrastructure.Repository;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Pulse.Registry.Domain.Context;
using Pulse.Registry.Domain.Entities;
using Pulse.Registry.Domain.Entities.Accounts;
using Pulse.Registry.Domain.Entities.Audits;
using Pulse.Registry.Domain.Entities.Contacts;

namespace Registry.Infrastructure.Tests.Repository;

public class TestOfferContext : RefContext
{
    public TestOfferContext(DbContextOptions<RefContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RefOfferEntity>(entity =>
        {
            // Table & PK
            entity.ToTable("Offer", "ref");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            // Columns
            entity.Property(e => e.AccountNumber)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.ClientEmail)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.CollaboratorEmail)
                .HasMaxLength(255);

            entity.Property(e => e.MissionLeaderEmail)
                .HasMaxLength(255);

            entity.Property(e => e.AccountingExpertEmail)
                .HasMaxLength(255);

            entity.Property(e => e.Offer)
                .IsRequired()
                .HasColumnName("Offer")
                .HasMaxLength(255);

            entity.Property(e => e.MigrationStatus)
                .HasMaxLength(50);

            entity.Property(e => e.BatchId)
                .IsRequired()
                .HasColumnType("uniqueidentifier");
        });



        // Ignore unrelated entities to avoid conflicts.
        modelBuilder.Ignore<ContactEntity>();
        modelBuilder.Ignore<DeepValidationEntity>();
        modelBuilder.Ignore<RegOperationEntity>();
        modelBuilder.Ignore<RefContactEntity>();
        modelBuilder.Ignore<RefAccountEntity>();
        modelBuilder.Ignore<RefRoleEntity>();
        modelBuilder.Ignore<AccountEntity>();
        modelBuilder.Ignore<RoleEntity>();
        modelBuilder.Ignore<HubSpotFormEntity>();
    }
}

public class OfferRepositoryTest
{
    private DbContextOptions<RefContext> CreateSqliteInMemoryOptions(string databaseName)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return new DbContextOptionsBuilder<RefContext>()
            .UseSqlite(connection)
            .Options;
    }

    [Fact]
    public async Task SaveOffersAsync_Should_SetSameNonEmptyBatchIdAndInsertOffers()
    {
        // Arrange
        var options = CreateSqliteInMemoryOptions(nameof(SaveOffersAsync_Should_SetSameNonEmptyBatchIdAndInsertOffers));
        using var context = new TestOfferContext(options);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        var offers = new List<RefOfferEntity>
            {
                new RefOfferEntity
                {
                    Id = 1,
                    AccountNumber = "ACC1",
                    ClientEmail = "client1@example.com",
                    CollaboratorEmail = "collab1@example.com",
                    MissionLeaderEmail = "leader1@example.com",
                    AccountingExpertEmail = "expert1@example.com",
                    Offer = "Offer A",
                    MigrationStatus = "NEW"
                },
                new RefOfferEntity
                {
                    Id = 2,
                    AccountNumber = "ACC2",
                    ClientEmail = "client2@example.com",
                    CollaboratorEmail = "collab2@example.com",
                    MissionLeaderEmail = "leader2@example.com",
                    AccountingExpertEmail = "expert2@example.com",
                    Offer = "Offer B",
                    MigrationStatus = "NEW"
                }
            };

        var batchId = Guid.NewGuid();

        var repo = new OfferRepository(context);

        // Act
        await repo.SaveOffersAsync(offers, batchId);

        // Assert
        var inserted = await context.RefOfferEntity.ToListAsync();
        Assert.Equal(2, inserted.Count);

        var insertedBatchId = inserted[0].BatchId;
        Assert.NotEqual(Guid.Empty, insertedBatchId);
        Assert.All(inserted, o => Assert.Equal(insertedBatchId, o.BatchId));

        Assert.Contains(inserted, o =>
            o.AccountNumber == "ACC1" &&
            o.ClientEmail == "client1@example.com" &&
            o.Offer == "Offer A" &&
            o.MigrationStatus == "NEW");
        Assert.Contains(inserted, o =>
            o.AccountNumber == "ACC2" &&
            o.ClientEmail == "client2@example.com" &&
            o.Offer == "Offer B" &&
            o.MigrationStatus == "NEW");
    }
}

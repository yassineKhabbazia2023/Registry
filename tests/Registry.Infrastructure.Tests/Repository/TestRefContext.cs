using Microsoft.EntityFrameworkCore;
using Pulse.Registry.Domain.Context;
using Pulse.Registry.Domain.Entities;
using Pulse.Registry.Domain.Entities.Contacts;
using Pulse.Registry.Domain.Entities.Audits;
using Pulse.Registry.Domain.Entities.Accounts;

namespace Registry.Infrastructure.Tests.Repository;

public class TestRefContext : RefContext
{
    public TestRefContext(DbContextOptions<RefContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure RefContactEntity (maps to ref.Contact)
        modelBuilder.Entity<RefContactEntity>(entity =>
        {
            entity.HasKey(e => e.EntityId);
            entity.ToTable("Contact", "ref");
            entity.Property(e => e.Email)
                  .IsRequired()
                  .HasMaxLength(255);
            entity.Property(e => e.FirstName).HasMaxLength(255);
            entity.Property(e => e.LastName).HasMaxLength(255);
            entity.Property(e => e.JobDescription).HasMaxLength(255);
            entity.Property(e => e.LandPhone).HasMaxLength(255);
            entity.Property(e => e.MobilePhone).HasMaxLength(255);
            entity.Property(e => e.OfficeCode).HasMaxLength(50);
            entity.Property(e => e.OperationType)
                  .IsRequired()
                  .HasMaxLength(20);
        });

        // Configure ContactEntity (maps to Contacts in schema "Contact")
        modelBuilder.Entity<ContactEntity>(entity =>
        {
            entity.ToTable("Contacts", "Contact");
            entity.HasKey(e => e.ContactId).HasName("C_Contact_PK");
            entity.Property(e => e.ContactId).IsRequired();
            entity.Property(e => e.ContactGlobalUniqueId).HasColumnType("UNIQUEIDENTIFIER");
            entity.Property(e => e.Email)
                  .IsRequired()
                  .HasMaxLength(250)
                  .HasColumnType("VARCHAR");
            entity.Property(e => e.Type)
                  .IsRequired()
                  .HasMaxLength(20)
                  .HasColumnType("VARCHAR");
        });

        // Configure DeepValidationEntity (if needed by the repository)
        modelBuilder.Entity<DeepValidationEntity>(entity =>
        {
            entity.ToTable("DeepValidations", "Audit");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
        });

        // Configure RegOperationEntity (used in deep validations)
        modelBuilder.Entity<RegOperationEntity>(entity =>
        {
            entity.ToTable("Operations", "reg");
            entity.Property(e => e.LastStatusApprovalBy)
                  .HasMaxLength(50)
                  .IsUnicode(false);
            entity.Property(e => e.Operation)
                  .IsRequired()
                  .HasMaxLength(10)
                  .IsUnicode(false);
            entity.Property(e => e.ApprovalStatus).HasMaxLength(20);
            entity.Property(e => e.Type)
                  .HasMaxLength(10)
                  .IsUnicode(false);
            entity.Property(e => e.CreatedBySystem).HasDefaultValue(false);
        });

        modelBuilder.Entity<RefRoleEntity>(entity =>
        {
            entity.HasKey(e => e.EntityId);

            entity.ToTable("Role", "ref");

            entity.Property(e => e.AccountNumber)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.ContactEmail)
                .IsRequired()
                .HasMaxLength(255);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.OperationType)
                .IsRequired()
                .HasMaxLength(20);
            entity.Property(e => e.RoleSource)
            .HasDefaultValue(null);
        });

        // Ignore unrelated entities to avoid conflicts.
        modelBuilder.Ignore<RefAccountEntity>();
        modelBuilder.Ignore<AccountEntity>();
        modelBuilder.Ignore<RoleEntity>();
    }
}

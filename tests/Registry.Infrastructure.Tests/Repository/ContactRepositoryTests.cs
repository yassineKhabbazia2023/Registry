using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Consts;
using Application.Interfaces;
using Application.Models;
using Application.Models.Contacts;
using Application.Requests;
using Domain.Entities.Contacts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Pulse.Registry.Domain.Context;
using Pulse.Registry.Domain.Entities;
using Registry.Application.Consts;
using Application.Mappers;
using Xunit;
using Application.Repository;
using Domain.Entities.Accounts;
using Domain.Entities.Audits;
using Domain.Entities;

namespace Registry.Infrastructure.Tests.Repository
{
    public class TestContactContext : RefContext
    {
        public TestContactContext(DbContextOptions<RefContext> options) : base(options) { }

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

            // Ignore unrelated entities to avoid conflicts.
            modelBuilder.Ignore<RefAccountEntity>();
            modelBuilder.Ignore<RefRoleEntity>();
            modelBuilder.Ignore<AccountEntity>();
            modelBuilder.Ignore<RoleEntity>();
            modelBuilder.Ignore<ArchivedRefAccount>();
            modelBuilder.Ignore<ArchivedRefContact>();
            modelBuilder.Ignore<ArchivedRefRole>();
            modelBuilder.Ignore<ArchivedDeepValidation>();
            modelBuilder.Ignore<ArchivedRegOperation>();
        }
    }

    public class ContactRepositoryTests
    {
        private DbContextOptions<RefContext> CreateInMemoryOptions(string databaseName)
        {
            return new DbContextOptionsBuilder<RefContext>()
                .UseInMemoryDatabase($"{databaseName}_{Guid.NewGuid()}")
                .Options;
        }

        private DbContextOptions<RefContext> CreateSqliteInMemoryOptions(string databaseName)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            return new DbContextOptionsBuilder<RefContext>()
                .UseSqlite(connection)
                .Options;
        }

        private IContactRepository CreateRepository(RefContext context, IDeepValidationRepository deepValidationRepository)
        {
            var logger = Mock.Of<ILogger<ContactRepository>>();
            return new ContactRepository(context, logger, deepValidationRepository);
        }

        #region BulkAddContactsAsync Tests

        [Fact]
        public async Task BulkAddContactsAsync_Should_BulkInsert_Contacts()
        {
            // Arrange - using SQLite in-memory via TestContactContext.
            var options = CreateSqliteInMemoryOptions(nameof(BulkAddContactsAsync_Should_BulkInsert_Contacts));
            using var context = new TestContactContext(options);
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            // RefContactCsv maps to RefContactEntity (ref.Contact table)
            var contactsCsv = new List<RefContactCsv>
            {
                new RefContactCsv { Email = "a@test.com", FirstName = "A", LastName = "Test", Operation = OperationAction.Insert },
                new RefContactCsv { Email = "b@test.com", FirstName = "B", LastName = "Test", Operation = OperationAction.Insert }
            };
            var repository = CreateRepository(context, Mock.Of<IDeepValidationRepository>());

            // Act
            await repository.BulkAddContactsAsync(contactsCsv);
            // Query the ref.Contact table (RefContact DbSet)
            var result = await context.RefContact.ToListAsync();

            // Assert
            Assert.Equal(contactsCsv.Count, result.Count);
        }

        #endregion

        #region AddContactAsync Tests

        [Fact]
        public async Task AddContactAsync_ShouldReturnFalse_IfContactAlreadyExists()
        {
            // Arrange - Add contact to Contacts table (ContactEntities).
            var options = CreateInMemoryOptions(nameof(AddContactAsync_ShouldReturnFalse_IfContactAlreadyExists));
            using var context = new RefContext(options);
            var deepValidationRepo = Mock.Of<IDeepValidationRepository>();
            var repository = CreateRepository(context, deepValidationRepo);
            var contactEntity = new ContactEntity
            {
                ContactId = 1,
                Email = "existing@test.com",
                FirstName = "Exist",
                LastName = "User",
                Type = "Customer"
            };
            context.ContactEntities.Add(contactEntity);
            await context.SaveChangesAsync();
            var contactModel = contactEntity.MapContactEntityToModel();

            // Act
            var result = await repository.AddContactAsync(contactModel);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task AddContactAsync_ShouldReturnTrue_IfContactDoesNotExist()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(AddContactAsync_ShouldReturnTrue_IfContactDoesNotExist));
            using var context = new RefContext(options);
            var deepValidationRepo = Mock.Of<IDeepValidationRepository>();
            var repository = CreateRepository(context, deepValidationRepo);
            var contactModel = new Contact
            {
                ContactId = 100,
                Email = "new@test.com",
                FirstName = "New",
                LastName = "User",
                Type = "Customer"
            };

            // Act
            var result = await repository.AddContactAsync(contactModel);

            // Assert
            Assert.True(result);
            var entity = await context.ContactEntities.FirstOrDefaultAsync(c => c.ContactId == contactModel.ContactId);
            Assert.NotNull(entity);
        }

        #endregion

        #region UpdateContactAsync Tests

        [Fact]
        public async Task UpdateContactAsync_ShouldReturnFalse_IfContactDoesNotExist()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(UpdateContactAsync_ShouldReturnFalse_IfContactDoesNotExist));
            using var context = new RefContext(options);
            var repository = CreateRepository(context, Mock.Of<IDeepValidationRepository>());
            var contactModel = new Contact
            {
                ContactId = 200,
                Email = "nonexistent@test.com",
                FirstName = "No",
                LastName = "Exist"
            };

            // Act
            var result = await repository.UpdateContactAsync(contactModel);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateContactAsync_ShouldReturnTrue_IfContactExists()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(UpdateContactAsync_ShouldReturnTrue_IfContactExists));
            using var context = new RefContext(options);
            var repository = CreateRepository(context, Mock.Of<IDeepValidationRepository>());
            var contactEntity = new ContactEntity
            {
                ContactId = 300,
                Email = "update@test.com",
                FirstName = "Update",
                LastName = "User",
                ContactGlobalUniqueId = Guid.NewGuid(),
                Type = "Customer"
            };
            context.ContactEntities.Add(contactEntity);
            await context.SaveChangesAsync();
            var contactModel = contactEntity.MapContactEntityToModel();
            var newGuid = Guid.NewGuid();
            contactModel.ContactGlobalUniqueId = newGuid;

            // Act
            var result = await repository.UpdateContactAsync(contactModel);

            // Assert
            Assert.True(result);
            var updatedEntity = await context.ContactEntities.FirstOrDefaultAsync(c => c.ContactId == contactEntity.ContactId);
            Assert.NotNull(updatedEntity);
            Assert.Equal(newGuid, updatedEntity.ContactGlobalUniqueId);
        }

        #endregion

        #region  DeleteContactByIdAsync Tests

        [Fact]
        public async Task DeleteContactByIdAsync_ShouldReturnFalse_IfContactIdIsInvalid()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(DeleteContactByIdAsync_ShouldReturnFalse_IfContactIdIsInvalid));
            using var context = new RefContext(options);
            var repository = CreateRepository(context, Mock.Of<IDeepValidationRepository>());

            // Act
            var result = await repository.DeleteContactByIdAsync(0);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteContactByIdAsync_ShouldReturnFalse_IfContactDoesNotExist()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(DeleteContactByIdAsync_ShouldReturnFalse_IfContactDoesNotExist));
            using var context = new RefContext(options);
            var repository = CreateRepository(context, Mock.Of<IDeepValidationRepository>());

            // Act
            var result = await repository.DeleteContactByIdAsync(9999);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteContactByIdAsync_ShouldReturnTrue_IfContactExists()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(DeleteContactByIdAsync_ShouldReturnTrue_IfContactExists));
            using var context = new RefContext(options);
            var repository = CreateRepository(context, Mock.Of<IDeepValidationRepository>());
            var contactEntity = new ContactEntity
            {
                ContactId = 400,
                Email = "delete@test.com",
                FirstName = "Delete",
                LastName = "User",
                Type = "Customer"
            };
            context.ContactEntities.Add(contactEntity);
            // Also add a related RoleEntity.
            var roleEntity = new RoleEntity
            {
                ContactId = 400,
                AccountId = 1,
                AccountGlobalUniqueId = Guid.NewGuid(),
                AccountNumber = "ACC001",
                RoleDuplicatesCounter = 1
            };
            context.RoleEntities.Add(roleEntity);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.DeleteContactByIdAsync(400);

            // Assert
            Assert.True(result);
            var deletedContact = await context.ContactEntities.FirstOrDefaultAsync(c => c.ContactId == 400);
            Assert.Null(deletedContact);
            var relatedRoles = await context.RoleEntities.Where(r => r.ContactId == 400).ToListAsync();
            Assert.Empty(relatedRoles);
        }

        #endregion

        #region GetContactByEmailOrIdAsync Tests

        [Fact]
        public async Task GetContactByEmailOrIdAsync_ShouldReturnContact_IfEmailExists()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(GetContactByEmailOrIdAsync_ShouldReturnContact_IfEmailExists));
            using var context = new RefContext(options);
            var contactEntity = new ContactEntity
            {
                ContactId = 600,
                Email = "contact@test.com",
                FirstName = "Contact",
                LastName = "Tester",
                Type = "Customer"
            };
            context.ContactEntities.Add(contactEntity);
            await context.SaveChangesAsync();
            var repository = CreateRepository(context, Mock.Of<IDeepValidationRepository>());

            // Act
            var result = await repository.GetContactByEmailOrIdAsync(email: "contact@test.com");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("contact@test.com", result.Email);
        }

        [Fact]
        public async Task GetContactByEmailOrIdAsync_ShouldReturnContact_IfContactIdExists()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(GetContactByEmailOrIdAsync_ShouldReturnContact_IfContactIdExists));
            using var context = new RefContext(options);
            var contactEntity = new ContactEntity
            {
                ContactId = 700,
                Email = "contact2@test.com",
                FirstName = "Contact2",
                LastName = "Tester2",
                Type = "Customer"
            };
            context.ContactEntities.Add(contactEntity);
            await context.SaveChangesAsync();
            var repository = CreateRepository(context, Mock.Of<IDeepValidationRepository>());

            // Act
            var result = await repository.GetContactByEmailOrIdAsync(contactId: 700);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("contact2@test.com", result.Email);
        }

        [Fact]
        public async Task GetContactByEmailOrIdAsync_ShouldReturnNull_IfContactDoesNotExist()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(GetContactByEmailOrIdAsync_ShouldReturnNull_IfContactDoesNotExist));
            using var context = new RefContext(options);
            var repository = CreateRepository(context, Mock.Of<IDeepValidationRepository>());

            // Act
            var result = await repository.GetContactByEmailOrIdAsync(contactId: 800);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region DoesContactExistByEmailOrIdAsync / DoesContactExistByEmailAsync Tests

        [Fact]
        public async Task DoesContactExistByEmailOrIdAsync_ShouldReturnTrue_IfEmailExists()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(DoesContactExistByEmailOrIdAsync_ShouldReturnTrue_IfEmailExists));
            using var context = new RefContext(options);
            var contactEntity = new ContactEntity
            {
                ContactId = 900,
                Email = "alice.smith@example.com",
                FirstName = "Alice",
                LastName = "Smith",
                Type = "Customer"
            };
            context.ContactEntities.Add(contactEntity);
            await context.SaveChangesAsync();
            var repository = CreateRepository(context, Mock.Of<IDeepValidationRepository>());

            // Act
            var result = await repository.DoesContactExistByEmailOrIdAsync("alice.smith@example.com");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task DoesContactExistByEmailAsync_ShouldReturnTrue_IfEmailExists()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(DoesContactExistByEmailAsync_ShouldReturnTrue_IfEmailExists));
            using var context = new RefContext(options);
            var contactEntity = new ContactEntity
            {
                ContactId = 901,
                Email = "charlie@test.com",
                FirstName = "Charlie",
                LastName = "Test",
                Type = "Customer"
            };
            context.ContactEntities.Add(contactEntity);
            await context.SaveChangesAsync();
            var repository = CreateRepository(context, Mock.Of<IDeepValidationRepository>());

            // Act
            var result = await repository.DoesContactExistByEmailAsync("charlie@test.com");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task DoesContactExistByEmailAsync_ShouldReturnFalse_IfEmailDoesNotExist()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(DoesContactExistByEmailAsync_ShouldReturnFalse_IfEmailDoesNotExist));
            using var context = new RefContext(options);
            var repository = CreateRepository(context, Mock.Of<IDeepValidationRepository>());

            // Act
            var result = await repository.DoesContactExistByEmailAsync("nonexistent@test.com");

            // Assert
            Assert.False(result);
        }

        #endregion

        #region GetRefContactByEmailAsync Tests

        [Fact]
        public async Task GetRefContactByEmailAsync_ShouldReturnRefContact_IfEmailMatchesAndIsCustomer()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(GetRefContactByEmailAsync_ShouldReturnRefContact_IfEmailMatchesAndIsCustomer));
            using var context = new RefContext(options);
            var refContact = new RefContactEntity
            {
                EntityId = Guid.NewGuid(),
                Email = "refcontact@test.com",
                FirstName = "Ref",
                LastName = "Contact",
                IsCustomer = true,
                OperationDate = DateTime.UtcNow,
                OperationType = OperationAction.Insert

            };
            context.RefContactEntity.Add(refContact);
            await context.SaveChangesAsync();
            var repository = CreateRepository(context, Mock.Of<IDeepValidationRepository>());

            // Act
            var result = await repository.GetRefContactByEmailAsync("refcontact@test.com");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("refcontact@test.com", result.Email);
        }

        #endregion

        #region GetContactsWithoutOperationsPagedAsync Tests

        [Fact]
        public async Task GetContactsWithoutOperationsPagedAsync_ShouldReturnAllRecords_WhenNoLastEntityIdProvided()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(GetContactsWithoutOperationsPagedAsync_ShouldReturnAllRecords_WhenNoLastEntityIdProvided));
            using var context = new RefContext(options);
            var contact1 = new RefContactEntity { Email= "cnt1@email.com" ,OperationType = OperationAction.Insert, EntityId = Guid.NewGuid(), OperationDate = DateTime.UtcNow.AddMinutes(1), ValidationDate = null };
            var contact2 = new RefContactEntity {Email= "cnt1@email.com" , OperationType = OperationAction.Insert, EntityId = Guid.NewGuid(), OperationDate = DateTime.UtcNow.AddMinutes(2), ValidationDate = null };
            var contact3 = new RefContactEntity { Email = "cnt1@email.com",OperationType = OperationAction.Insert, EntityId = Guid.NewGuid(), OperationDate = DateTime.UtcNow.AddMinutes(3), ValidationDate = null };
            context.RefContactEntity.AddRange(contact1, contact2, contact3);
            await context.SaveChangesAsync();
            var repository = CreateRepository(context, Mock.Of<IDeepValidationRepository>());

            // Act
            var results = await repository.GetContactsWithoutOperationsPagedAsync(10);

            // Assert
            Assert.Equal(3, results.Count);
            Assert.True(results.SequenceEqual(results.OrderBy(x => x.OperationDate)));
        }

        [Fact]
        public async Task GetContactsWithoutOperationsPagedAsync_ShouldReturnRecords_GreaterThanLastEntityId()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(GetContactsWithoutOperationsPagedAsync_ShouldReturnRecords_GreaterThanLastEntityId));
            using var context = new RefContext(options);
            var guid1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var guid2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            var guid3 = Guid.Parse("00000000-0000-0000-0000-000000000003");
            var guid4 = Guid.Parse("00000000-0000-0000-0000-000000000004");
            var contact1 = new RefContactEntity {Email = "cnt1@email.fr", OperationType = OperationAction.Insert, EntityId = guid1, OperationDate = DateTime.UtcNow.AddMinutes(1), ValidationDate = null };
            var contact2 = new RefContactEntity {Email = "cnt2@email.fr", OperationType = OperationAction.Insert, EntityId = guid2, OperationDate = DateTime.UtcNow.AddMinutes(2), ValidationDate = null };
            var contact3 = new RefContactEntity {Email = "cnt3@email.fr", OperationType = OperationAction.Insert, EntityId = guid3, OperationDate = DateTime.UtcNow.AddMinutes(3), ValidationDate = null };
            var contact4 = new RefContactEntity {Email = "cnt4@email.fr", OperationType = OperationAction.Insert, EntityId = guid4, OperationDate = DateTime.UtcNow.AddMinutes(4), ValidationDate = null };
            context.RefContactEntity.AddRange(contact1, contact2, contact3, contact4);
            await context.SaveChangesAsync();
            var repository = CreateRepository(context, Mock.Of<IDeepValidationRepository>());

            // Act
            var results = await repository.GetContactsWithoutOperationsPagedAsync(10, guid2);

            // Assert
            Assert.Equal(2, results.Count);
            Assert.All(results, x => Assert.True(x.EntityId.CompareTo(guid2) > 0));
        }

        #endregion
    }
}

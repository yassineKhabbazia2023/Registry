// <copyright file="RoleRepositoryTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using AutoFixture;
using Microsoft.EntityFrameworkCore;
using Pulse.Registry.Domain.Context;
using FluentAssertions;
using Infrastructure.Repository;
using Pulse.Registry.Domain.Entities;
using Registry.Application.Consts;
using Application.Consts;
using Microsoft.Data.Sqlite;
using Application.Models;
using Pulse.Registry.Domain.Entities.Accounts;
using Pulse.Registry.Domain.Entities.Contacts;
using Pulse.Registry.Domain.Entities.Audits;

namespace Registry.Infrastructure.Tests.Repository;

public class RoleRepositoryTests
{
    private readonly Fixture _fixture;

    public RoleRepositoryTests()
    {
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList().ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    private DbContextOptions<RefContext> GetDbOptions()
    {
        return new DbContextOptionsBuilder<RefContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
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

    [Fact]
    public async Task AddRoleAsync_ShouldNotAddRole_WhenRoleAlreadyExists()
    {
        // Arrange
        var roleEntity = _fixture.Create<RoleEntity>();

        using (var context = new RefContext(GetDbOptions()))
        {
            context.RoleEntity.Add(roleEntity);
            await context.SaveChangesAsync();

            var repository = new RoleRepository(context);
            var initialCount = context.RoleEntity.Count();

            // Act
            await repository.AddRoleAsync(roleEntity);

            // Assert
            context.RoleEntity.Count().Should().Be(initialCount);
        }
    }

    [Fact]
    public async Task AddRoleAsync_ShouldAddRole_WhenRoleDoesNotExist()
    {
        // Arrange
        var roleEntity = _fixture.Create<RoleEntity>();

        using (var context = new RefContext(GetDbOptions()))
        {
            var repository = new RoleRepository(context);
            var initialCount = context.RoleEntity.Count();

            // Act
            await repository.AddRoleAsync(roleEntity);

            // Assert
            context.RoleEntity.Count().Should().Be(initialCount + 1);
            var savedRole = await context.RoleEntity.FirstOrDefaultAsync(r =>
                r.ContactId == roleEntity.ContactId && r.AccountId == roleEntity.AccountId);
            savedRole.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task DeleteRoleAsync_ShouldRemoveRole_WhenRoleExists()
    {
        // Arrange
        var roleEntity = _fixture.Create<RoleEntity>();

        using (var context = new RefContext(GetDbOptions()))
        {
            context.RoleEntity.Add(roleEntity);
            await context.SaveChangesAsync();

            var repository = new RoleRepository(context);
            var initialCount = context.RoleEntity.Count();

            // Act
            await repository.DeleteRoleAsync(roleEntity);

            // Assert
            context.RoleEntity.Count().Should().Be(initialCount - 1);
            var deletedRole = await context.RoleEntity.FirstOrDefaultAsync(r =>
                r.ContactId == roleEntity.ContactId && r.AccountId == roleEntity.AccountId);
            deletedRole.Should().BeNull();
        }
    }

    [Fact]
    public void GetUnprocessedRoles_ShouldReturnOnlyUnprocessedRoles()
    {
        // Arrange
        var unprocessedRole = _fixture.Create<RefRoleEntity>();
        unprocessedRole.ValidationDate = null;
        var processedRole = _fixture.Create<RefRoleEntity>();
        var operation = new RegOperationEntity
        {
            EntityId = processedRole.EntityId,
            Type = "ROLE",
            ApprovalStatus = ApprovalStatus.Approved,
            Operation = "INSERT"
        };

        using (var context = new RefContext(GetDbOptions()))
        {
            context.RefRoleEntity.AddRange(unprocessedRole, processedRole);
            context.RegOperationEntity.Add(operation);
            context.SaveChanges();

            var repository = new RoleRepository(context);

            // Act
            var result = repository.GetUnprocessedRoles();

            // Assert
            result.Should().ContainSingle();
            result.First().EntityId.Should().Be(unprocessedRole.EntityId);
        }
    }

    [Fact]
    public async Task GetRefRoleAsync_ShouldReturnRole_WhenRoleExists()
    {
        // Arrange
        var refRoleEntity = _fixture.Create<RefRoleEntity>();

        using (var context = new RefContext(GetDbOptions()))
        {
            context.RefRoleEntity.Add(refRoleEntity);
            await context.SaveChangesAsync();

            var repository = new RoleRepository(context);

            // Act
            var result = await repository.GetRefRoleAsync(
                refRoleEntity.AccountNumber,
                refRoleEntity.ContactEmail);

            // Assert
            result.Should().NotBeNull();
            result.AccountNumber.Should().Be(refRoleEntity.AccountNumber);
            result.ContactEmail.Should().Be(refRoleEntity.ContactEmail);
        }
    }

    [Fact]
    public async Task GetRefRoleAsync_ShouldReturnNull_WhenRoleDoesNotExist()
    {
        using (var context = new RefContext(GetDbOptions()))
        {
            var repository = new RoleRepository(context);

            // Act
            var result = await repository.GetRefRoleAsync("nonexistent", "email@test.com");

            // Assert
            result.Should().BeNull();
        }
    }

    [Theory]
    [InlineData(OperationAction.Insert, true)]
    [InlineData(OperationAction.Update, false)]
    [InlineData(OperationAction.Delete, false)]
    public async Task HasInsertRefRoleAsync_ShouldOnlyMatchEquivalentInsertRole(
        string operationType,
        bool expected)
    {
        var refRole = _fixture.Build<RefRoleEntity>()
            .With(role => role.OperationType, operationType)
            .Create();
        using var context = new RefContext(GetDbOptions());
        context.RefRoleEntity.Add(refRole);
        await context.SaveChangesAsync();

        var result = await new RoleRepository(context).HasInsertRefRoleAsync(
            refRole.AccountNumber,
            refRole.ContactEmail);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task AddRefRoleAsync_ShouldAddRole_AndReturnTrue()
    {
        // Arrange
        var refRoleEntity = _fixture.Create<RefRoleEntity>();

        using (var context = new RefContext(GetDbOptions()))
        {
            var repository = new RoleRepository(context);
            var initialCount = context.RefRoleEntity.Count();

            // Act
            var result = await repository.AddRefRoleAsync(refRoleEntity);

            // Assert
            result.Should().BeTrue();
            context.RefRoleEntity.Count().Should().Be(initialCount + 1);
            var savedRole = await context.RefRoleEntity.FirstOrDefaultAsync(r =>
                r.EntityId == refRoleEntity.EntityId);
            savedRole.Should().NotBeNull();
        }
    }

    [Fact]
    public void IsRoleExistedInPulse_ShouldReturnTrue_WhenRoleExists()
    {
        // Arrange
        var accountEntity = _fixture.Create<AccountEntity>();
        var contactEntity = _fixture.Create<ContactEntity>();
        var roleEntity = _fixture.Build<RoleEntity>()
            .With(r => r.AccountId, accountEntity.AccountId)
            .With(r => r.ContactId, contactEntity.ContactId)
            .Without(r => r.Account)
            .Without(r => r.Contact)
            .Create();

        using (var context = new RefContext(GetDbOptions()))
        {
            context.AccountEntity.Add(accountEntity);
            context.ContactEntity.Add(contactEntity);
            context.RoleEntity.Add(roleEntity);
            context.SaveChanges();

            var repository = new RoleRepository(context);

            // Act
            var result = repository.DoesRoleExistInPulse(
                accountEntity.AccountNumber,
                contactEntity.Email);

            // Assert
            result.Should().BeTrue();
        }
    }

    [Fact]
    public void IsRoleExistedInPulse_ShouldReturnFalse_WhenRoleDoesNotExist()
    {
        using (var context = new RefContext(GetDbOptions()))
        {
            var repository = new RoleRepository(context);

            // Act
            var result = repository.DoesRoleExistInPulse("nonexistent", "email@test.com");

            // Assert
            result.Should().BeFalse();
        }
    }

    [Fact]
    public async void GetContactPulseRolesAsync_Should_Return_Roles_By_Email()
    {
        // Arrange 
        var accounts = _fixture.CreateMany<AccountEntity>(2);

        var contactEntity = _fixture.Build<ContactEntity>()
            .Without(c => c.RoleEntity)
            .Create();
        var contactEntity2 = _fixture.Build<ContactEntity>()
            .Without(c => c.RoleEntity)
            .Create();

        var roles = new List<RoleEntity>()
        {
            new RoleEntity()
            {
                AccountId = accounts.First().AccountId,
                ContactId = contactEntity.ContactId,
                RoleDuplicatesCounter = 5
            },
            new RoleEntity()
            {
                AccountId = accounts.Last().AccountId,
                ContactId = contactEntity.ContactId,
                RoleDuplicatesCounter = 0
            },
            new RoleEntity()
            {
                AccountId = accounts.First().AccountId,
                ContactId = contactEntity2.ContactId,
                RoleDuplicatesCounter = 0
            }
        };

        using (var context = new RefContext(GetDbOptions()))
        {
            context.AccountEntity.AddRange(accounts);
            context.Add(contactEntity);
            context.AddRange(roles);


            await context.SaveChangesAsync();

            var repository = new RoleRepository(context);

            // Act
            var result = await repository.GetRolesForContactAsync(contactEntity.Email);

            // Assert
            result.Count().Should().Be(2);
        }
    }

    [Fact]
    public async void GetDeepValidationFailedRoles_should_Returns_RefRoles()
    {
        // Arrange
        var account = _fixture.Create<RefAccountEntity>();
        var account2 = _fixture.Create<RefAccountEntity>();
        var contact = _fixture.Create<RefContactEntity>();
        var roleEntityId = new Guid("4de300a7-b444-4912-aaa0-af3a5dd364ab");

        var refRoles = new List<RefRoleEntity>() {
            new RefRoleEntity() {
                AccountNumber = account.AccountNumber,
                ContactEmail = contact.Email,
                EntityId = roleEntityId,
                OperationType = OperationAction.Insert,
                },
            new RefRoleEntity() {
                AccountNumber = account2.AccountNumber,
                ContactEmail = contact.Email,
                EntityId = new Guid(),
                OperationType = OperationAction.Insert,
            }
        };

        var deepValidationEntity = new DeepValidationEntity()
        {
            EntityId = roleEntityId,
            Type = "role",
            Id = 1003,
            Reason = "dead"
        };

        var deepValidationEnties = new List<DeepValidationEntity>()
        { deepValidationEntity,new DeepValidationEntity()
        {
           EntityId = roleEntityId,
            Type = "Account",
            Id = 1,
            Reason = "dead"
        }
        };

        using (var context = new RefContext(GetDbOptions()))
        {
            context.RefAccountEntity.AddRange(new List<RefAccountEntity>() { account, account2 });
            context.RefContactEntity.Add(contact);
            context.RefRoleEntity.AddRange(refRoles);
            context.DeepValidationEntity.AddRange(deepValidationEnties);

            await context.SaveChangesAsync();

            var repository = new RoleRepository(context);
            var result = await repository.GetDeepValidationFailedRoles();

            result.Count().Should().Be(1);
        }
    }

    [Fact]
    public async Task DoesRoleExistInPulse_ByIds_ShouldReturnTrue_WhenRoleExists()
    {
        // Arrange
        using var context = new RefContext(GetDbOptions());
        var role = new RoleEntity { AccountId = 1, ContactId = 2 };
        context.RoleEntity.Add(role);
        await context.SaveChangesAsync();
        var repository = new RoleRepository(context);

        // Act
        var exists = await repository.DoesRoleExistInPulse(1, 2);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task DoesRoleExistInPulse_ByIds_ShouldReturnFalse_WhenRoleDoesNotExist()
    {
        // Arrange
        using var context = new RefContext(GetDbOptions());
        var repository = new RoleRepository(context);

        // Act
        var exists = await repository.DoesRoleExistInPulse(99, 100);

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task GetPulseRole_ShouldReturnRole_WhenExists()
    {
        // Arrange
        using var context = new RefContext(GetDbOptions());
        var account = new AccountEntity
        {
            AccountId = 5,
            AccountNumber = "ACC5",
            LegalName = "Test Account 5",
            AccountGlobalUniqueId = Guid.NewGuid()
        };
        var contact = new ContactEntity
        {
            ContactId = 7,
            Email = "test@example.com",
            ContactGlobalUniqueId = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "User",
            Type = "customer"
        };
        var domainRole = new RoleEntity
        {
            AccountId = account.AccountId,
            ContactId = contact.ContactId,
            AccountGlobalUniqueId = account.AccountGlobalUniqueId.Value,
            AccountNumber = account.AccountNumber,
            ContactEmail = contact.Email,
            ContactGlobalUniqueId = contact.ContactGlobalUniqueId.Value,
            RoleDuplicatesCounter = 3
        };
        context.AccountEntity.Add(account);
        context.ContactEntity.Add(contact);
        context.RoleEntity.Add(domainRole);
        await context.SaveChangesAsync();
        var repository = new RoleRepository(context);

        // Act
        var result = await repository.GetPulseRole(contact.Email, account.AccountNumber);

        // Assert
        result.Should().NotBeNull();
        result!.AccountId.Should().Be(account.AccountId);
        result.ContactId.Should().Be(contact.ContactId);
        result.RoleDuplicatesCounter.Should().Be(3);
    }

    [Fact]
    public async Task GetPulseRole_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        using var context = new RefContext(GetDbOptions());
        var repository = new RoleRepository(context);

        // Act
        var result = await repository.GetPulseRole("no@one.com", "NOACC");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdatePulseRole_ShouldReturnFalse_WhenRoleDoesNotExist()
    {
        // Arrange
        using var context = new RefContext(GetDbOptions());
        var repository = new RoleRepository(context);
        var updatedRole = new RoleEntity { AccountId = 9, ContactId = 9 };

        // Act
        var result = await repository.UpdatePulseRole(updatedRole);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdatePulseRole_ShouldUpdateAndReturnTrue_WhenRoleExists()
    {
        // Arrange
        using var context = new RefContext(GetDbOptions());
        var existingRole = new RoleEntity { AccountId = 3, ContactId = 4, RoleDuplicatesCounter = 1 };
        context.RoleEntity.Add(existingRole);
        await context.SaveChangesAsync();

        var repository = new RoleRepository(context);

        existingRole.RoleDuplicatesCounter = 99;

        // Act
        var result = await repository.UpdatePulseRole(existingRole);

        // Assert
        result.Should().BeTrue();
        var fetched = await context.RoleEntity
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.AccountId == 3 && r.ContactId == 4);
        fetched!.RoleDuplicatesCounter.Should().Be(99);
    }

    [Fact]
    public async Task UpdateRefRoleAsync_ShouldPersistChanges()
    {
        // Arrange
        using var context = new RefContext(GetDbOptions());
        var refRole = new RefRoleEntity
        {
            EntityId = Guid.NewGuid(),
            AccountNumber = "A1",
            ContactEmail = "c@x.com",
            RoleSource = "SRC",
            OperationType = OperationAction.Insert,
        };
        context.RefRoleEntity.Add(refRole);
        await context.SaveChangesAsync();

        var repository = new RoleRepository(context);
        refRole.RoleSource = "NEW";

        // Act
        await repository.UpdateRefRoleAsync(refRole);

        // Assert
        var fetched = await context.RefRoleEntity
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.EntityId == refRole.EntityId);
        fetched!.RoleSource.Should().Be("NEW");
    }

    [Fact]
    public async Task AddRolesAsync_ShouldBulkInsertEntities_WhenSourceIsNullOrWhitespace()
    {
        // Arrange
        var roles = _fixture.CreateMany<RefRoleCsv>(3).ToList();
        var options = CreateSqliteInMemoryOptions(nameof(AddRolesAsync_ShouldBulkInsertEntities_WhenSourceIsNullOrWhitespace));

        using var context = new TestRefContext(options);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        var repository = new RoleRepository(context);

        // Act
        await repository.AddRolesAsync(roles, source: null);

        // Assert
        var inserted = await context.RefRoleEntity.ToListAsync();
        inserted.Count.Should().Be(3);
        inserted.All(r => string.IsNullOrWhiteSpace(r.RoleSource)).Should().BeTrue();
    }

    [Fact]
    public async Task AddRolesAsync_ShouldSetRoleSourceAndBulkInsert_WhenSourceProvided()
    {
        // Arrange
        var options = CreateSqliteInMemoryOptions(nameof(AddRolesAsync_ShouldSetRoleSourceAndBulkInsert_WhenSourceProvided));
        var roles = _fixture.CreateMany<RefRoleCsv>(4).ToList();
        const string source = "PENNYLANE";

        using var context = new TestRefContext(options);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        var repository = new RoleRepository(context);

        // Act
        await repository.AddRolesAsync(roles, source);

        // Assert
        var inserted = await context.RefRoleEntity.ToListAsync();
        inserted.Count.Should().Be(4);
        inserted.All(r => r.RoleSource == source).Should().BeTrue();
    }
}

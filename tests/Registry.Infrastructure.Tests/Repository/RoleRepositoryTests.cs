using Application.Models;
using AutoFixture;
using Microsoft.EntityFrameworkCore;
using Pulse.ContactRegistry.Domain.Context;
using Moq;
using Microsoft.Extensions.Logging;
using FluentAssertions;
using Domain.Entities.Accounts;
using Infrastructure.Repository;
using Pulse.ContactRegistry.Domain.Entities;
using Domain.Entities.Contacts;
using Registry.Application.Consts;

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

    [Fact]
    public async Task AddRoleAsync_ShouldNotAddRole_WhenRoleAlreadyExists()
    {
        // Arrange
        var roleEntity = _fixture.Create<RoleEntity>();

        using (var context = new RefContext(GetDbOptions()))
        {
            context.RoleEntities.Add(roleEntity);
            await context.SaveChangesAsync();

            var repository = new RoleRepository(context);
            var initialCount = context.RoleEntities.Count();

            // Act
            await repository.AddRoleAsync(roleEntity);

            // Assert
            context.RoleEntities.Count().Should().Be(initialCount);
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
            var initialCount = context.RoleEntities.Count();

            // Act
            await repository.AddRoleAsync(roleEntity);

            // Assert
            context.RoleEntities.Count().Should().Be(initialCount + 1);
            var savedRole = await context.RoleEntities.FirstOrDefaultAsync(r =>
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
            context.RoleEntities.Add(roleEntity);
            await context.SaveChangesAsync();

            var repository = new RoleRepository(context);
            var initialCount = context.RoleEntities.Count();

            // Act
            await repository.DeleteRoleAsync(roleEntity);

            // Assert
            context.RoleEntities.Count().Should().Be(initialCount - 1);
            var deletedRole = await context.RoleEntities.FirstOrDefaultAsync(r =>
                r.ContactId == roleEntity.ContactId && r.AccountId == roleEntity.AccountId);
            deletedRole.Should().BeNull();
        }
    }

    [Fact]
    public void GetUnprocessedRoles_ShouldReturnOnlyUnprocessedRoles()
    {
        // Arrange
        var unprocessedRole = _fixture.Create<RefRoleEntity>();
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
            .Create();

        using (var context = new RefContext(GetDbOptions()))
        {
            context.AccountEntities.Add(accountEntity);
            context.ContactEntities.Add(contactEntity);
            context.RoleEntities.Add(roleEntity);
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

        var contactEntity = _fixture.Create<ContactEntity>();
        var contactEntity2 = _fixture.Create<ContactEntity>();

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
            context.AccountEntities.AddRange(accounts);
            context.Add(contactEntity);
            context.AddRange(roles);


            await context.SaveChangesAsync();

            var repository = new RoleRepository(context);

            // Act
            var result = await  repository.GetRolesForContactAsync(contactEntity.Email);

            // Assert
            result.Count().Should().Be(2);
        }

    }
}
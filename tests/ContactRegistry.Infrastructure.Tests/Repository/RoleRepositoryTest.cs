using ContactRegistry.Infrastructure.Tests.Utils;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Context;
using Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContactRegistry.Infrastructure.Tests.Repository
{
    public class RoleRepositoryTest
    {
        private ApplicationDbContext context;

        public RoleRepositoryTest()
        {
            context = DbContextMockExtensions.CreateInMemoryDbContext();
        }

        [Fact]
        public async Task AddRolesAsync()
        {
            var testAccount = new AlxAccount
            {
                Id = Guid.NewGuid(),
                AccountFlagEscActif = true,
                LegalName = "Test Legal Name",
                AccountNumber = "1234567890"
            };

            var testRole = new AlxRole
            {
                RoleId = Guid.NewGuid(),
                ContactId = Guid.NewGuid(), // This should be updated to match the contact's Id after creation
                AccountId = testAccount.Id,
                Onboarded = true,
                Account = testAccount
            };



            var roles = new List<AlxRole>() { testRole };

            // Act
            var repository = new RoleRepository(context);
            await repository.AddRolesAsync(roles);

            // Assert
            var result = await context.AlxRoles.FirstAsync();
            result.Should().NotBeNull();
            result.Should().Be(testRole);
        }

        [Fact]
        public async Task GetRolesAsync_WhenRolesExist_ReturnsRoles()
        {
            // Arrange
            var contactId = Guid.NewGuid();
            var accountId = Guid.NewGuid();

            var role1 = new CreRole
            {
                Id = Guid.NewGuid(),
                ContactId = contactId,
                AccountId = accountId,
                Deleted = null
            };

            var role2 = new CreRole
            {
                Id = Guid.NewGuid(),
                ContactId = contactId,
                AccountId = accountId,
                Deleted = null
            };

            context.CreRoles.AddRange(role1, role2);
            await context.SaveChangesAsync();

            var repository = new RoleRepository(context);

            // Act
            var roles = new List<CreRole>();
            await foreach (var role in repository.GetRolesAsync())
            {
                roles.Add(role);
            }

            // Assert
            roles.Should().HaveCount(2);
            roles.Should().ContainEquivalentOf(role1);
            roles.Should().ContainEquivalentOf(role2);
        }

        [Fact]
        public async Task GetRolesAsync_WhenNoRolesExist_ReturnsEmpty()
        {
            // Arrange
            var repository = new RoleRepository(context);

            // Act
            var roles = new List<CreRole>();
            await foreach (var role in repository.GetRolesAsync())
            {
                roles.Add(role);
            }

            // Assert
            roles.Should().BeEmpty();
        }
    }
}

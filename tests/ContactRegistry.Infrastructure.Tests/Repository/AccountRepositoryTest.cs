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
    public class AccountRepositoryTest
    {
        private readonly ApplicationDbContext context;

        public AccountRepositoryTest()
        {
            context = DbContextMockExtensions.CreateInMemoryDbContext();
        }

        [Fact]
        public async Task AddAccountsAsync()
        {
            // Arrange
            var account = new AlxAccount
            {
                AccountGlobalUniqueIdentifier = Guid.NewGuid(),
                AccountFlagEscActif = true,
                LegalName = "Test Legal Name",
                AccountNumber = "1234567890"
            };

            var accounts = new List<AlxAccount>() { account };

            // Act
            var repository = new AccountRepository(context);
            await repository.AddAccountsAsync(accounts);

            // Assert
            var result = await context.AlxAccounts.FirstAsync();
            result.Should().NotBeNull();
            result.Should().Be(account);
        }

        [Fact]
        public async Task GetAccountsAsync_WhenAccountsExist_ReturnsAccounts()
        {
            // Arrange
            var account1 = new CreAccount
            {
                Id = Guid.NewGuid(),
                AccountNumber = "1234567890",
                LegalName = "Test Legal Name 1",
                Updated = DateTime.UtcNow
            };

            var account2 = new CreAccount
            {
                Id = Guid.NewGuid(),
                AccountNumber = "0987654321",
                LegalName = "Test Legal Name 2",
                Updated = DateTime.UtcNow
            };

            context.CreAccounts.AddRange(account1, account2);
            await context.SaveChangesAsync();

            var repository = new AccountRepository(context);

            // Act
            var accounts = new List<CreAccount>();
            await foreach (var account in repository.GetAccountsAsync())
            {
                accounts.Add(account);
            }

            // Assert
            accounts.Should().HaveCount(2);
            accounts.Should().ContainEquivalentOf(account1);
            accounts.Should().ContainEquivalentOf(account2);
        }

        [Fact]
        public async Task GetAccountsAsync_WhenNoAccountsExist_ReturnsEmpty()
        {
            // Arrange
            var repository = new AccountRepository(context);

            // Act
            var accounts = new List<CreAccount>();
            await foreach (var account in repository.GetAccountsAsync())
            {
                accounts.Add(account);
            }

            // Assert
            accounts.Should().BeEmpty();
        }
    }
}

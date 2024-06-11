using Domain.Entities;
using FluentAssertions;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace ContactRegistry.Infrastructure.Tests.Context
{
    public class ApplicationDbContextTest
    {
        private DbContextOptions<ApplicationDbContext> CreateInMemoryOptions(string databaseName)
        {
            return new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;
        }

        [Fact]
        public void CanAddAndRetrieveAlxContact()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(CanAddAndRetrieveAlxContact));
            var testContact = new AlxContact
            {
                Id = Guid.NewGuid(),
                OfficeId = Guid.NewGuid(),
                IsCustomer = true,
                IsActive = true,
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                LandPhone = "1234567890",
                MobilePhone = "0987654321",
                JobDescription = "Developer",
                Roles = new System.Collections.Generic.List<AlxRole>()
            };

            // Act
            using (var context = new ApplicationDbContext(options))
            {
                context.AlxContacts.Add(testContact);
                context.SaveChanges();
            }

            // Assert
            using (var context = new ApplicationDbContext(options))
            {
                var retrievedContact = context.AlxContacts.FirstOrDefault(c => c.Id == testContact.Id);
                retrievedContact.Should().NotBeNull();
                retrievedContact.Should().BeEquivalentTo(testContact, options => options.Excluding(c => c.Roles));
            }
        }

        [Fact]
        public void CanAddAndRetrieveAlxAccount()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(CanAddAndRetrieveAlxAccount));
            var testAccount = new AlxAccount
            {
                Id = Guid.NewGuid(),
                AccountFlagEscActif = true,
                LegalName = "Test Legal Name",
                AccountNumber = "1234567890"
            };

            // Act
            using (var context = new ApplicationDbContext(options))
            {
                context.AlxAccounts.Add(testAccount);
                context.SaveChanges();
            }

            // Assert
            using (var context = new ApplicationDbContext(options))
            {
                var retrievedAccount = context.AlxAccounts.FirstOrDefault(a => a.Id == testAccount.Id);
                retrievedAccount.Should().NotBeNull();
                retrievedAccount.Should().BeEquivalentTo(testAccount);
            }
        }

        [Fact]
        public void CanAddAndRetrieveAlxRole()
        {
            // Arrange
            var options = CreateInMemoryOptions(nameof(CanAddAndRetrieveAlxRole));
            var testRole = new AlxRole
            {
                RoleId = Guid.NewGuid(),
                ContactId = Guid.NewGuid(),
                AccountId = Guid.NewGuid(),
                Onboarded = true
            };

            // Act
            using (var context = new ApplicationDbContext(options))
            {
                context.AlxRoles.Add(testRole);
                context.SaveChanges();
            }

            // Assert
            using (var context = new ApplicationDbContext(options))
            {
                var retrievedRole = context.AlxRoles.FirstOrDefault(r => r.RoleId == testRole.RoleId);
                retrievedRole.Should().NotBeNull();
                retrievedRole.Should().BeEquivalentTo(testRole);
            }
        }
    }
}

using ContactRegistry.Infrastructure.Tests.Utils;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Context;
using Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;

namespace ContactRegistry.Infrastructure.Tests.Repository
{
    public class ContactRepositoryTest
    {
        private ApplicationDbContext context;

        public ContactRepositoryTest()
        {
            context = DbContextMockExtensions.CreateInMemoryDbContext();
        }

        [Fact]
        public async Task AddContactsAsync()
        {
            // Arrange
            var testAccount = new AlxAccount
            {
                AccountGlobalUniqueIdentifier = Guid.NewGuid(),
                AccountFlagEscActif = true,
                LegalName = "Test Legal Name",
                AccountNumber = "1234567890"
            };

            var testRole = new AlxRole
            {
                RoleId = Guid.NewGuid(),
                ContactId = Guid.NewGuid(),
                AccountId = testAccount.AccountGlobalUniqueIdentifier,
                Onboarded = true,
                Account = testAccount,
                RoleDelegataireEmail = "test@email.fr",
                IsFavorite = false,
                RoleSignatory = false
            };

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
                Roles = new List<AlxRole> { testRole }
            };



            var contacts = new List<AlxContact>() { testContact };

            // Act
            var repository = new ContactRepository(context);
            await repository.AddContactsAsync(contacts);

            // Assert
            var result = await context.AlxContacts.FirstAsync();
            result.Should().NotBeNull();
            result.Should().Be(testContact);
        }

        [Fact]
        public async Task GetContactsAsync_WhenContactsExist_ReturnsContacts()
        {
            // Arrange
            var contact1 = new CreContact
            {
                Id = Guid.NewGuid(),
                OfficeId = Guid.NewGuid(),
                IsCustomer = true,
                IsActive = true, 
                Updated = DateTime.UtcNow, 
                Deleted = null, 
                FirstName = "Test Contact 1",
                LastName = "Last Name 1", 
                Email = "test1@example.com",
                LandPhone = "123-456-7890", 
                MobilePhone = "987-654-3210", 
                JobDescription = "Job Description 1", 
                Source = "Source 1", 
                Roles = new List<CreRole>
                {
                    new CreRole 
                    { 
                        RoleId = Guid.NewGuid(), 
                        AccountId = new Guid("ff05e5c7-22b1-4366-9a67-aaa51d6742a0"),
                        ContactId = Guid.NewGuid(),
                        Onboarded = true,
                        RoleDelegataireEmail = "test@email.fr",
                        IsFavorite = false,
                        RoleSignatory = false
                    },
                }
            };

            var contact2 = new CreContact
            {
                Id = Guid.NewGuid(),
                OfficeId = Guid.NewGuid(),
                IsCustomer = false,
                IsActive = true,
                Updated = DateTime.UtcNow,
                Deleted = null,
                FirstName = "Test Contact 2",
                LastName = "Last Name 2",
                Email = "test2@example.com",
                LandPhone = "234-567-8901",
                MobilePhone = "876-543-2109",
                JobDescription = "Job Description 2",
                Source = "Source 2",
                Roles = new List<CreRole>
                {
                    new CreRole 
                    { 
                        RoleId = Guid.NewGuid(), 
                        AccountId = new Guid("040e4782-28b3-45ba-8f11-c699538ffdad"),
                        ContactId = Guid.NewGuid(),
                        Onboarded = true,
                        RoleDelegataireEmail = "test@email.fr",
                        IsFavorite = false,
                        RoleSignatory = false
                    },
                }
            };

            context.CreContacts.AddRange(contact1, contact2);
            await context.SaveChangesAsync();

            var repository = new ContactRepository(context);

            // Act
            var contacts = new List<CreContact>();
            await foreach (var contact in repository.GetContactsAsync())
            {
                contacts.Add(contact);
            }

            // Assert
            contacts.Should().HaveCount(2);
            contacts.Should().ContainEquivalentOf(contact1);
            contacts.Should().ContainEquivalentOf(contact2);
        }

        [Fact]
        public async Task GetContactsAsync_WhenNoContactsExist_ReturnsEmpty()
        {
            // Arrange
            var repository = new ContactRepository(context);

            // Act
            var contacts = new List<CreContact>();
            await foreach (var contact in repository.GetContactsAsync())
            {
                contacts.Add(contact);
            }

            // Assert
            contacts.Should().BeEmpty();
        }
    }
}

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
                AccountGlobalUniqueIdentifier = Guid.NewGuid(),
                AccountNumber = "ABC12345",
                DeploymentStatus = "Success",
                LegalName = "Pulse Corporation",
                AccountCommercialName = "Pulse Corp",
                AccountType = "Corporation",
                AccountEmail = "info@pulse.com",
                AccountNafIdentifier = "NAF123456",
                AccountFlagEscActif = true,
                AccountSectorCode = "Sector123",
                AccountTaxeValeurAjoutee = "TVA123456",
                AccountDeliveryEmail = "delivery@pulse.com",
                AccountBillingEmail = "billing@pulse.com",
                AccountTaxationSystem = "Standard",
                AccountSourceName = "SourceName",
                AccountISIN = "ISIN123456",
                AccountRegisterIdentification1 = "RegID123456",
                AccountStaffSize = "100",
                AccountDeliveryFax = "123-456-7890",
                AccountBillingFax = "098-765-4321",
                AccountTurnoverSlice = "1M-10M",
                AccountRegimeFiscal = "RegimeFiscal",
                AccountTypeTenueComptable = "TypeTenueComptable",
                AccountFormeJuridique = "FormeJuridique",
                AccountStaffSizeSlice = "50-100",
                AccountEscCategory = "Category",
                AccountCodeFormeJuridique = "CodeFormeJuridique",
                AccountInsertedDate = DateTime.Now,
                AccountUpdatedDate = DateTime.Now,
                CreatedBy = "System",
                ModifiedBy = "System",
                DeliveryAddressLine1 = "123 Delivery St",
                DeliveryAddressLine2 = "Suite 100",
                DeliveryAddressLine3 = string.Empty,
                DeliveryCity = "Delivery City",
                DeliveryZipCode = "12345",
                DeliveryCountry = "Country",
                DeliveryState = "State",
                BillingAddressLine1 = "456 Billing Ave",
                BillingAddressLine2 = "Suite 200",
                BillingAddressLine3 = "",
                BillingCity = "Billing City",
                BillingZipCode = "67890",
                BillingCountry = "Country",
                BillingState = "State",
                DeploymentDate = DateTime.Now
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
                var retrievedAccount = context.AlxAccounts.FirstOrDefault(a => a.AccountGlobalUniqueIdentifier == testAccount.AccountGlobalUniqueIdentifier);
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
                Onboarded = true,
                RoleDelegataireEmail = "test@email.fr",
                IsFavorite = false,
                RoleSignatory = false
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

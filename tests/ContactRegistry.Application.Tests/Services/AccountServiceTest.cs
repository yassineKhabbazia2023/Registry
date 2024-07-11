using Application.Interfaces;
using Application.Models;
using Application.Services;
using Domain.Entities;
using FluentAssertions;
using Moq;
using System.Text.Json;
using CreAccount = Application.Models.CreAccount;

namespace ContactRegistry.Application.Tests.Services
{
    public class AccountServiceTest
    {
        [Fact]
        public async Task ProcessAccountAsync_Adds_Account()
        {
            // Arrange
            var account = new AccountCsv()
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
                AccountTurnover = "1M-10M",
                AccountRegimeFiscal = "RegimeFiscal",
                AccountTypeTenueComptable = "TypeTenueComptable",
                AccountFormeJuridique = "FormeJuridique",
                AccountStaffSizeSlice = "50-100",
                AccountEscCategory = "Category",
                AccountCodeFormeJuridique = "CodeFormeJuridique",
                AccountInsertedDate = DateTime.Now.ToString(),
                AccountUpdatedDate = DateTime.Now.ToString(),
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
                DeploymentDate = DateTime.Now.ToString(),
                AccountBillingPhone = "BillingPhone",
                AccountDeliveryPhone = "DeliveryPhone"
            };

            var accounts = new List<AccountCsv>() { account };

            var expectedAccountData = accounts
               .Select(
               a => new AlxAccount
               {
                   AccountGlobalUniqueIdentifier = a.AccountGlobalUniqueIdentifier,
                   AccountFlagEscActif = a.AccountFlagEscActif,
                   LegalName = a.LegalName,
                   AccountNumber = a.AccountNumber,
                   AccountCommercialName = a.AccountCommercialName,
                   AccountType = a.AccountType,
                   AccountEmail = a.AccountEmail,
                   AccountNafIdentifier = a.AccountNafIdentifier,
                   AccountSectorCode = a.AccountSectorCode,
                   AccountTaxeValeurAjoutee = a.AccountTaxeValeurAjoutee,
                   AccountDeliveryEmail = a.AccountDeliveryEmail,
                   AccountBillingEmail = a.AccountBillingEmail,
                   AccountTaxationSystem = a.AccountTaxationSystem,
                   AccountSourceName = a.AccountSourceName,
                   AccountISIN = a.AccountISIN,
                   AccountRegisterIdentification1 = a.AccountRegisterIdentification1,
                   AccountStaffSize = a.AccountStaffSize,
                   AccountDeliveryFax = a.AccountDeliveryFax,
                   AccountBillingFax = a.AccountBillingFax,
                   AccountTurnover = a.AccountTurnover,
                   AccountRegimeFiscal = a.AccountRegimeFiscal,
                   AccountTypeTenueComptable = a.AccountTypeTenueComptable,
                   AccountFormeJuridique = a.AccountFormeJuridique,
                   AccountStaffSizeSlice = a.AccountStaffSizeSlice,
                   AccountEscCategory = a.AccountEscCategory,
                   AccountCodeFormeJuridique = a.AccountCodeFormeJuridique,
                   AccountInsertedDate = DateTime.Parse(a.AccountInsertedDate),
                   AccountUpdatedDate = DateTime.Parse(a.AccountUpdatedDate),
                   CreatedBy = a.CreatedBy,
                   ModifiedBy = a.ModifiedBy,
                   DeliveryAddressLine1 = a.DeliveryAddressLine1,
                   DeliveryAddressLine2 = a.DeliveryAddressLine2,
                   DeliveryAddressLine3 = null,
                   DeliveryCity = a.DeliveryCity,
                   DeliveryZipCode = a.DeliveryZipCode,
                   DeliveryCountry = a.DeliveryCountry,
                   DeliveryState = a.DeliveryState,
                   BillingAddressLine1 = a.BillingAddressLine1,
                   BillingAddressLine2 = a.BillingAddressLine2,
                   BillingAddressLine3 = null,
                   BillingCity = a.BillingCity,
                   BillingZipCode = a.BillingZipCode,
                   BillingCountry = a.BillingCountry,
                   BillingState = a.BillingState,
                   DeploymentStatus = a.DeploymentStatus,
                   DeploymentDate = DateTime.Parse(a.DeploymentDate),
                   AccountBillingPhone = a.AccountBillingPhone,
                   AccountDeliveryPhone = a.AccountDeliveryPhone,
               }).ToList();

            var accountRepository = new Mock<IAccountRepository>(MockBehavior.Strict);
            accountRepository.Setup(r => r.AddAccountsAsync(It.IsAny<IEnumerable<AlxAccount>>())).
                Callback<IEnumerable<AlxAccount>>(data =>
                {
                    data.Should().BeEquivalentTo(expectedAccountData);
                })
                .Returns(Task.CompletedTask);

            var processDeltaTriggerRepositoryMock = new Mock<IProcessDeltaTriggerRepository>(MockBehavior.Strict);
            processDeltaTriggerRepositoryMock.Setup(p => p.UpdateAccountProcessAsync(true)).Returns(Task.CompletedTask);

            // Act
            var accountService = new AccountService(accountRepository.Object, processDeltaTriggerRepositoryMock.Object);
            await accountService.ProcessAccountAsync(accounts);

            accountRepository.VerifyAll();
        }

        [Fact]
        public async Task StreamAccountsJsonAsync_Writes_ExpectedData()
        {
            // Arrange
            var account1 = new Domain.Entities.CreAccount
            {
                Id = new Guid("e400bb2f-cebb-422c-a450-e0e26ff3313b"),
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
                AccountTurnover = "1M-10M",
                AccountRegimeFiscal = "RegimeFiscal",
                AccountTypeTenueComptable = "TypeTenueComptable",
                AccountFormeJuridique = "FormeJuridique",
                AccountStaffSizeSlice = "50-100",
                AccountEscCategory = "Category",
                AccountCodeFormeJuridique = "CodeFormeJuridique",
                AccountInsertedDate = null,
                AccountUpdatedDate = null,
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
                DeploymentDate = null
            };

            var applicationAccount = new CreAccount
            {
                AccountGlobalUniqueIdentifier = new Guid("e400bb2f-cebb-422c-a450-e0e26ff3313b"),
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
                AccountInsertedDate = null,
                AccountUpdatedDate = null,
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
                DeploymentDate = null
            };

            IEnumerable<Domain.Entities.CreAccount> accounts = new List<Domain.Entities.CreAccount> { account1 };

            IEnumerable<CreAccount> applicationAccounts = new List<CreAccount> { applicationAccount };

            var options = new JsonSerializerOptions { WriteIndented = true };
            var expectedJsonData =  JsonSerializer.Serialize(applicationAccounts, options);

            var accountRepository = new Mock<IAccountRepository>();
            accountRepository.Setup(r => r.GetAccountsAsync()).Returns(GetAsyncEnumerable(accounts));

            var stream = new MemoryStream();
            var streamWriter = new StreamWriter(stream);

            var processDeltaTriggerRepositoryMock = new Mock<IProcessDeltaTriggerRepository>(MockBehavior.Strict);


            var accountService = new AccountService(accountRepository.Object, processDeltaTriggerRepositoryMock.Object);

            // Act
            await accountService.StreamAccountsJsonAsync(streamWriter);

            stream.Position = 0;
            var reader = new StreamReader(stream);
            var jsonData = await reader.ReadToEndAsync();

            // Assert
            accountRepository.Verify(c => c.GetAccountsAsync(), Times.Once);
            jsonData.Should().BeEquivalentTo(expectedJsonData);
        }

        private async IAsyncEnumerable<Domain.Entities.CreAccount> GetAsyncEnumerable(IEnumerable<Domain.Entities.CreAccount> accounts)
        {
            foreach (var account in accounts)
            {
                yield return account;
            }
        }
    }
}

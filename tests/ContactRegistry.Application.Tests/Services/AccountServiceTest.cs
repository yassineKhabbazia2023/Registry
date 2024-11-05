using Application.Interfaces;
using Application.Models;
using Application.Services;
using AutoFixture;
using Castle.Core.Logging;
using Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using CreAccount = Application.Models.CreAccount;

namespace ContactRegistry.Application.Tests.Services
{
    public class AccountServiceTest
    {
        private readonly Fixture _fixture;

        public AccountServiceTest()
        {
            _fixture = new Fixture();
            _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList().ForEach(b => _fixture.Behaviors.Remove(b));
            _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        }

        [Theory]
        [InlineData(1,1)]
        [InlineData(2000,1)]
        public async Task ProcessAccountAsync_Adds_Account_With2000Accounts(int accountCsvLenght, int functionTimeCalled)
        {
            // Arrange
            var accounts = _fixture.Build<AccountCsv>()
                .With(a => a.AccountInsertedDate, DateTime.UtcNow.ToString())
                .With(a => a.AccountUpdatedDate, DateTime.UtcNow.ToString())
                .With(a => a.DeploymentDate, DateTime.UtcNow.ToString())
                .Without(a => a.DeliveryAddressLine3)
                .Without(a => a.BillingAddressLine3)
                .CreateMany(accountCsvLenght);

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
            accountRepository.Setup(r => r.AddAccountsAsync(It.IsAny<List<AlxAccount>>())).
                Callback<IEnumerable<AlxAccount>>(data =>
                {
                    data.Count().Should().BeGreaterThanOrEqualTo(expectedAccountData.Count);
                })
                .Returns(Task.CompletedTask);

            accountRepository.Setup(r => r.GetCountAccountActifAsync())
               .ReturnsAsync((creContactActif: 20, alxContactActif: 44));
            var processDeltaTriggerRepositoryMock = new Mock<IProcessDeltaTriggerRepository>(MockBehavior.Strict);
            processDeltaTriggerRepositoryMock.Setup(p => p.UpdateAccountProcessAsync(true)).Returns(Task.CompletedTask);

            var loggerMock = new Mock<ILogger<AccountService>>(MockBehavior.Default);

            // Act
            var accountService = new AccountService(loggerMock.Object, accountRepository.Object, processDeltaTriggerRepositoryMock.Object);
            await accountService.ProcessAccountAsync(accounts);

            accountRepository.VerifyAll();
            accountRepository.Verify(a => a.AddAccountsAsync(It.IsAny<List<AlxAccount>>()), Times.AtLeast(functionTimeCalled));
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(2000, 1)]
        public async Task AddAccountAsync_Adds_Account_With2000Accounts(int accountCsvLenght, int functionTimeCalled)
        {
            // Arrange
            var accounts = _fixture.Build<RefAccountCsv>()
                .With(a => a.AccountInsertedDate, DateTime.UtcNow.ToString())
                .With(a => a.AccountUpdatedDate, DateTime.UtcNow.ToString())
                .Without(a => a.DeliveryAddressLine3)
                .Without(a => a.BillingAddressLine3)
                .CreateMany(accountCsvLenght);

            var accountRepository = new Mock<IAccountRepository>();
            accountRepository.Setup(r => r.AddAccountsAsync(It.IsAny<IEnumerable<RefAccountCsv>>())).
                Callback<IEnumerable<RefAccountCsv>>(data =>
                {
                    data.Count().Should().BeGreaterThanOrEqualTo(accounts.Count());
                })
                .Returns(Task.CompletedTask);

            var processDeltaTriggerRepositoryMock = new Mock<IProcessDeltaTriggerRepository>();

            var loggerMock = new Mock<ILogger<AccountService>>(MockBehavior.Default);

            // Act
            var accountService = new AccountService(loggerMock.Object, accountRepository.Object, processDeltaTriggerRepositoryMock.Object);
            await accountService.InsertAccountsAsync(accounts);

            accountRepository.VerifyAll();
            accountRepository.Verify(a => a.AddAccountsAsync(It.IsAny<IEnumerable<RefAccountCsv>>()), Times.AtLeast(functionTimeCalled));
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

            accountRepository.Setup(r => r.GetCountAccountActifAsync())
               .ReturnsAsync((creContactActif: 20, alxContactActif: 44));
            var stream = new MemoryStream();
            var streamWriter = new StreamWriter(stream);

            var processDeltaTriggerRepositoryMock = new Mock<IProcessDeltaTriggerRepository>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<AccountService>>(MockBehavior.Default);


            var accountService = new AccountService(loggerMock.Object, accountRepository.Object, processDeltaTriggerRepositoryMock.Object);

            // Act
            await accountService.StreamAccountsJsonAsync(streamWriter);

            stream.Position = 0;
            var reader = new StreamReader(stream);
            var jsonData = await reader.ReadToEndAsync();

            // Assert
            accountRepository.Verify(c => c.GetAccountsAsync(), Times.Once);
            jsonData.Should().BeEquivalentTo(expectedJsonData);
        }

        [Fact]
        public async Task ClearAlxAsync_Should_Be_Success()
        {
            var accountRepository = new Mock<IAccountRepository>();
            accountRepository.Setup(a => a.ClearAlxAsync()).Returns(Task.CompletedTask); 
            
            var processDeltaTriggerRepositoryMock = new Mock<IProcessDeltaTriggerRepository>(MockBehavior.Strict);
            var loggerMock = new Mock<ILogger<AccountService>>(MockBehavior.Default);

            var accountService = new AccountService(loggerMock.Object, accountRepository.Object, processDeltaTriggerRepositoryMock.Object);

            await accountService.ClearAlxAsync();

            accountRepository.Verify(a => a.ClearAlxAsync(), Times.Once);
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

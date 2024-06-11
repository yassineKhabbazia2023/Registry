using Application.Interfaces;
using Application.Models;
using Application.Services;
using Domain.Entities;
using FluentAssertions;
using Moq;
using System.Text.Json;

namespace ContactRegistry.Application.Tests.Services
{
    public class AccountServiceTest
    {
        [Fact]
        public async Task ProcessAccountAsync_Adds_Account()
        {
            // Arrange
            var account = new AccountCsv(
                Id: Guid.NewGuid(),
                AccountNumber: "123456789",
                LegalName: "Example Corp",
                AccountFlagESCActif: true
            );

            var accounts = new List<AccountCsv>() { account };

            var expectedAccountData = accounts
               .Select(
               a => new AlxAccount
               {
                   Id = a.Id,
                   LegalName = a.LegalName,
                   AccountNumber = a.AccountNumber,
                   AccountFlagEscActif = a.AccountFlagESCActif
               }).ToList();

            var accountRepository = new Mock<IAccountRepository>(MockBehavior.Strict);
            accountRepository.Setup(r => r.AddAccountsAsync(It.IsAny<IEnumerable<AlxAccount>>())).
                Callback<IEnumerable<AlxAccount>>(data =>
                {
                    data.Should().BeEquivalentTo(expectedAccountData);
                })
                .Returns(Task.CompletedTask);

            // Act
            var accountService = new AccountService(accountRepository.Object);
            await accountService.ProcessAccountAsync(accounts);

            accountRepository.VerifyAll();
        }

        [Fact]
        public async Task StreamAccountsJsonAsync_Writes_ExpectedData()
        {
            // Arrange
            var account1 = new Domain.Entities.CreAccount
            {
                Id = Guid.NewGuid(),
                AccountNumber = "1234567890",
                LegalName = "Test Legal Name 1",
                Updated = DateTime.UtcNow
            };

            IEnumerable<Domain.Entities.CreAccount> accounts = new List<Domain.Entities.CreAccount> { account1 };

            var options = new JsonSerializerOptions { WriteIndented = true };
            var expectedJsonData =  JsonSerializer.Serialize(accounts, options);

            var accountRepository = new Mock<IAccountRepository>();
            accountRepository.Setup(r => r.GetAccountsAsync()).Returns(GetAsyncEnumerable(accounts));

            var stream = new MemoryStream();
            var streamWriter = new StreamWriter(stream);

            var accountService = new AccountService(accountRepository.Object);

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

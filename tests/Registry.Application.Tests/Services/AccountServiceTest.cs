using Application.Interfaces;
using Application.Models;
using Application.Services;
using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

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


            var loggerMock = new Mock<ILogger<AccountService>>(MockBehavior.Default);

            // Act
            var accountService = new AccountService(loggerMock.Object, accountRepository.Object);
            await accountService.InsertAccountsAsync(accounts);

            accountRepository.VerifyAll();
            accountRepository.Verify(a => a.AddAccountsAsync(It.IsAny<IEnumerable<RefAccountCsv>>()), Times.AtLeast(functionTimeCalled));
        }

    }
}

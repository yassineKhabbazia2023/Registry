using Application.Helpers;
using Application.Interfaces;
using Application.Models;
using Application.Services;
using AutoFixture;
using Domain.Constants.Enums;
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

            var validationHelper = new ValidationHelper<RefAccountCsv>();
            var loggerMock = new Mock<ILogger<AccountService>>(MockBehavior.Default);

            // Act
            var accountService = new AccountService(loggerMock.Object, accountRepository.Object, validationHelper);
            await accountService.InsertAccountsAsync(accounts);

            accountRepository.VerifyAll();
            accountRepository.Verify(a => a.AddAccountsAsync(It.IsAny<IEnumerable<RefAccountCsv>>()), Times.AtLeast(functionTimeCalled));
        }

        [Fact]
        public void ValidateContacts_ShouldReturnErrors_WhenInvalidContactsProvided()
        {
            // Arrange
            var accounts = new List<RefAccountCsv>
            {
                new() { AccountFlagStatus = 5,LegalName ="Test1", AccountNumber = "Test1", Operation = OperationStatusEnum.INSERT.ToString()  },
                new() { AccountFlagStatus = 1,LegalName =" ", AccountNumber = "Test1", Operation = OperationStatusEnum.INSERT.ToString()  },
                new() { AccountFlagStatus = 1,LegalName ="Test1", AccountNumber = "Test@1", Operation = OperationStatusEnum.INSERT.ToString()  },
                new() { AccountFlagStatus = 1,LegalName ="Test1", AccountNumber = "Test1", Operation = "YOLO"  },
            };
            var accountRepository = new Mock<IAccountRepository>();
            var validationHelper = new ValidationHelper<RefAccountCsv>();
            var loggerMock = new Mock<ILogger<AccountService>>(MockBehavior.Default);
            
            var service = new AccountService(loggerMock.Object, accountRepository.Object, validationHelper);

            // Act
            var errors = validationHelper.Validate(accounts);

            // Assert
            Assert.Equal(4, errors.Errors.Count);
        }
    }
}

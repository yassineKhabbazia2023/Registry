//// <copyright file="AccountServiceTest.cs" company="Pulse">
//// Copyright (c) Pulse. All rights reserved.
//// </copyright>

using Application.Consts;
using Application.Enums;
using Application.Interfaces;
using Application.Models;
using Application.Requests;
using Application.Services;
using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Pulse.Back.Events.IntegrationEvents.EventsData;
using Pulse.Registry.Domain.Entities;
using Pulse.Registry.Domain.Entities.Accounts;
using Registry.Application.Consts;

namespace Registry.Application.Tests.Services;

public class AccountServiceTest
{
    private readonly Fixture _fixture;
    private readonly Mock<ILogger<AccountService>> _logger;

    public AccountServiceTest()
    {
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList().ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        _logger = new Mock<ILogger<AccountService>>();
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
        var operationRepositoryMock = new Mock<IOperationRepository>();

        // Act
        var accountService = new AccountService(accountRepository.Object, operationRepositoryMock.Object, _logger.Object);
        await accountService.InsertAccountsAsync(accounts);

        accountRepository.VerifyAll();
        accountRepository.Verify(a => a.AddAccountsAsync(It.IsAny<IEnumerable<RefAccountCsv>>()), Times.AtLeast(functionTimeCalled));
    }

    [Fact]
    public async Task UpdateAccountProcessStatusAsync_Should_Update_OperationsList()
    {
        // Arrange

        var operations = _fixture.CreateMany<RegOperationEntity>(10);

        var criteria = new OperationSearchCriteria
        {
            OperationName = OperationAction.Update,
            OperationApprovalStatus = ApprovalStatus.Approved,
        };

        var operationsRepositoryMock = new Mock<IOperationRepository>();
        operationsRepositoryMock.Setup(r => r.FetchOperationsByCriteriaAsync(It.IsAny<OperationSearchCriteria>(), It.IsAny<OperationStrategyType>(), It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<string>()))
            .Callback<OperationSearchCriteria, OperationStrategyType, string, bool?, string>((criteria, operationStrategyType, accountNumber, filterByPublished, secondaryFilter) =>
            {
                criteria.OperationName.Should().Be(OperationAction.Update);
                criteria.OperationApprovalStatus.Should().Be(ApprovalStatus.Approved);
            })
            .ReturnsAsync(operations);

        operationsRepositoryMock.Setup(r => r.BulkUpdateOperationsStatusAsync(It.IsAny<string>(), It.IsAny<IEnumerable<RegOperationEntity>>()))
            .Callback<string, IEnumerable<RegOperationEntity>>((operationName, operations) =>
            {
                operations.Count().Should().Be(operations.Count());
            })
            .ReturnsAsync(true);

        // Act
        var accountService = new AccountService(null, operationsRepositoryMock.Object, _logger.Object);
        await accountService.UpdateAccountProcessStatusAsync("TestAccount", OperationAction.Update);

        // Assert
        operationsRepositoryMock.VerifyAll();
    }

    [Fact]
    public async Task SyncAccountAsync_When_EventType_Insert_AccountExists_Returns_False()
    {
        // Arrange
        var eventType = OperationAction.Insert;

        AccountStateEventData accountStateEventData = new AccountStateEventData()
        {
            AccountGlobalUniqueId = Guid.NewGuid(),
            AccountId = 1,
            AccountNumber = "TestAccount",
            LegalName = "TestLegalName",
            SiretNumber = "TestSiretNumber",
            Status = "TestStatus",
            AccountRoutingCode = "0-B2G",
            AccountRoutingLabel = "B2G",
            AccountLegalFormLabel = "Entrepreneur individuel",
            AccountElectronicAddressId = "factures@example.com",
        };

        var accountEntity = new AccountEntity()
        {
            AccountGlobalUniqueId = Guid.NewGuid(),
            AccountId = 1,
            AccountNumber = "TestAccount",
        };

        var operationRepositoryMock = new Mock<IOperationRepository>(MockBehavior.Strict);
        var accountRepositoryMock = new Mock<IAccountRepository>(MockBehavior.Strict);

        accountRepositoryMock.Setup(s => s.GetAccountByNumberOrIdAsync(It.IsAny<string>()))
            .Callback<string>((accountNumber) =>
            {
                accountNumber.Should().NotBe(null);
                Assert.Equal(accountNumber, accountStateEventData.AccountNumber);
            }).ReturnsAsync(accountEntity);

        accountRepositoryMock.Setup(s => s.AddAccountAsync(It.IsAny<AccountEntity>()))
            .Callback<AccountEntity>((accountToInsert) =>
            {
                accountToInsert.Should().NotBeNull();
                Assert.Equal(accountStateEventData.AccountRoutingCode, accountToInsert.AccountRoutingCode);
                Assert.Equal(accountStateEventData.AccountRoutingLabel, accountToInsert.AccountRoutingLabel);
                Assert.Equal(accountStateEventData.AccountLegalFormLabel, accountToInsert.AccountLegalFormLabel);
                Assert.Equal(accountStateEventData.AccountElectronicAddressId, accountToInsert.AccountElectronicAddressId);
            })
            .Returns(Task.CompletedTask);
        // Act

        var accountService = new AccountService(accountRepositoryMock.Object, operationRepositoryMock.Object, _logger.Object);
        var result = await accountService.SyncAcountAsync(accountStateEventData, eventType);

        // Assert
        accountRepositoryMock.Verify(r => r.GetAccountByNumberOrIdAsync(accountStateEventData.AccountNumber), Times.Once);
        accountRepositoryMock.Verify(r => r.AddAccountAsync(accountEntity), Times.Never);
        accountRepositoryMock.Verify(r => r.UpdateAccountAsync(accountEntity), Times.Never);
        accountRepositoryMock.Verify(r => r.RemoveAccountAsync(accountEntity), Times.Never);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SyncAccountAsync_When_EventType_Insert_AccountNotExists_Returns_True()
    {
        // Arrange
        var eventType = OperationAction.Insert;

        AccountStateEventData accountStateEventData = new AccountStateEventData()
        {
            AccountGlobalUniqueId = Guid.NewGuid(),
            AccountId = 1,
            AccountNumber = "TestAccount",
            LegalName = "TestLegalName",
            SiretNumber = "TestSiretNumber",
            Status = "TestStatus",
            AccountRoutingCode = "0-B2B",
            AccountRoutingLabel = "B2B",
            AccountLegalFormLabel = "Société par actions simplifiée",
            AccountElectronicAddressId = "invoices@example.com",
        };

        var accountEntity = new AccountEntity()
        {
            AccountGlobalUniqueId = Guid.NewGuid(),
            AccountId = 1,
            AccountNumber = "TestAccount",
        };

        var operationRepositoryMock = new Mock<IOperationRepository>(MockBehavior.Strict);

        var accountRepositoryMock = new Mock<IAccountRepository>(MockBehavior.Strict);

        accountRepositoryMock.Setup(s => s.GetAccountByNumberOrIdAsync(It.IsAny<string>()))
            .Callback<string>((accountNumber) =>
            {
                Assert.Equal("TestAccount", accountNumber);
            })
            .ReturnsAsync(It.IsAny<AccountEntity>());

        accountRepositoryMock.Setup(s => s.AddAccountAsync(It.IsAny<AccountEntity>()))
            .Callback<AccountEntity>((accountToInsert) =>
            {
                accountToInsert.Should().NotBeNull();
                Assert.Equal(accountStateEventData.AccountRoutingCode, accountToInsert.AccountRoutingCode);
                Assert.Equal(accountStateEventData.AccountRoutingLabel, accountToInsert.AccountRoutingLabel);
                Assert.Equal(accountStateEventData.AccountLegalFormLabel, accountToInsert.AccountLegalFormLabel);
                Assert.Equal(accountStateEventData.AccountElectronicAddressId, accountToInsert.AccountElectronicAddressId);
            })
            .Returns(Task.CompletedTask);

        // Act
        var accountService = new AccountService(accountRepositoryMock.Object, operationRepositoryMock.Object, _logger.Object);

        var result = await accountService.SyncAcountAsync(accountStateEventData, eventType);

        // Assert
        accountRepositoryMock.Verify(r => r.GetAccountByNumberOrIdAsync(accountStateEventData.AccountNumber), Times.Once);
        accountRepositoryMock.Verify(r => r.AddAccountAsync(accountEntity), Times.Never);
        accountRepositoryMock.Verify(r => r.UpdateAccountAsync(accountEntity), Times.Never);
        accountRepositoryMock.Verify(r => r.RemoveAccountAsync(accountEntity), Times.Never);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SyncAccountAsync_When_EventType_Update_AccountExists_Returns_true()
    {
        // Arrange
        var eventType = OperationAction.Update;
        Guid identifier = new Guid("17499ca4-599f-424e-a05c-e3c05571c9ff");

        AccountStateEventData accountStateEventData = new AccountStateEventData()
        {
            AccountGlobalUniqueId = identifier,
            AccountId = 1,
            AccountNumber = "TestAccount2",
            LegalName = "TestLegalName",
            SiretNumber = "TestSiretNumber",
            Status = "TestStatus",
            AccountRoutingCode = "0-B2B",
            AccountRoutingLabel = "B2B",
            AccountLegalFormLabel = "Société par actions simplifiée",
            AccountElectronicAddressId = "invoices@example.com",
        };

        var accountEntity = new AccountEntity()
        {
            AccountGlobalUniqueId = identifier,
            AccountId = 1,
            AccountNumber = "TestAccount",
        };

        var updatedAccountEntity = new AccountEntity()
        {
            AccountGlobalUniqueId = identifier,
            AccountId = accountStateEventData.AccountId,
            AccountNumber = accountStateEventData.AccountNumber,
        };

        var operationRepositoryMock = new Mock<IOperationRepository>(MockBehavior.Strict);
        var accountRepositoryMock = new Mock<IAccountRepository>(MockBehavior.Strict);

        accountRepositoryMock.Setup(s => s.GetAccountByNumberOrIdAsync(It.IsAny<string>()))
            .Callback<string>((accountNumber) =>
            {
                accountNumber.Should().NotBe(null);
                Assert.Equal(accountNumber, accountStateEventData.AccountNumber);
            }).ReturnsAsync(accountEntity);

        accountRepositoryMock.Setup(s => s.UpdateAccountAsync(It.IsAny<AccountEntity>()))
            .Callback<AccountEntity>((accountToUpdate) =>
            {
                accountToUpdate.Should().NotBeNull();
                Assert.Equal(updatedAccountEntity.AccountNumber, accountToUpdate.AccountNumber);
                Assert.Equal(updatedAccountEntity.AccountGlobalUniqueId, accountToUpdate.AccountGlobalUniqueId);
                Assert.Equal(updatedAccountEntity.AccountId, accountToUpdate.AccountId);
                Assert.Equal(accountStateEventData.AccountRoutingCode, accountToUpdate.AccountRoutingCode);
                Assert.Equal(accountStateEventData.AccountRoutingLabel, accountToUpdate.AccountRoutingLabel);
                Assert.Equal(accountStateEventData.AccountLegalFormLabel, accountToUpdate.AccountLegalFormLabel);
                Assert.Equal(accountStateEventData.AccountElectronicAddressId, accountToUpdate.AccountElectronicAddressId);
            })
            .Returns(Task.CompletedTask);

        // Act
        var accountService = new AccountService(accountRepositoryMock.Object, operationRepositoryMock.Object, _logger.Object);
        var result = await accountService.SyncAcountAsync(accountStateEventData, eventType);

        // Assert
        accountRepositoryMock.VerifyAll();
        accountRepositoryMock.Verify(r => r.GetAccountByNumberOrIdAsync(accountStateEventData.AccountNumber), Times.Once);
        accountRepositoryMock.Verify(r => r.AddAccountAsync(accountEntity), Times.Never);
        accountRepositoryMock.Verify(r => r.UpdateAccountAsync(It.IsAny<AccountEntity>()), Times.Once);
        accountRepositoryMock.Verify(r => r.RemoveAccountAsync(accountEntity), Times.Never);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SyncAccountAsync_When_EventType_Delete_AccountExists_Returns_True()
    {
        // Arrange
        var eventType = OperationAction.Delete;
        Guid identifier = Guid.NewGuid();

        AccountStateEventData accountStateEventData = new AccountStateEventData()
        {
            AccountGlobalUniqueId = identifier,
            AccountId = 1,
            AccountNumber = "TestAccount",
            LegalName = "TestLegalName",
            SiretNumber = "TestSiretNumber",
            Status = "TestStatus",
        };

        var accountEntity = new AccountEntity()
        {
            AccountGlobalUniqueId = identifier,
            AccountId = 1,
            AccountNumber = "TestAccount",
        };

        var operationRepositoryMock = new Mock<IOperationRepository>(MockBehavior.Strict);
        var accountRepositoryMock = new Mock<IAccountRepository>(MockBehavior.Strict);

        accountRepositoryMock.Setup(s => s.GetAccountByNumberOrIdAsync(It.IsAny<string>()))
            .Callback<string>((accountNumber) =>
            {
                accountNumber.Should().NotBe(null);
                Assert.Equal(accountNumber, accountStateEventData.AccountNumber);
            })
            .ReturnsAsync(accountEntity);

        accountRepositoryMock.Setup(s => s.RemoveAccountAsync(It.IsAny<AccountEntity>()))
            .Callback<AccountEntity>((accountToRemove) =>
            {
                accountToRemove.Should().NotBeNull();
                Assert.Equal(accountEntity.AccountNumber, accountToRemove.AccountNumber);
                Assert.Equal(accountEntity.AccountGlobalUniqueId, accountToRemove.AccountGlobalUniqueId);
                Assert.Equal(accountEntity.AccountId, accountToRemove.AccountId);
            })
            .Returns(Task.CompletedTask);

        // Act
        var accountService = new AccountService(accountRepositoryMock.Object, operationRepositoryMock.Object, _logger.Object);
        var result = await accountService.SyncAcountAsync(accountStateEventData, eventType);

        // Assert
        accountRepositoryMock.VerifyAll();
        accountRepositoryMock.Verify(r => r.GetAccountByNumberOrIdAsync(accountStateEventData.AccountNumber), Times.Once);
        accountRepositoryMock.Verify(r => r.RemoveAccountAsync(It.IsAny<AccountEntity>()), Times.Once);
        accountRepositoryMock.Verify(r => r.AddAccountAsync(It.IsAny<AccountEntity>()), Times.Never);
        accountRepositoryMock.Verify(r => r.UpdateAccountAsync(It.IsAny<AccountEntity>()), Times.Never);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SyncAccountAsync_When_EventType_Delete_AccountNotExists_Returns_False()
    {
        // Arrange
        var eventType = OperationAction.Delete;

        AccountStateEventData accountStateEventData = new AccountStateEventData()
        {
            AccountGlobalUniqueId = Guid.NewGuid(),
            AccountId = 1,
            AccountNumber = "TestAccount",
            LegalName = "TestLegalName",
            SiretNumber = "TestSiretNumber",
            Status = "TestStatus",
        };

        var operationRepositoryMock = new Mock<IOperationRepository>(MockBehavior.Strict);
        var accountRepositoryMock = new Mock<IAccountRepository>(MockBehavior.Strict);

        accountRepositoryMock.Setup(s => s.GetAccountByNumberOrIdAsync(It.IsAny<string>()))
            .Callback<string>((accountNumber) =>
            {
                accountNumber.Should().NotBe(null);
                Assert.Equal(accountNumber, accountStateEventData.AccountNumber);
            })
            .ReturnsAsync((AccountEntity)null);

        // Act
        var accountService = new AccountService(accountRepositoryMock.Object, operationRepositoryMock.Object, _logger.Object);
        var result = await accountService.SyncAcountAsync(accountStateEventData, eventType);

        // Assert
        accountRepositoryMock.VerifyAll();
        accountRepositoryMock.Verify(r => r.GetAccountByNumberOrIdAsync(accountStateEventData.AccountNumber), Times.Once);
        accountRepositoryMock.Verify(r => r.RemoveAccountAsync(It.IsAny<AccountEntity>()), Times.Never);
        accountRepositoryMock.Verify(r => r.AddAccountAsync(It.IsAny<AccountEntity>()), Times.Never);
        accountRepositoryMock.Verify(r => r.UpdateAccountAsync(It.IsAny<AccountEntity>()), Times.Never);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetAccountNumberByIdAsync_ShouldReturnAccountNumber_WhenAccountExists()
    {
        // Arrange
        var accountRepositoryMock = new Mock<IAccountRepository>(MockBehavior.Strict);
        var operationRepositoryMock = new Mock<IOperationRepository>();
        var accountId = 123;
        var expectedAccountNumber = "accountnumber";

        var accountEntity = new AccountEntity
        {
            AccountId = accountId,
            AccountNumber = expectedAccountNumber
        };

        accountRepositoryMock.Setup(repo => repo.GetAccountByNumberOrIdAsync(accountId.ToString()))
            .ReturnsAsync(accountEntity);

        var accountService = new AccountService(accountRepositoryMock.Object, operationRepositoryMock.Object, _logger.Object);

        // Act
        var result = await accountService.GetAccountNumberByIdAsync(accountId);

        // Assert
        result.Should().Be(expectedAccountNumber);
        accountRepositoryMock.Verify(repo => repo.GetAccountByNumberOrIdAsync(accountId.ToString()), Times.Once);
    }

    [Fact]
    public async Task GetAccountNumberByIdAsync_ShouldReturnNull_WhenAccountDoesNotExist()
    {
        // Arrange
        var accountRepositoryMock = new Mock<IAccountRepository>(MockBehavior.Strict);
        var operationRepositoryMock = new Mock<IOperationRepository>();
        var accountId = 456;

        accountRepositoryMock.Setup(repo => repo.GetAccountByNumberOrIdAsync(accountId.ToString()))
            .ReturnsAsync((AccountEntity)null);

        var accountService = new AccountService(accountRepositoryMock.Object, operationRepositoryMock.Object, _logger.Object);

        // Act
        var result = await accountService.GetAccountNumberByIdAsync(accountId);

        // Assert
        result.Should().BeNull();
        accountRepositoryMock.Verify(repo => repo.GetAccountByNumberOrIdAsync(accountId.ToString()), Times.Once);
    }
}

// <copyright file="AccountRepositoryTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Consts;
using Domain.Entities.Accounts;
using Domain.Entities.Contacts;
using FluentAssertions;
using Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using Pulse.Registry.Domain.Context;
using Pulse.Registry.Domain.Entities;
using Registry.Application.Consts;
using OperationType = EFCore.BulkExtensions.OperationType;

namespace Registry.Infrastructure.Tests.Repository;

public class AccountRepositoryTests
{
    private readonly DbContextOptions<RefContext> _dbContextOptions;

    public AccountRepositoryTests()
    {
        _dbContextOptions = new DbContextOptionsBuilder<RefContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .Options;
    }

    [Fact]
    public async Task AddAccountAsync_Should_Add_Account()
    {
        // Arrange
        using var context = new RefContext(_dbContextOptions);
        var repository = new AccountRepository(context);
        var account = new AccountEntity
        {
            AccountId = 11,
            AccountNumber = "12345",
        };

        // Act
        await repository.AddAccountAsync(account);
        var result = await context.AccountEntities.FindAsync(account.AccountId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(account.AccountNumber, result.AccountNumber);
    }

    [Fact]
    public async Task GetAccountByNumberAsync_Using_AccountNumber_Should_Return_Account()
    {
        // Arrange
        using var context = new RefContext(_dbContextOptions);
        var repository = new AccountRepository(context);
        var account = new AccountEntity
        {
            AccountId = 2,
            AccountNumber = "MyAccount",
        };
        context.AccountEntities.Add(account);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetAccountByNumberOrIdAsync("MyAccount");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("MyAccount", result.AccountNumber);
    }

    [Fact]
    public async Task GetAccountByNumberAsync_Using_AccountId_Should_Return_Account()
    {
        // Arrange
        using var context = new RefContext(_dbContextOptions);
        var repository = new AccountRepository(context);
        var account = new AccountEntity
        {
            AccountId = 3,
            AccountNumber = "1000244200",
        };
        context.AccountEntities.Add(account);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetAccountByNumberOrIdAsync("1000244200");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("1000244200", result.AccountNumber);
    }

    [Fact]
    public async Task UpdateAccountAsync_Should_Update_Account()
    {
        // Arrange
        using var context = new RefContext(_dbContextOptions);
        var repository = new AccountRepository(context);
        var account = new AccountEntity
        {
            AccountId = 4,
            AccountNumber = "12345",
            AccountGlobalUniqueId = Guid.NewGuid(),
        };
        context.AccountEntities.Add(account);
        await context.SaveChangesAsync();

        // Act
        var guid = Guid.NewGuid();
        account.AccountGlobalUniqueId = guid;
        await repository.UpdateAccountAsync(account);

        // Assert
        var updatedAccount = await context.AccountEntities.FindAsync(account.AccountId);
        Assert.NotNull(updatedAccount);
        Assert.Equal(guid, updatedAccount.AccountGlobalUniqueId);
    }

    [Fact]
    public async Task RemoveAccountAsync_Should_Remove_Account()
    {
        // Arrange
        using var context = new RefContext(_dbContextOptions);
        var repository = new AccountRepository(context);
        var account = new AccountEntity
        {
            AccountId = 5,
            AccountNumber = "12345",
        };
        context.AccountEntities.Add(account);
        await context.SaveChangesAsync();

        // Act
        await repository.RemoveAccountAsync(account);

        // Assert
        var updatedAccount = await context.AccountEntities.FindAsync(account.AccountId);
        Assert.Null(updatedAccount);
    }

    [Fact]
    public async Task DoesAccountExistsInOperations_ShouldThrowNullIfProcessStatusArgumentsIsNull()
    {
        string accountNumber = "123456";
        string operationType = OperationName.Insert;
        string? processStatus = null;
        using (var context = new RefContext(GetDbOptions()))
        {
            var contactRepos = new AccountRepository(context);
            var action = async () => await contactRepos.DoesAccountExistInOperations(accountNumber, operationType, processStatus);
            await action.Should().ThrowAsync<ArgumentException>();
        }
    }

    [Fact]
    public async Task DoesAccountExistsInOperations_ShouldThrowNullIAccountNumberArgumentsIsNull()
    {
        string accountNumber = "";
        string operationType = OperationName.Insert;
        string? processStatus = "READY";
        using (var context = new RefContext(GetDbOptions()))
        {
            var contactRepos = new AccountRepository(context);
            var action = async () => await contactRepos.DoesAccountExistInOperations(accountNumber, operationType, processStatus);
            await action.Should().ThrowAsync<ArgumentException>();
        }
    }

    [Fact]
    public async Task DoesAccountExistsInOperations_ShouldThrowNullIfOperationTypeArgumentsIsNull()
    {
        string accountNumber = "123456";
        string operationType = "";
        string? processStatus = "READY";
        using (var context = new RefContext(GetDbOptions()))
        {
            var contactRepos = new AccountRepository(context);
            var action = async () => await contactRepos.DoesAccountExistInOperations(accountNumber, operationType, processStatus);
            await action.Should().ThrowAsync<ArgumentException>();
        }
    }

    [Fact]
    public async Task DoesAccountExistsInOperations_ShouldReturnFalseIfAccountDoesNotExists()
    {
        string accountNumber = "123456";
        string operationType = OperationName.Insert;
        string? processStatus = "READY";
        using (var context = new RefContext(GetDbOptions()))
        {
            var contactRepos = new AccountRepository(context);
            var result = await contactRepos.DoesAccountExistInOperations(accountNumber, operationType, processStatus);
            result.Should().BeFalse();
        }
    }

    [Fact]
    public async Task DoesAccountExistsInOperations_ShouldReturnTrueIfAccountDoesExists()
    {
        string accountNumber = "123456";
        string operationType = OperationName.Insert;
        string? processStatus = "READY";
        using (var context = new RefContext(GetDbOptions()))
        {
            var contactRepos = new AccountRepository(context);
            var refAccount = new RefAccountEntity()
            {
                EntityId = Guid.NewGuid(),
                AccountNumber = accountNumber,
                LegalName = "HakounaMatata",
                AccountFlagStatus = 1,
                AccountType = "Client",
                OperationType = operationType,
                OperationDate = DateTime.Now
            };
            var operation = new RegOperationEntity()
            {
                ApprovalStatus = ApprovalStatus.Approved,
                EntityId = refAccount.EntityId,
                CreationDate = DateTime.Now,
                LastStatusApprovalDate = DateTime.Now,
                LastStatusProcessedDate = DateTime.Now,
                Operation = operationType,
                ProcessStatus = ProcessStatus.Ready,
                Type = "ACCOUNT"
            };
            context.RefAccountEntity.Add(refAccount);
            context.RegOperationEntity.Add(operation);
            context.SaveChanges();

            var result = await contactRepos.DoesAccountExistInOperations(accountNumber, operationType, processStatus);
            result.Should().BeTrue();
        }
    }

    private DbContextOptions<RefContext> GetDbOptions()
    {
        return new DbContextOptionsBuilder<RefContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    #region DeepValidation
    [Fact]
    public async Task ValidationAccountOperation_InsertAccount_AlreadyExists()
    {
        // Arrange
        using var context = new RefContext(_dbContextOptions);
        var guid = Guid.NewGuid();
        var repository = new AccountRepository(context);
        var refAccount = new RefAccountEntity
        {
            EntityId = guid,
            AccountNumber = "accountNumber1",
            OperationType = OperationName.Insert
        };
        context.RefAccountEntity.Add(refAccount);
        var account = new AccountEntity
        {
            AccountId = 1231,
            AccountNumber = "accountNumber1",
            AccountGlobalUniqueId = guid,
        };
        context.AccountEntities.Add(account);
        await context.SaveChangesAsync();

        // Act
        await repository.ValidateAccountOperation();
        var audit = context.DeepValidationEntities.FirstOrDefault(x => x.EntityId == guid);

        // Assert
        Assert.NotNull(audit);
        Assert.Equal($"Operation of Type : {refAccount.OperationType} with this Account Number {refAccount.AccountNumber} already exists", audit.Reason);
    }

    [Fact]
    public async Task ValidationAccountOperation_UpdateAccount_AlreadyExists()
    {
        // Arrange
        using var context = new RefContext(_dbContextOptions);
        var guid = Guid.NewGuid();
        var repository = new AccountRepository(context);
        var refAccount = new RefAccountEntity
        {
            EntityId = guid,
            AccountNumber = "accountNumber2",
            OperationType = OperationName.Update
        };
        context.RefAccountEntity.Add(refAccount);
        var account = new AccountEntity
        {
            AccountId = 1232,
            AccountNumber = "accountNumber2",
            AccountGlobalUniqueId = guid,
        };
        context.AccountEntities.Add(account);
        await context.SaveChangesAsync();

        // Act
        await repository.ValidateAccountOperation();
        var operation = context.RegOperationEntity.FirstOrDefault(x => x.EntityId == guid);

        // Assert
        Assert.NotNull(operation);
    }

    [Fact]
    public async Task ValidationAccountOperation_InsertAccount_DontExists()
    {
        // Arrange
        using var context = new RefContext(_dbContextOptions);
        var guid = Guid.NewGuid();
        var repository = new AccountRepository(context);
        var refAccount = new RefAccountEntity
        {
            EntityId = guid,
            AccountNumber = "accountNumber3",
            OperationType = OperationName.Insert,
        };
        context.RefAccountEntity.Add(refAccount);
        await context.SaveChangesAsync();

        // Act
        await repository.ValidateAccountOperation();
        var operation = context.RegOperationEntity.FirstOrDefault(x => x.EntityId == guid);

        // Assert
        Assert.NotNull(operation);
    }

    [Fact]
    public async Task ValidationAccountOperation_InsertAccount_DontExists_OpearationExists()
    {
        // Arrange
        using var context = new RefContext(_dbContextOptions);
        var guid = Guid.NewGuid();
        var guidExists = Guid.NewGuid();
        var repository = new AccountRepository(context);
        var refAccount = new RefAccountEntity
        {
            EntityId = guid,
            AccountNumber = "accountNumber4",
            OperationType = OperationName.Insert,
        };
        context.RefAccountEntity.Add(refAccount);
        var refAccountExists = new RefAccountEntity
        {
            EntityId = guidExists,
            AccountNumber = "accountNumber4",
            OperationType = OperationName.Insert
        };
        context.RefAccountEntity.Add(refAccountExists);
        var operationInsert = new RegOperationEntity
        {
            EntityId = guidExists,
            Operation = OperationName.Insert,
            ApprovalStatus = ApprovalStatus.Approved,

        };
        context.RegOperationEntity.Add(operationInsert);
        await context.SaveChangesAsync();

        // Act
        await repository.ValidateAccountOperation();
        var audit = context.DeepValidationEntities.FirstOrDefault(x => x.EntityId == guid);

        // Assert
        Assert.NotNull(audit);
        Assert.Equal($"Operation of Type : {refAccount.OperationType} with this Account Number {refAccount.AccountNumber} already exists", audit.Reason);
    }

    [Fact]
    public async Task ValidationAccountOperation_UpdateAccount_DontExists_OpearationDoesntExists()
    {
        // Arrange
        using var context = new RefContext(_dbContextOptions);
        var guid = Guid.NewGuid();
        var repository = new AccountRepository(context);
        var refAccount = new RefAccountEntity
        {
            EntityId = guid,
            AccountNumber = "accountNumber5",
            OperationType = OperationName.Update
        };
        context.RefAccountEntity.Add(refAccount);
        await context.SaveChangesAsync();

        // Act
        await repository.ValidateAccountOperation();
        var audit = context.DeepValidationEntities.FirstOrDefault(x => x.EntityId == guid);

        // Assert
        Assert.NotNull(audit);
        Assert.Equal($"Operation of Type : {refAccount.OperationType} with this Account Number {refAccount.AccountNumber} account does not exists", audit.Reason);
    }

    [Fact]
    public async Task ValidationAccountOperation_UpdateAccount_DontExists_OpearationExists()
    {
        // Arrange
        using var context = new RefContext(_dbContextOptions);
        var guid = Guid.NewGuid();
        var repository = new AccountRepository(context);
        var refAccount = new RefAccountEntity
        {
            EntityId = guid,
            AccountNumber = "accountNumber6",
            OperationType = OperationName.Update
        };
        context.RefAccountEntity.Add(refAccount);
        var operationUpdate = new RegOperationEntity
        {
            EntityId = guid,
            Operation = OperationName.Insert,
            ApprovalStatus = ApprovalStatus.Approved,

        };
        context.RegOperationEntity.Add(operationUpdate);
        await context.SaveChangesAsync();

        // Act
        await repository.ValidateAccountOperation();
        var operation = context.RegOperationEntity.FirstOrDefault(x => x.EntityId == guid);

        // Assert
        Assert.NotNull(operation);
    }

    [Fact]
    public async Task ValidationAccountOperation_DeleteAccount_Exists_RoleDuplicatorGreaterThan1()
    {
        // Arrange
        using var context = new RefContext(_dbContextOptions);
        var guid = Guid.NewGuid();
        var repository = new AccountRepository(context);
        var refAccount = new RefAccountEntity
        {
            EntityId = guid,
            AccountNumber = "accountNumber12",
            OperationType = OperationName.Delete,
        };
        context.RefAccountEntity.Add(refAccount);
        var account = new AccountEntity
        {
            AccountId = 1112,
            AccountNumber = "accountNumber12",
            AccountGlobalUniqueId = guid,
        };
        context.AccountEntities.Add(account);
        var contact = new ContactEntity
        {
            ContactId = 1112,
            Email = "test@test.com",
            Type = "Customer",
        };
        context.ContactEntities.Add(contact);
        var role = new RoleEntity
        {
            AccountId = 1112,
            ContactId = 1112,
            AccountGlobalUniqueId = guid,
            RoleDuplicatesCounter = 2,
            AccountNumber = "accountNumber12"
        };
        context.RoleEntities.Add(role);
        await context.SaveChangesAsync();

        // Act
        await repository.ValidateAccountOperation();
        var operationDeletedAccount = context.RegOperationEntity.FirstOrDefault(x => x.EntityId == guid 
                                                    && x.Operation == OperationName.Delete
                                                    && x.Type == "ACCOUNT");
        var roleDeleted = context.RoleEntities.FirstOrDefault(r => r.AccountGlobalUniqueId == guid);

        // Assert
        Assert.NotNull(operationDeletedAccount);
        Assert.NotNull(roleDeleted);
        Assert.Equal(0, roleDeleted.RoleDuplicatesCounter);
    }

    [Fact]
    public async Task ValidationAccountOperation_DeleteAccount_DontExists_RoleDuplicatorGreaterThan1()
    {
        // Arrange
        using var context = new RefContext(_dbContextOptions);
        var guid = Guid.NewGuid();
        var repository = new AccountRepository(context);
        var refAccount = new RefAccountEntity
        {
            EntityId = guid,
            AccountNumber = "accountNumber7",
            OperationType = OperationName.Delete,
        };
        context.RefAccountEntity.Add(refAccount);
        var account = new AccountEntity
        {
            AccountId = 1111,
            AccountNumber = "accountNumber7",
            AccountGlobalUniqueId = guid,
        };
        context.AccountEntities.Add(account);
        var contact = new ContactEntity
        {
            ContactId = 1111,
            Email = "test@test.com",
            Type = "Customer",
        };
        context.ContactEntities.Add(contact);
        var role = new RoleEntity
        {
            AccountId = 1111,
            ContactId = 1111,
            AccountGlobalUniqueId = guid,
            RoleDuplicatesCounter = 2,
            AccountNumber = "accountNumber7"
        };
        context.RoleEntities.Add(role);
        await context.SaveChangesAsync();

        // Act
        await repository.ValidateAccountOperation();
        var operation = context.RegOperationEntity.FirstOrDefault(x => x.EntityId == guid);
        var roleDeleted = context.RoleEntities.FirstOrDefault(r => r.AccountGlobalUniqueId == guid);

        // Assert
        Assert.NotNull(operation);
        Assert.NotNull(roleDeleted);
        Assert.Equal(0 , roleDeleted.RoleDuplicatesCounter);
    }

    [Fact]
    public async Task ValidationAccountOperation_DeleteAccount_DontExists_RoleDuplicatorZero()
    {
        // Arrange
        using var context = new RefContext(_dbContextOptions);
        var guid = Guid.NewGuid();
        var repository = new AccountRepository(context);
        var refAccount = new RefAccountEntity
        {
            EntityId = guid,
            AccountNumber = "accountNumber8",
            OperationType = OperationName.Delete
        };
        context.RefAccountEntity.Add(refAccount);
        var account = new AccountEntity
        {
            AccountId = 1,
            AccountNumber = "accountNumber8",
            AccountGlobalUniqueId = guid,
        };
        context.AccountEntities.Add(account);
        var contact = new ContactEntity
        {
            ContactId = 1,
            Email = "test@test.com",
            Type = "Customer",
        };
        context.ContactEntities.Add(contact);
        var role = new RoleEntity
        {
            AccountId = 1,
            ContactId = 1,
            AccountGlobalUniqueId = guid,
            RoleDuplicatesCounter = 0
        };
        context.RoleEntities.Add(role);
        var operationDelete = new RegOperationEntity
        {
            EntityId = guid,
            Operation = OperationName.Delete,
            ApprovalStatus = ApprovalStatus.Approved,
            Type = "Account"
        };
        context.RegOperationEntity.Add(operationDelete);
        await context.SaveChangesAsync();

        // Act
        await repository.ValidateAccountOperation();
        var operation = context.RegOperationEntity.FirstOrDefault(x => x.EntityId == guid);
        var roleDeleted = context.RoleEntities.FirstOrDefault(r => r.AccountGlobalUniqueId == guid);

        // Assert
        Assert.NotNull(operation);
        Assert.NotNull(roleDeleted);
        Assert.Equal(0, roleDeleted.RoleDuplicatesCounter);
    }
    #endregion
}

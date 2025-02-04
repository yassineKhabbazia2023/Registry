// <copyright file="AccountRepositoryTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;
using AutoFixture;
using Domain.Entities.Accounts;
using Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using Pulse.ContactRegistry.Domain.Context;

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
            AccountId = 1,
            AccountNumber = "12345",
            LegalName = "Test Legal",
            Siret = "SIRET123",
            Status = "Active",
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
            LegalName = "Test Legal",
            Siret = "SIRET123",
            Status = "Active",
            IsActive = true
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
            LegalName = "Test Legal",
            Siret = "SIRET123",
            Status = "Active",
            IsActive = true
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
            LegalName = "Test Legal",
            Siret = "SIRET123",
            Status = "Active",
        };
        context.AccountEntities.Add(account);
        await context.SaveChangesAsync();

        // Act
        account.Status = "Inactive";
        await repository.UpdateAccountAsync(account);

        // Assert
        var updatedAccount = await context.AccountEntities.FindAsync(account.AccountId);
        Assert.NotNull(updatedAccount);
        Assert.Equal("Inactive", updatedAccount.Status);
    }

    [Fact]
    public async Task RemoveAccountAsync_Should_Set_Account_Inactive()
    {
        // Arrange
        using var context = new RefContext(_dbContextOptions);
        var repository = new AccountRepository(context);
        var account = new AccountEntity
        {
            AccountId = 5,
            AccountNumber = "12345",
            LegalName = "Test Legal",
            Siret = "SIRET123",
            Status = "Active",
            IsActive = true
        };
        context.AccountEntities.Add(account);
        await context.SaveChangesAsync();

        // Act
        await repository.RemoveAccountAsync(account);

        // Assert
        var updatedAccount = await context.AccountEntities.FindAsync(account.AccountId);
        Assert.NotNull(updatedAccount);
        Assert.False(updatedAccount.IsActive);
    }
}

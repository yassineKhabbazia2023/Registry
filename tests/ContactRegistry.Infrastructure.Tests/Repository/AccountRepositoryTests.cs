// <copyright file="AccountRepositoryTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;
using AutoFixture;
using Domain.Entities;
using Infrastructure.Context;
using Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using Pulse.ContactRegistry.Infrastructure.Context;

namespace ContactRegistry.Infrastructure.Tests.Repository;

public class AccountRepositoryTests
{
    private readonly Fixture _fixture;
    private readonly DbContextOptions<ApplicationDbContext> _dbContextOptions;
    private readonly DbContextOptions<RefContext> _refContextOptions;

    public AccountRepositoryTests()
    {
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList().ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        _dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;
        _refContextOptions = new DbContextOptionsBuilder<RefContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;
    }

    [Fact]
    public async Task AddAccountsAsync_ShouldInsertAccountsInDatabase()
    {
        using (var context = new ApplicationDbContext(_dbContextOptions))
        {
            context.Database.OpenConnection();
            context.Database.EnsureCreated();

            var accounts = _fixture.CreateMany<AlxAccount>(2);

            var repository = new AccountRepository(context, null!);

            await repository.AddAccountsAsync(accounts);

            var result = await context.AlxAccounts.CountAsync();

            Assert.Equal(2, result);
        }
    }

    [Fact]
    public async Task AddAccountsAsync_ShouldInsertRefAccountsInDatabase()
    {
        using (var context = new RefContext(_refContextOptions))
        {
            context.Database.OpenConnection();
            context.Database.EnsureCreated();

            var accounts = _fixture.Build<RefAccountCsv>()
                .With(r => r.AccountInsertedDate, DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"))
                .With(r => r.AccountUpdatedDate, DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"))
                .With(r => r.DeploymentDate, DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"))
                .CreateMany(2);

            var repository = new AccountRepository(null!, context);

            await repository.AddAccountsAsync(accounts);

            var result = await context.RefAccountEntity.CountAsync();

            Assert.Equal(2, result);
        }
    }
}

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

    public AccountRepositoryTests()
    {
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList().ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public async Task AddAccountsAsync_ShouldInsertRefAccountsInDatabase()
    {
        var options = new DbContextOptionsBuilder<RefContext>()
        .UseSqlite("Filename=:memory:")
        .Options;

        using (var context = new RefContext(options))
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

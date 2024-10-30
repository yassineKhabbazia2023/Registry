// <copyright file="ContactRepositoryTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;
using AutoFixture;
using Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using Pulse.ContactRegistry.Infrastructure.Context;

namespace ContactRegistry.Infrastructure.Tests.Repository;

public class ContactRepositoryTests
{
    private readonly Fixture _fixture;

    public ContactRepositoryTests()
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

            var contacts = _fixture.CreateMany<RefContactCsv>(2);

            var repository = new ContactRepository(null!, context);

            await repository.AddContactsAsync(contacts);

            var result = await context.RefContactEntity.CountAsync();

            Assert.Equal(2, result);
        }
    }
}

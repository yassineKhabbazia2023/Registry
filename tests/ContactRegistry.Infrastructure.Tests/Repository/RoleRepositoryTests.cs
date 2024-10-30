// <copyright file="RoleRepositoryTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;
using AutoFixture;
using Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using Pulse.ContactRegistry.Infrastructure.Context;

namespace ContactRegistry.Infrastructure.Tests.Repository;

public class RoleRepositoryTests
{
    private readonly Fixture _fixture;

    public RoleRepositoryTests()
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

            var roles = _fixture.CreateMany<RefRoleCsv>(2);

            var repository = new RoleRepository(null!, context);

            await repository.AddRolesAsync(roles);

            var result = await context.RefRoleEntity.CountAsync();

            Assert.Equal(2, result);
        }
    }
}

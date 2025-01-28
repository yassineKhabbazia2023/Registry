// <copyright file="AccountRepositoryTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;
using AutoFixture;
using Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using Pulse.ContactRegistry.Domain.Context;

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

}

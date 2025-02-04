// <copyright file="RoleRepositoryTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;
using AutoFixture;
using Microsoft.EntityFrameworkCore;
using Pulse.ContactRegistry.Domain.Context;

namespace Registry.Infrastructure.Tests.Repository;

public class RoleRepositoryTests
{
    private readonly Fixture _fixture;

    public RoleRepositoryTests()
    {
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList().ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }
}

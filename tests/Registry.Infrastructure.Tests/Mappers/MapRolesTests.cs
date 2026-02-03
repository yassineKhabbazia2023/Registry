// <copyright file="MapRolesTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;
using AutoFixture;
using Application.Mappers;

namespace Registry.Infrastructure.Tests.Mappers;

public class MapRolesTests
{
    private readonly Fixture _fixture;

    public MapRolesTests()
    {
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList().ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public void MapRoleCsvToRoleEntity_ShouldMapsCorrectly()
    {
        var role = _fixture.Create<RefRoleCsv>();

        var result = role.MapRoleCsvToRoleEntity();

        Assert.NotNull(result);
        Assert.Equal(role.RoleFlagStatus, result.RoleFlagStatus);
        Assert.Equal(role.ContactEmail, result.ContactEmail);
        Assert.Equal(role.AccountNumber, result.AccountNumber);
        Assert.Equal(role.Description, result.Description);
        Assert.Equal(role.Operation, result.OperationType);
        Assert.Equal(role.ContactFlagPortailFactures, result.ContactFlagPortailFactures);
    }

    [Fact]
    public void MapRoleCsvToRoleEntity_WithNullSource_ShouldReturnNull()
    {
        var result = MapRoles.MapRoleCsvToRoleEntity(null!);

        Assert.Null(result);
    }

    [Fact]
    public void MapRoleCsvsToRoleEntities_ShouldMapsCorrectly()
    {
        var roles = _fixture.CreateMany<RefRoleCsv>(2);

        var result = roles.MapRoleCsvsToRoleEntities();

        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Equal(roles.Count(), result.Count());
    }

    [Fact]
    public void MapRoleCsvsToRoleEntities_WithNullSource_ShouldReturnEmptyList()
    {
        var result = MapRoles.MapRoleCsvsToRoleEntities(null!);

        Assert.Empty(result);
    }
}

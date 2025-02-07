// <copyright file="RoleServiceTest.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Models;
using Application.Services;
using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Registry.Application.Tests.Services;

public class RoleServiceTest
{
    private readonly Fixture _fixture;

    public RoleServiceTest()
    {
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList().ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public async Task InsertRolesAsync_Should_Be_Success()
    {
        var repository = new Mock<IRoleRepository>();
        var factory = Mock.Of<IRoleDeepValidatorFactory>();
        var roleService = new RoleService(null!, repository.Object,factory);

        await roleService.InsertRolesAsync(It.IsAny<IEnumerable<RefRoleCsv>>());

        repository.Verify(x => x.AddRolesAsync(It.IsAny<IEnumerable<RefRoleCsv>>()), Times.Once);
    }


    [Theory]
    [InlineData(1, 1)]
    [InlineData(2000, 1)]
    public async Task AddRoleAsync_Adds_Role_With2000Roles(int roleCsvLenght, int functionTimeCalled)
    {
        // Arrange
        var roles = _fixture.Build<RefRoleCsv>()
            .CreateMany(roleCsvLenght);

        var roleRepository = new Mock<IRoleRepository>();
        roleRepository.Setup(r => r.AddRolesAsync(It.IsAny<IEnumerable<RefRoleCsv>>())).
            Callback<IEnumerable<RefRoleCsv>>(data =>
            {
                data.Count().Should().BeGreaterThanOrEqualTo(roles.Count());
            })
            .Returns(Task.CompletedTask);


        var loggerMock = new Mock<ILogger<RoleService>>(MockBehavior.Default);

        // Act
        var factory = Mock.Of<IRoleDeepValidatorFactory>();
        var roleService = new RoleService(null!, roleRepository.Object, factory);
        await roleService.InsertRolesAsync(roles);

        roleRepository.VerifyAll();
        roleRepository.Verify(a => a.AddRolesAsync(It.IsAny<IEnumerable<RefRoleCsv>>()), Times.AtLeast(functionTimeCalled));
    }
}

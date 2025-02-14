// <copyright file="RoleServiceTest.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Consts;
using Application.Interfaces;
using Application.Interfaces.RuleValidators;
using Application.Models;
using Application.Services;
using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Pulse.Registry.Domain.Entities;

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
        var roleService = new RoleService(null!, repository.Object, factory);

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

    [Fact]
    public async Task CreateValidRolesOperationsAsync_When_OperationType_Insert()
    {
        // Arrange
        var refRole = _fixture.Create<RefRoleEntity>();
        refRole.OperationType = OperationName.Insert;

        var roles = new List<RefRoleEntity>() { refRole };

        var roleRepositoryMock = new Mock<IRoleRepository>(MockBehavior.Loose);
        roleRepositoryMock.Setup(r => r.GetDeepValidationFailedRoles())
            .ReturnsAsync(roles);

        var roleDeepValidatorMock = new Mock<IRoleDeepValidator>();
        var sequence = new MockSequence();
        roleDeepValidatorMock.InSequence(sequence)
            .Setup(v => v.Instantiate(It.IsAny<RefRoleEntity>()))
            .Callback<RefRoleEntity>((role) =>
            {
                Assert.Equal(role, refRole);
            }).ReturnsAsync(roleDeepValidatorMock.Object).Verifiable();
        roleDeepValidatorMock.InSequence(sequence)
            .Setup(v => v.ContactShouldExistInPulseOrOperations())
            .ReturnsAsync(roleDeepValidatorMock.Object);
        roleDeepValidatorMock.InSequence(sequence)
            .Setup(v => v.AccountShouldExistInPulseOrOperations())
            .ReturnsAsync(roleDeepValidatorMock.Object);
        roleDeepValidatorMock.InSequence(sequence)
            .Setup(v => v.RoleShouldShouldNotExistInPulse())
            .ReturnsAsync(roleDeepValidatorMock.Object);
        roleDeepValidatorMock.InSequence(sequence)
            .Setup(v => v.TryAddOperation())
            .ReturnsAsync(roleDeepValidatorMock.Object);
        roleDeepValidatorMock.InSequence(sequence)
            .Setup(v => v.Validate())
            .ReturnsAsync(true);

        var loggerMock = new Mock<ILogger<RoleService>>();
        var roleDeepValidatorFactoryMock = new Mock<IRoleDeepValidatorFactory>();
        roleDeepValidatorFactoryMock.Setup(rf => rf.Create())
            .Returns(roleDeepValidatorMock.Object);

        // Act
        var roleService = new RoleService(loggerMock.Object, roleRepositoryMock.Object, roleDeepValidatorFactoryMock.Object);
        var result = await roleService.ReviewFailedRolesOperationsAsync();

        // Assert
        roleDeepValidatorMock.Verify();
        result.Should().Contain(true);
    }

    [Fact]
    public async Task CreateValidRolesOperationsAsync_When_OperationType_Delete()
    {
        // Arrange
        var refRole = _fixture.Create<RefRoleEntity>();
        refRole.OperationType = OperationName.Delete;

        var roles = new List<RefRoleEntity>() { refRole };

        var roleRepositoryMock = new Mock<IRoleRepository>(MockBehavior.Loose);
        roleRepositoryMock.Setup(r => r.GetDeepValidationFailedRoles())
            .ReturnsAsync(roles);

        var roleDeepValidatorMock = new Mock<IRoleDeepValidator>();
        var sequence = new MockSequence();
        roleDeepValidatorMock.InSequence(sequence)
            .Setup(v => v.Instantiate(It.IsAny<RefRoleEntity>()))
            .Callback<RefRoleEntity>((role) =>
            {
                Assert.Equal(role, refRole);
            }).ReturnsAsync(roleDeepValidatorMock.Object).Verifiable();
        roleDeepValidatorMock.InSequence(sequence)
            .Setup(v => v.ContactShouldExistInPulse())
            .ReturnsAsync(roleDeepValidatorMock.Object);
        roleDeepValidatorMock.InSequence(sequence)
            .Setup(v => v.AccountShouldExistInPulse())
            .ReturnsAsync(roleDeepValidatorMock.Object);
        roleDeepValidatorMock.InSequence(sequence)
            .Setup(v => v.RoleShouldExistInPulse())
            .ReturnsAsync(roleDeepValidatorMock.Object);
        roleDeepValidatorMock.InSequence(sequence)
            .Setup(v => v.TryAddOperation())
            .ReturnsAsync(roleDeepValidatorMock.Object);
        roleDeepValidatorMock.InSequence(sequence)
            .Setup(v => v.Validate())
            .ReturnsAsync(true);

        var loggerMock = new Mock<ILogger<RoleService>>();
        var roleDeepValidatorFactoryMock = new Mock<IRoleDeepValidatorFactory>();
        roleDeepValidatorFactoryMock.Setup(rf => rf.Create())
            .Returns(roleDeepValidatorMock.Object);

        // Act
        var roleService = new RoleService(loggerMock.Object, roleRepositoryMock.Object, roleDeepValidatorFactoryMock.Object);
        var result = await roleService.ReviewFailedRolesOperationsAsync();

        // Assert
        roleDeepValidatorMock.Verify();
        result.Should().Contain(true);
    }
}

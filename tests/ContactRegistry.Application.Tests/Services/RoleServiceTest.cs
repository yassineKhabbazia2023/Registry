// <copyright file="RoleServiceTest.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Models;
using Application.Services;
using AutoFixture;
using Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using CreRole = Application.Models.CreRole;

namespace ContactRegistry.Application.Tests.Services;

public class RoleServiceTest
{
    private readonly Fixture _fixture;

    public RoleServiceTest()
    {
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList().ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2000, 1)]
    public async Task ProcessRoleAsyncAsync_Adds_Role(int roleCsvLenght, int functionTimeCalled)
    {
        // Arrange
        var roles = _fixture.CreateMany<RoleCsv>(roleCsvLenght);

        var expectedRoles = roles
            .Select(
            a => new AlxRole
            {
                RoleId = a.RoleId,
                AccountId = a.AccountId,
                ContactId = a.ContactId,
                Onboarded = a.Onboarded,
                IsFavorite = a.IsFavorite,
                RoleSignatory = a.RoleSignatory,
                RoleDelegataireEmail = a.RoleDelegataireEmail
            }).ToList();

        var roleRepository = new Mock<IRoleRepository>(MockBehavior.Strict);
        roleRepository.Setup(r => r.AddRolesAsync(It.IsAny<IEnumerable<AlxRole>>())).
            Callback<IEnumerable<AlxRole>>(data =>
            {
                data.Should().BeEquivalentTo(expectedRoles);
            })
            .Returns(Task.CompletedTask);
        roleRepository.Setup(r => r.GetCountRolesActifAsync())
            .ReturnsAsync((22, 44));
        var processDeltaTriggerRepositoryMock = new Mock<IProcessDeltaTriggerRepository>(MockBehavior.Strict);
        processDeltaTriggerRepositoryMock.Setup(p => p.UpdateRoleProcessAsync(true)).Returns(Task.CompletedTask);
        var loggerMock = new Mock<ILogger<RoleService>>(MockBehavior.Default);

        // Act
        var roleService = new RoleService(loggerMock.Object, roleRepository.Object, processDeltaTriggerRepositoryMock.Object);
        await roleService.ProcessRoleAsync(roles);

        roleRepository.VerifyAll();
        roleRepository.Verify(a => a.AddRolesAsync(It.IsAny<List<AlxRole>>()), Times.AtLeast(functionTimeCalled));
    }

    [Fact]
    public async Task StreamRolesJsonAsync_Writes_ExpectedData()
    {
        // Arrange
        var role1 = new Domain.Entities.CreRole
        {
            RoleId = Guid.Empty,
            AccountId = Guid.Empty,
            ContactId = Guid.Empty,
            Deleted = default
        };

        var expectedRole = new CreRole
        {
            ContactId = Guid.Empty,
            AccountId = Guid.Empty,
            Deleted = null,
            RoleId = Guid.Empty,
        };

        IEnumerable<Domain.Entities.CreRole> roles = new List<Domain.Entities.CreRole>() { role1 };
        IEnumerable<CreRole> expectedRoleList = new List<CreRole>() { expectedRole };

        var options = new JsonSerializerOptions { WriteIndented = true };
        var expectedJsonData = JsonSerializer.Serialize(expectedRoleList, options);

        var roleRepository = new Mock<IRoleRepository>();
        roleRepository.Setup(r => r.GetRolesAsync()).Returns(GetAsyncEnumerable(roles));

        roleRepository.Setup(r => r.GetCountRolesActifAsync())
            .ReturnsAsync((22, 44));

        var processDeltaTriggerRepositoryMock = new Mock<IProcessDeltaTriggerRepository>(MockBehavior.Strict);

        var stream = new MemoryStream();
        var streamWriter = new StreamWriter(stream);
        var loggerMock = new Mock<ILogger<RoleService>>(MockBehavior.Default);

        var roleService = new RoleService(loggerMock.Object, roleRepository.Object, processDeltaTriggerRepositoryMock.Object);

        // Act
        await roleService.StreamRolesJsonAsync(streamWriter);

        stream.Position = 0;
        var reader = new StreamReader(stream);
        var jsonData = await reader.ReadToEndAsync();

        // Assert
        roleRepository.Verify(c => c.GetRolesAsync(), Times.Once);
        jsonData.Should().BeEquivalentTo(expectedJsonData);
    }

    [Fact]
    public async Task ClearAlxAsync_Should_Be_Success()
    {
        var roleRepository = new Mock<IRoleRepository>();
        roleRepository.Setup(a => a.ClearAlxAsync()).Returns(Task.CompletedTask);

        var processDeltaTriggerRepositoryMock = new Mock<IProcessDeltaTriggerRepository>(MockBehavior.Strict);
        var loggerMock = new Mock<ILogger<RoleService>>(MockBehavior.Default);

        var roleService = new RoleService(loggerMock.Object, roleRepository.Object, processDeltaTriggerRepositoryMock.Object);

        await roleService.ClearAlxAsync();

        roleRepository.Verify(a => a.ClearAlxAsync(), Times.Once);
    }

    private async IAsyncEnumerable<Domain.Entities.CreRole> GetAsyncEnumerable(IEnumerable<Domain.Entities.CreRole> roles)
    {
        foreach (var role in roles)
        {
            yield return role;
        }
    }

    [Fact]
    public async Task InsertRolesAsync_Should_Be_Success()
    {
        var repository = new Mock<IRoleRepository>();
        var roleService = new RoleService(null!, repository.Object, null!);

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

        var processDeltaTriggerRepositoryMock = new Mock<IProcessDeltaTriggerRepository>();

        var loggerMock = new Mock<ILogger<RoleService>>(MockBehavior.Default);

        // Act
        var roleService = new RoleService(loggerMock.Object, roleRepository.Object, processDeltaTriggerRepositoryMock.Object);
        await roleService.InsertRolesAsync(roles);

        roleRepository.VerifyAll();
        roleRepository.Verify(a => a.AddRolesAsync(It.IsAny<IEnumerable<RefRoleCsv>>()), Times.AtLeast(functionTimeCalled));
    }
}

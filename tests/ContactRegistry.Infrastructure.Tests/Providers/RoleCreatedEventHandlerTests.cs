// <copyright file="RoleCreatedEventHandlerTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;
using Infrastructure.Providers;
using Moq;
using Application.Interfaces;
using Application.Models;

namespace Infrastructure.Tests.Providers;

public class RoleCreatedEventHandlerTests
{
    private readonly Mock<IRoleRegistryService> _roleRegistryService = new(MockBehavior.Strict);

    [Fact]
    public async Task HandleAsync_WithValidMessage_ShouldCreateRole()
    {
        // Arrange
        var doesRoleExist = false;

        var loggerMock = new Mock<ILogger<RoleCreatedEventHandler>>();

        _roleRegistryService.Setup(a => a.DoesRoleExistAsync(It.IsAny<RoleRegistry>()))
        .ReturnsAsync(doesRoleExist)
            .Verifiable();

        _roleRegistryService.Setup(a => a.CreateRoleAsync(It.IsAny<RoleRegistry>()))
        .Returns(Task.CompletedTask)
            .Verifiable();

        _roleRegistryService.Setup(a => a.UpdateRoleAsync(It.IsAny<RoleRegistry>()))
        .Returns(Task.CompletedTask)
            .Verifiable();

        var handler = new RoleCreatedEventHandler(loggerMock.Object, _roleRegistryService.Object);
        var message = "{\"EventType\":\"RoleCreatedEvent\",\"Data\":{\"ContactId\":123,\"AccountId\":22,\"ContactEmail\":\"email@test.fr\",\"AccountId\":\"199099090\",\"IsSignatory\":1,\"IsFavorite\":1,\"IsDelegation\":1,}}";

        // Act
        await handler.HandleAsync(message);

        // Assert
        _roleRegistryService.Verify(repo => repo.DoesRoleExistAsync(It.IsAny<RoleRegistry>()), Times.Once);
        _roleRegistryService.Verify(repo => repo.CreateRoleAsync(It.IsAny<RoleRegistry>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithValidMessage_ShouldUpdateRole()
    {
        // Arrange
        var doesRoleExist = true;

        var loggerMock = new Mock<ILogger<RoleCreatedEventHandler>>();

        _roleRegistryService.Setup(a => a.DoesRoleExistAsync(It.IsAny<RoleRegistry>()))
        .ReturnsAsync(doesRoleExist)
            .Verifiable();

        _roleRegistryService.Setup(a => a.CreateRoleAsync(It.IsAny<RoleRegistry>()))
        .Returns(Task.CompletedTask)
            .Verifiable();

        _roleRegistryService.Setup(a => a.UpdateRoleAsync(It.IsAny<RoleRegistry>()))
        .Returns(Task.CompletedTask)
            .Verifiable();


        var handler = new RoleCreatedEventHandler(loggerMock.Object, _roleRegistryService.Object);
        var message = "{\"EventType\":\"RoleCreatedEvent\",\"Data\":{\"ContactId\":123,\"AccountId\":22,\"ContactEmail\":\"email@test.fr\",\"AccountId\":\"199099090\",\"IsSignatory\":1,\"IsFavorite\":1,\"IsDelegation\":1,}}";

        // Act
        await handler.HandleAsync(message);

        // Assert
        _roleRegistryService.Verify(repo => repo.DoesRoleExistAsync(It.IsAny<RoleRegistry>()), Times.Once);
        _roleRegistryService.Verify(repo => repo.UpdateRoleAsync(It.IsAny<RoleRegistry>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithNullMessage_ShouldNotCreateRole()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<RoleCreatedEventHandler>>();

        _roleRegistryService.Setup(a => a.UpdateRoleAsync(It.IsAny<RoleRegistry>()))
        .Returns(Task.CompletedTask)
            .Verifiable();


        var handler = new RoleCreatedEventHandler(loggerMock.Object, _roleRegistryService.Object);

        // Act
        await handler.HandleAsync(null!);

        // Assert
        _roleRegistryService.Verify(repo => repo.CreateRoleAsync(It.IsAny<RoleRegistry>()), Times.Never);
    }
}

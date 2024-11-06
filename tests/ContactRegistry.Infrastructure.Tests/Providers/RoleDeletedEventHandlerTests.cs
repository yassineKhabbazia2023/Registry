// <copyright file="RoleDeletedEventHandlerTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;
using Infrastructure.Providers;
using Moq;
using Application.Interfaces;
using Application.Models;

namespace Infrastructure.Tests.Providers;

public class RoleDeletedEventHandlerTests
{
    private readonly Mock<IRoleRegistryService> _roleRegistryService = new(MockBehavior.Strict);

    [Fact]
    public async Task HandleAsync_WithValidMessage_ShouldDeleteRole()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<RoleDeletedEventHandler>>();

        _roleRegistryService.Setup(a => a.UpdateRoleAsync(It.IsAny<RoleRegistry>()))
        .Returns(Task.CompletedTask)
            .Verifiable();

        var handler = new RoleDeletedEventHandler(loggerMock.Object, _roleRegistryService.Object);
        var message = "{\"EventType\":\"RoleDeletedEvent\",\"Data\":{\"ContactId\":123,\"AccountId\":22,\"ContactEmail\":\"email@test.fr\",\"AccountId\":\"199099090\",\"IsSignatory\":1,\"IsFavorite\":1,\"IsDelegation\":1,}}";

        // Act
        await handler.HandleAsync(message);

        // Assert
        _roleRegistryService.Verify(repo => repo.UpdateRoleAsync(It.IsAny<RoleRegistry>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithNullMessage_ShouldNotDeleteRole()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<RoleDeletedEventHandler>>();

        _roleRegistryService.Setup(a => a.UpdateRoleAsync(It.IsAny<RoleRegistry>()))
        .Returns(Task.CompletedTask)
            .Verifiable();


        var handler = new RoleDeletedEventHandler(loggerMock.Object, _roleRegistryService.Object);

        // Act
        await handler.HandleAsync(null!);

        // Assert
        _roleRegistryService.Verify(repo => repo.UpdateRoleAsync(It.IsAny<RoleRegistry>()), Times.Never);
    }
}

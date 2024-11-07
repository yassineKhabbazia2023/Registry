// <copyright file="RoleCreatedEventHandlerTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;
using Infrastructure.Providers;
using Moq;
using Application.Interfaces;
using Application.Models;
using System.Net;

namespace Infrastructure.Tests.Providers;

public class RoleCreatedEventHandlerTests
{
    private readonly Mock<IRoleRegistryProvider> _roleRegistryProvider = new(MockBehavior.Strict);

    [Fact]
    public async Task HandleAsync_WithValidMessage_ShouldCreateRole()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<RoleCreatedEventHandler>>();
        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK);

        _roleRegistryProvider.Setup(a => a.CreateRoleAsync(It.IsAny<RoleRegistry>()))
            .ReturnsAsync(responseMessage)
            .Verifiable();

        var handler = new RoleCreatedEventHandler(loggerMock.Object, _roleRegistryProvider.Object);
        var message = "{\"EventType\":\"RoleCreatedEvent\",\"Data\":{\"ContactId\":123,\"AccountId\":22,\"ContactEmail\":\"email@test.fr\",\"AccountId\":\"199099090\",\"IsSignatory\":1,\"IsFavorite\":1,\"IsDelegation\":1,}}";

        // Act
        await handler.HandleAsync(message);

        // Assert
        _roleRegistryProvider.Verify(repo => repo.CreateRoleAsync(It.IsAny<RoleRegistry>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithNullMessage_ShouldNotCreateRole()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<RoleCreatedEventHandler>>();

        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK);

        _roleRegistryProvider.Setup(a => a.CreateRoleAsync(It.IsAny<RoleRegistry>()))
            .ReturnsAsync(responseMessage)
            .Verifiable();


        var handler = new RoleCreatedEventHandler(loggerMock.Object, _roleRegistryProvider.Object);

        // Act
        await handler.HandleAsync(null!);

        // Assert
        _roleRegistryProvider.Verify(repo => repo.CreateRoleAsync(It.IsAny<RoleRegistry>()), Times.Never);
    }
}

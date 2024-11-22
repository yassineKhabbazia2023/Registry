// <copyright file="RoleCreatedEventHandlerTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;
using Infrastructure.Providers;
using Moq;
using Application.Interfaces;
using Application.Models;
using System.Net;
using Pulse.Back.Events.IntegrationEvents;
using AutoFixture;
using Newtonsoft.Json;
using Infrastructure.Mappers;

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

    [Fact]
    public async Task HandleAsync_IfCreateRoleAsyncFailed_ShouldLogError()
    {
        var logger = new Mock<ILogger<RoleCreatedEventHandler>>();
        var roleRegistryProvider = new Mock<IRoleRegistryProvider>();
        var fixture = new Fixture();

        RoleCreatedEvent roleCreatedEvent = fixture.Create<RoleCreatedEvent>();
        RoleRegistry roleRegistry = roleCreatedEvent.Data.RoleEventCreatedDataToModel();
        HttpResponseMessage responseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest);
        var roleCreatedEventHandler = new RoleCreatedEventHandler(logger.Object, roleRegistryProvider.Object);

        roleRegistryProvider.Setup(x => x.CreateRoleAsync(It.IsAny<RoleRegistry>()))
            .ReturnsAsync(responseMessage);


        await roleCreatedEventHandler.HandleAsync(JsonConvert.SerializeObject(roleCreatedEvent));

        logger.Verify(x => x.Log(
       LogLevel.Error,
       It.IsAny<EventId>(),
       It.IsAny<It.IsAnyType>(),
       It.IsAny<Exception>(),
       (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
       Times.Once);

    }

}

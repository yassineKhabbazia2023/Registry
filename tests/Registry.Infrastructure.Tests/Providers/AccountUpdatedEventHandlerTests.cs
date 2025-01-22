// <copyright file="AccountUpdatedEventHandlerTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;
using Infrastructure.Providers;
using Moq;
using Application.Interfaces;
using Application.Models;
using System.Net;

namespace Infrastructure.Tests.Providers;

public class AccountUpdatedEventHandlerTests
{
    private readonly Mock<IAccountRegistryProvider> _accountRegistryProvider = new(MockBehavior.Strict);

    [Fact]
    public async Task HandleAsync_WithValidMessage_ShouldUpdateDeployment()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<AccountUpdatedEventHandler>>();

        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK);

        _accountRegistryProvider.Setup(a => a.UpdateDeploymentAsync(It.IsAny<DeploymentPlanningRegistry>()))
            .ReturnsAsync(responseMessage)
            .Verifiable();

        var handler = new AccountUpdatedEventHandler(loggerMock.Object, _accountRegistryProvider.Object);
        var message = "{\"EventType\":\"AccountCreatedEvent\",\"Data\":{\"AccountId\":123,\"LegalName\":\"John Doe\",\"AccountNumber\":\"accountnumber\",\"Status\":\"ToDeploy\"}}";

        // Act
        await handler.HandleAsync(message);

        // Assert
        _accountRegistryProvider.Verify(repo => repo.UpdateDeploymentAsync(It.IsAny<DeploymentPlanningRegistry>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithNullMessage_ShouldNotUpdateDeployment()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<AccountUpdatedEventHandler>>();

        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK);

        _accountRegistryProvider.Setup(a => a.UpdateDeploymentAsync(It.IsAny<DeploymentPlanningRegistry>()))
            .ReturnsAsync(responseMessage)
            .Verifiable();

        var handler = new AccountUpdatedEventHandler(loggerMock.Object, _accountRegistryProvider.Object);

        // Act
        await handler.HandleAsync(null!);

        // Assert
        _accountRegistryProvider.Verify(repo => repo.UpdateDeploymentAsync(It.IsAny<DeploymentPlanningRegistry>()), Times.Never);
    }
}

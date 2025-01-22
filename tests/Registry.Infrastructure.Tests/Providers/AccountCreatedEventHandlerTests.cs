// <copyright file="AccountCreatedEventHandlerTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;
using Infrastructure.Providers;
using Moq;
using Application.Interfaces;
using Application.Models;
using System.Net;

namespace Infrastructure.Tests.Providers;

public class AccountCreatedEventHandlerTests
{
    private readonly Mock<IAccountRegistryProvider> _accountRegistryProvider = new(MockBehavior.Strict);

    [Fact]
    public async Task HandleAsync_WithValidMessage_ShouldCreateDeployment()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<AccountCreatedEventHandler>>();
        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK);

        _accountRegistryProvider.Setup(a => a.CreateDeploymentAsync(It.IsAny<DeploymentPlanningRegistry>()))
            .ReturnsAsync(responseMessage)
            .Verifiable();

        var handler = new AccountCreatedEventHandler(loggerMock.Object, _accountRegistryProvider.Object);
        var message = "{\"EventType\":\"AccountCreatedEvent\",\"Data\":{\"AccountId\":123,\"LegalName\":\"John Doe\",\"AccountNumber\":\"accountnumber\",\"Status\":\"ToDeploy\"}}";

        // Act
        await handler.HandleAsync(message);

        // Assert
        _accountRegistryProvider.Verify(repo => repo.CreateDeploymentAsync(It.IsAny<DeploymentPlanningRegistry>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithNullMessage_ShouldNotCreateDeployment()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<AccountCreatedEventHandler>>();

        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK);

        _accountRegistryProvider.Setup(a => a.CreateDeploymentAsync(It.IsAny<DeploymentPlanningRegistry>()))
            .ReturnsAsync(responseMessage)
            .Verifiable();


        var handler = new AccountCreatedEventHandler(loggerMock.Object, _accountRegistryProvider.Object);

        // Act
        await handler.HandleAsync(null!);

        // Assert
        _accountRegistryProvider.Verify(repo => repo.CreateDeploymentAsync(It.IsAny<DeploymentPlanningRegistry>()), Times.Never);
    }
}

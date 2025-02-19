// <copyright file="AccountCreatedEventHandlerTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;
using Infrastructure.Providers;
using Moq;
using Application.Interfaces;
using Application.Models;
using System.Net;
using Pulse.Back.Events.IntegrationEvents.EventsData;
using Application.Consts;

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

        var accountServiceMock = new Mock<IAccountService>(MockBehavior.Strict);
        accountServiceMock.Setup(x => x.SyncAcountAsync(It.IsAny<AccountStateEventData>(), It.IsAny<string>()))
            .ReturnsAsync(false);
        accountServiceMock.Setup(x => x.UpdateAccountProcessStatusAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        var handler = new AccountCreatedEventHandler(loggerMock.Object, _accountRegistryProvider.Object, accountServiceMock.Object);
        var message = "{\"EventType\":\"AccountCreatedEvent\",\"Data\":{\"AccountId\":123,\"LegalName\":\"John Doe\",\"AccountNumber\":\"accountnumber\",\"Status\":\"ToDeploy\"}}";

        // Act
        await handler.HandleAsync(message);

        // Assert
        _accountRegistryProvider.Verify(repo => repo.CreateDeploymentAsync(It.IsAny<DeploymentPlanningRegistry>()), Times.Never);
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

        var accountServiceMock = new Mock<IAccountService>(MockBehavior.Strict);
        accountServiceMock.Setup(x => x.SyncAcountAsync(It.IsAny<AccountStateEventData>(), It.IsAny<string>()))
            .ReturnsAsync(false);
        accountServiceMock.Setup(x => x.UpdateAccountProcessStatusAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        var handler = new AccountCreatedEventHandler(loggerMock.Object, _accountRegistryProvider.Object, accountServiceMock.Object);

        // Act
        await handler.HandleAsync(null!);

        // Assert
        _accountRegistryProvider.Verify(repo => repo.CreateDeploymentAsync(It.IsAny<DeploymentPlanningRegistry>()), Times.Never);
    }

    [Fact]
    public async Task TriggerSyncAndUpdateProcessStatusStep_ShouldCallSyncAndUpdate()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<AccountCreatedEventHandler>>();
        var accountServiceMock = new Mock<IAccountService>(MockBehavior.Strict);

        var testData = new AccountStateEventData { AccountNumber = "accountnumber" };
        var eventType = "AccountCreatedEvent";
        
        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK);

        _accountRegistryProvider.Setup(a => a.CreateDeploymentAsync(It.IsAny<DeploymentPlanningRegistry>()))
              .ReturnsAsync(responseMessage)
              .Verifiable();

        accountServiceMock.Setup(x => x.SyncAcountAsync(It.IsAny<AccountStateEventData>(), It.IsAny<string>()))
            .Callback<AccountStateEventData, string>((data, operation) =>
            {
                Assert.Equal("accountnumber", data.AccountNumber);
                Assert.Equal(OperationName.Insert, operation);
            })
            .ReturnsAsync(true);

        accountServiceMock.Setup(x => x.UpdateAccountProcessStatusAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string>((accountNumber, operation) =>
            {
                Assert.Equal("accountnumber", accountNumber);
                Assert.Equal(OperationName.Insert, operation);
            })
            .Returns(Task.CompletedTask);

        var handler = new AccountCreatedEventHandler(loggerMock.Object, _accountRegistryProvider.Object, accountServiceMock.Object);

        // Act
        var message = "{\"EventType\":\"AccountCreatedEvent\",\"Data\":{\"AccountId\":123,\"LegalName\":\"John Doe\",\"AccountNumber\":\"accountnumber\",\"Status\":\"ToDeploy\"}}";
        await handler.HandleAsync(message);

        // Assert
        accountServiceMock.VerifyAll();
        accountServiceMock.Verify(x => x.SyncAcountAsync(It.IsAny<AccountStateEventData>(), It.IsAny<string>()), Times.Once);
        accountServiceMock.Verify(x => x.UpdateAccountProcessStatusAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithNullMessage_ShouldNotTriggerSyncAndUpdate()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<AccountCreatedEventHandler>>();
        var accountServiceMock = new Mock<IAccountService>(MockBehavior.Strict);

        var handler = new AccountCreatedEventHandler(loggerMock.Object, _accountRegistryProvider.Object, accountServiceMock.Object);

        // Act
        await handler.HandleAsync(null!);

        // Assert
        accountServiceMock.VerifyAll();
        accountServiceMock.Verify(x => x.SyncAcountAsync(It.IsAny<AccountStateEventData>(), It.IsAny<string>()), Times.Never);
        accountServiceMock.Verify(x => x.UpdateAccountProcessStatusAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}

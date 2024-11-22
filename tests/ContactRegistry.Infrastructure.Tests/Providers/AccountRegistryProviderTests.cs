// <copyright file="AccountRegistryProviderTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Moq;
using Application.Models;
using System.Net;
using Moq.Protected;
using Infrastructure.Providers;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Tests.Providers;

public class AccountRegistryProviderTests
{
    [Fact]
    public async Task PostAsync_WithValidMessage_ShouldCreateDeployment()
    {
        // Arrange
        var demploymentMock = new DeploymentPlanningRegistry()
        {
            AccountNumber = "accountnumber",
            DeploymentDate = DateTime.Now,
            DeploymentStatus = "deploy"
        };

        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);

        var configuration = new Mock<IConfiguration>(MockBehavior.Strict);
        configuration.SetupGet(acc => acc["RegistryApiUrl"])
            .Returns("https://test/");

        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponseMessage);

        var httpClient = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://test/")
        };

        var clientFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        clientFactory.Setup(acc => acc.CreateClient("RegistryApi")).Returns(httpClient);

        var provider = new AccountRegistryProvider(clientFactory.Object);

        //Act
        var response = await provider.CreateDeploymentAsync(demploymentMock);

        //Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        clientFactory.VerifyAll();
    }

    [Fact]
    public async Task PutAsync_WithValidMessage_ShouldUpdateDeployment()
    {
        // Arrange
        var demploymentMock = new DeploymentPlanningRegistry()
        {
            AccountNumber = "accountnumber",
            DeploymentDate= DateTime.Now,
            DeploymentStatus = "deploy"
        };

        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);

        var configuration = new Mock<IConfiguration>(MockBehavior.Strict);
        configuration.SetupGet(acc => acc["RegistryApiUrl"])
            .Returns("https://test/");

        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponseMessage);

        var httpClient = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://test/")
        };

        var clientFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        clientFactory.Setup(acc => acc.CreateClient("RegistryApi")).Returns(httpClient);

        var provider = new AccountRegistryProvider(clientFactory.Object);

        //Act
        var response = await provider.UpdateDeploymentAsync(demploymentMock);

        //Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        clientFactory.VerifyAll();
    }
}

using Application.Interfaces;
using Application.Models.Results;
using Application.Requests;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Registry.WebApi.Controllers;

namespace Registry.WebApi.Tests.Controllers;

public class HubSpotControllerTests
{
    [Fact]
    public async Task SubmitIntegrationAsync_Should_ReturnOk()
    {
        // Arrange
        var request = new HubSpotSubmissionInputRequest
        {
            DematerializationEmail = "facturation@test.fr",
            FirstName = "test"
        };

        var serviceMock = new Mock<IHubSpotService>();
        var accountNumber = "ACC-2025-001847";
        serviceMock
            .Setup(s => s.SubmitIntegrationAsync(accountNumber, request))
            .ReturnsAsync(new HubSpotSubmissionResult { IsSuccess = true, StatusCode = 200 });

        var controller = new HubSpotController(serviceMock.Object);
        var currentUserId = 123;

        // Act
        var result = await controller.SubmitIntegrationAsync(currentUserId, accountNumber, request);

        // Assert
        Assert.IsType<OkResult>(result);
        serviceMock.Verify(s => s.SubmitIntegrationAsync(accountNumber, request), Times.Once);
    }

    [Fact]
    public async Task SubmitIntegrationAsync_WhenProviderFails_ReturnsBadRequest()
    {
        // Arrange
        var request = new HubSpotSubmissionInputRequest
        {
            DematerializationEmail = "facturation@test.fr",
            FirstName = "test"
        };

        var serviceMock = new Mock<IHubSpotService>();
        var accountNumber = "ACC-2025-001847";
        serviceMock
            .Setup(s => s.SubmitIntegrationAsync(accountNumber, request))
            .ReturnsAsync(new HubSpotSubmissionResult { IsSuccess = false, StatusCode = 400, ErrorMessage = "Bad request" });

        var controller = new HubSpotController(serviceMock.Object);
        var currentUserId = 123;

        // Act
        var result = await controller.SubmitIntegrationAsync(currentUserId, accountNumber, request);

        // Assert
        var badRequest = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
        Assert.Equal("Bad request", badRequest.Value);
    }
}

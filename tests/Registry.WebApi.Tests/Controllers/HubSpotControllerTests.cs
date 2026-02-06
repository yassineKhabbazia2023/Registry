using Application.Interfaces;
using Application.Models.Results;
using Application.Requests;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Registry.WebApi.Controllers;

namespace Registry.WebApi.Tests.Controllers;

public class HubSpotControllerTests
{
    [Fact]
    public async Task SubmitIntegrationAsync_Should_ReturnCreated()
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
            .ReturnsAsync(HubSpotFormSubmissionResult.Created(true));

        var controller = new HubSpotController(serviceMock.Object);
        var currentUserId = 123;

        // Act
        var result = await controller.SubmitIntegrationAsync(currentUserId, accountNumber, request);

        // Assert
        var statusResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status201Created, statusResult.StatusCode);
        serviceMock.Verify(s => s.SubmitIntegrationAsync(accountNumber, request), Times.Once);
    }

    [Fact]
    public async Task SubmitIntegrationAsync_WhenDuplicate_ReturnsUnprocessableEntity()
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
            .ReturnsAsync(HubSpotFormSubmissionResult.Rejected());

        var controller = new HubSpotController(serviceMock.Object);
        var currentUserId = 123;

        // Act
        var result = await controller.SubmitIntegrationAsync(currentUserId, accountNumber, request);

        // Assert
        var unprocessable = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, unprocessable.StatusCode);
    }

    [Fact]
    public async Task SubmitIntegrationAsync_WhenServiceThrows_ReturnsUnprocessableEntity()
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
            .ThrowsAsync(new ArgumentException("invalid"));

        var controller = new HubSpotController(serviceMock.Object);
        var currentUserId = 123;

        // Act
        var result = await controller.SubmitIntegrationAsync(currentUserId, accountNumber, request);

        // Assert
        var unprocessable = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, unprocessable.StatusCode);
    }

    [Fact]
    public async Task GetSubmissionStateAsync_WhenFound_ReturnsOk()
    {
        // Arrange
        var serviceMock = new Mock<IHubSpotService>();
        var accountNumber = "ACC-2025-001847";
        serviceMock
            .Setup(s => s.GetSubmissionStateAsync(accountNumber))
            .ReturnsAsync(HubSpotSubmissionStateResult.Found());

        var controller = new HubSpotController(serviceMock.Object);

        // Act
        var result = await controller.GetSubmissionStateAsync(accountNumber);

        // Assert
        var statusResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status200OK, statusResult.StatusCode);
    }

    [Fact]
    public async Task GetSubmissionStateAsync_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var serviceMock = new Mock<IHubSpotService>();
        var accountNumber = "ACC-2025-001847";
        serviceMock
            .Setup(s => s.GetSubmissionStateAsync(accountNumber))
            .ReturnsAsync(HubSpotSubmissionStateResult.NotFound());

        var controller = new HubSpotController(serviceMock.Object);

        // Act
        var result = await controller.GetSubmissionStateAsync(accountNumber);

        // Assert
        var statusResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, statusResult.StatusCode);
    }

    [Fact]
    public async Task GetSubmissionStateAsync_WhenServiceThrows_ReturnsNotFound()
    {
        // Arrange
        var serviceMock = new Mock<IHubSpotService>();
        var accountNumber = "ACC-2025-001847";
        serviceMock
            .Setup(s => s.GetSubmissionStateAsync(accountNumber))
            .ThrowsAsync(new ArgumentException("invalid"));

        var controller = new HubSpotController(serviceMock.Object);

        // Act
        var result = await controller.GetSubmissionStateAsync(accountNumber);

        // Assert
        var statusResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, statusResult.StatusCode);
    }
}

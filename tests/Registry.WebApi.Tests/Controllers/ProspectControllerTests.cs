using Application.Exceptions;
using Application.Interfaces;
using Kpmg.ExceptionMiddleware.AdvancedException;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Registry.WebApi.Controllers;

namespace Registry.WebApi.Tests.Controllers;

public class ProspectControllerTests
{
    #region CheckEligibilityAsync

    [Fact]
    public async Task CheckEligibilityAsync_WhenCustomerAlreadyExists_ReturnsOk()
    {
        // Arrange
        const string siret = "91772785100011";
        var serviceMock = new Mock<IProspectEligibilityService>();
        serviceMock
            .Setup(service => service.CheckEligibilityAsync(siret))
            .ReturnsAsync(false);

        var controller = new ProspectController(serviceMock.Object);

        // Act
        var result = await controller.CheckEligibilityAsync(siret);

        // Assert
        var statusResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status200OK, statusResult.StatusCode);
    }

    [Fact]
    public async Task CheckEligibilityAsync_WhenCustomerDoesNotExist_ReturnsAccepted()
    {
        // Arrange
        const string siret = "73282932000074";
        var serviceMock = new Mock<IProspectEligibilityService>();
        serviceMock
            .Setup(service => service.CheckEligibilityAsync(siret))
            .ReturnsAsync(true);

        var controller = new ProspectController(serviceMock.Object);

        // Act
        var result = await controller.CheckEligibilityAsync(siret);

        // Assert
        var statusResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status202Accepted, statusResult.StatusCode);
    }

    [Fact]
    public async Task CheckEligibilityAsync_WhenSiretIsInvalid_ThrowsBadRequestException()
    {
        // Arrange
        const string invalidSiret = "123";
        var serviceMock = new Mock<IProspectEligibilityService>();
        serviceMock
            .Setup(service => service.CheckEligibilityAsync(invalidSiret))
            .ThrowsAsync(new BadRequestException("REG004", "invalid siret"));

        var controller = new ProspectController(serviceMock.Object);

        // Act
        var result = await Assert.ThrowsAsync<BadRequestException>(() => controller.CheckEligibilityAsync(invalidSiret));

        // Assert
        Assert.Equal("REG004", result.Code);
        Assert.Equal("invalid siret", result.Message);
    }

    [Fact]
    public async Task CheckEligibilityAsync_WhenAkuiteoSearchFails_ReturnsBadRequest()
    {
        // Arrange
        const string siret = "72200393604516";
        var serviceMock = new Mock<IProspectEligibilityService>();
        serviceMock
            .Setup(service => service.CheckEligibilityAsync(siret))
            .ThrowsAsync(new AkuiteoAccountSearchTechnicalException("Akuiteo is unavailable."));

        var controller = new ProspectController(serviceMock.Object);

        // Act
        var result = await controller.CheckEligibilityAsync(siret);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    #endregion
}

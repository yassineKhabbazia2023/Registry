using Application.Exceptions;
using Application.Interfaces;
using Application.Models.Results;
using Application.Requests;
using Kpmg.ExceptionMiddleware.AdvancedException;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Registry.WebApi.Controllers;

namespace Registry.WebApi.Tests.Controllers;

/// <summary>
/// Tests for <see cref="AkuiteoController"/>.
/// </summary>
public class AkuiteoControllerTests
{
    #region CreateCustomerAsync

    /// <summary>
    /// Ensures the controller returns 201 with the created account number.
    /// </summary>
    [Fact]
    public async Task CreateCustomerAsync_ShouldReturnCreated()
    {
        // Arrange
        var request = CreateRequest();
        var response = new AkuiteoCustomerCreationResponse
        {
            AccountNumber = "9010001713"
        };

        var serviceMock = new Mock<IAkuiteoCustomerService>();
        var contactServiceMock = new Mock<IAkuiteoContactService>();
        serviceMock
            .Setup(service => service.CreateCustomerAsync(request))
            .ReturnsAsync(response);

        var controller = new AkuiteoController(serviceMock.Object, contactServiceMock.Object);

        // Act
        var result = await controller.CreateCustomerAsync(request);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);
        var payload = Assert.IsType<AkuiteoCustomerCreationResponse>(objectResult.Value);
        Assert.Equal("9010001713", payload.AccountNumber);
        serviceMock.Verify(service => service.CreateCustomerAsync(request), Times.Once);
    }

    /// <summary>
    /// Ensures the controller returns 201 with the created contact identifier.
    /// </summary>
    [Fact]
    public async Task CreateContactAsync_ShouldReturnCreated()
    {
        // Arrange
        var request = CreateContactRequest();
        var response = new AkuiteoContactCreationResponse
        {
            ContactId = "500145940"
        };

        var serviceMock = new Mock<IAkuiteoContactService>();
        var customerServiceMock = new Mock<IAkuiteoCustomerService>();
        serviceMock
            .Setup(service => service.CreateContactAsync(request))
            .ReturnsAsync(response);

        var controller = new AkuiteoController(customerServiceMock.Object, serviceMock.Object);

        // Act
        var result = await controller.CreateContactAsync(request);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);
        var payload = Assert.IsType<AkuiteoContactCreationResponse>(objectResult.Value);
        Assert.Equal("500145940", payload.ContactId);
        serviceMock.Verify(service => service.CreateContactAsync(request), Times.Once);
    }

    /// <summary>
    /// Ensures the controller translates technical Akuiteo failures to 409.
    /// </summary>
    [Fact]
    public async Task CreateCustomerAsync_WhenServiceThrowsConflict_ShouldReturnConflict()
    {
        // Arrange
        var request = CreateRequest();

        var serviceMock = new Mock<IAkuiteoCustomerService>();
        var contactServiceMock = new Mock<IAkuiteoContactService>();
        serviceMock
            .Setup(service => service.CreateCustomerAsync(request))
            .ThrowsAsync(new AkuiteoCustomerCreationTechnicalException("technical failure"));

        var controller = new AkuiteoController(serviceMock.Object, contactServiceMock.Object);

        // Act
        var result = await controller.CreateCustomerAsync(request);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, objectResult.StatusCode);
        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(StatusCodes.Status409Conflict, problemDetails.Status);
        Assert.Equal("technical failure", problemDetails.Title);
    }

    /// <summary>
    /// Ensures the controller translates technical Akuiteo contact failures to 409.
    /// </summary>
    [Fact]
    public async Task CreateContactAsync_WhenServiceThrowsConflict_ShouldReturnConflict()
    {
        // Arrange
        var request = CreateContactRequest();

        var serviceMock = new Mock<IAkuiteoContactService>();
        var customerServiceMock = new Mock<IAkuiteoCustomerService>();
        serviceMock
            .Setup(service => service.CreateContactAsync(request))
            .ThrowsAsync(new AkuiteoContactCreationTechnicalException("technical failure"));

        var controller = new AkuiteoController(customerServiceMock.Object, serviceMock.Object);

        // Act
        var result = await controller.CreateContactAsync(request);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, objectResult.StatusCode);
        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(StatusCodes.Status409Conflict, problemDetails.Status);
        Assert.Equal("technical failure", problemDetails.Title);
    }

    /// <summary>
    /// Ensures the controller translates invalid Akuiteo payload failures to 400.
    /// </summary>
    [Fact]
    public async Task CreateCustomerAsync_WhenServiceThrowsBadRequest_ShouldReturnBadRequest()
    {
        // Arrange
        var request = CreateRequest();

        var serviceMock = new Mock<IAkuiteoCustomerService>();
        var contactServiceMock = new Mock<IAkuiteoContactService>();
        serviceMock
            .Setup(service => service.CreateCustomerAsync(request))
            .ThrowsAsync(new BadRequestException(Errors.InvalidContactId, "invalid contact"));

        var controller = new AkuiteoController(serviceMock.Object, contactServiceMock.Object);

        // Act
        var result = await controller.CreateCustomerAsync(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    /// <summary>
    /// Ensures the controller translates invalid Akuiteo contact payload failures to 400.
    /// </summary>
    [Fact]
    public async Task CreateContactAsync_WhenServiceThrowsBadRequest_ShouldReturnBadRequest()
    {
        // Arrange
        var request = CreateContactRequest();

        var serviceMock = new Mock<IAkuiteoContactService>();
        var customerServiceMock = new Mock<IAkuiteoCustomerService>();
        serviceMock
            .Setup(service => service.CreateContactAsync(request))
            .ThrowsAsync(new BadRequestException(Errors.InvalidContactId, "invalid contact"));

        var controller = new AkuiteoController(customerServiceMock.Object, serviceMock.Object);

        // Act
        var result = await controller.CreateContactAsync(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    #endregion

    /// <summary>
    /// Creates a valid Akuiteo request.
    /// </summary>
    /// <returns>A valid request instance.</returns>
    private static AkuiteoCustomerCreationRequest CreateRequest()
    {
        return new AkuiteoCustomerCreationRequest
        {
            LegalName = "Boulangerie du coin",
            Siret = "72200393604516",
            Siren = "722003936",
            LegalStructure = "PersonneMorale",
            LegalForm = "SAS",
            NafCode = "10.71C",
            Address = "221B Baker Street",
            ZipCode = "69002",
            City = "Lyon",
            DepartmentCode = "69",
            RegionCode = "84",
            CountryCode = "FR",
            CaseManagerContactId = 10,
            AccountManagerContactId = 20
        };
    }

    /// <summary>
    /// Creates a valid Akuiteo contact request.
    /// </summary>
    /// <returns>A valid contact request instance.</returns>
    private static AkuiteoContactCreationRequest CreateContactRequest()
    {
        return new AkuiteoContactCreationRequest
        {
            AccountNumber = "9000120123",
            Title = "M",
            LastName = "Jean",
            FirstName = "Dupont",
            JobTitle = "Gérant",
            ContactDepartment = "Direction",
            CompanyRole = "Président",
            ContactTypes = new AkuiteoContactTypesRequest
            {
                IsDigitalVaultContact = true,
                IsDebtCollectionContact = false,
                IsMandateSignatory = true
            },
            Email = "o.dbira@boulangerie.fr",
            MobilePhone = "06 12 34 56 78"
        };
    }
}

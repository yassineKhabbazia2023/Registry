using Application.Exceptions;
using Application.Interfaces;
using Application.Models;
using Application.Models.Contacts;
using Application.Models.Results;
using Application.Requests;
using Application.Services;
using Kpmg.ExceptionMiddleware.AdvancedException;
using Microsoft.Extensions.Logging;
using Moq;

namespace Registry.Application.Tests.Services;

/// <summary>
/// Tests for <see cref="AkuiteoCustomerService"/>.
/// </summary>
public class AkuiteoCustomerServiceTests
{
    #region Fields

    private readonly Mock<IAkuiteoCustomerProvider> akuiteoCustomerProviderMock;
    private readonly Mock<IContactRepository> contactRepositoryMock;
    private readonly Mock<ILogger<AkuiteoCustomerService>> loggerMock;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="AkuiteoCustomerServiceTests"/> class.
    /// </summary>
    public AkuiteoCustomerServiceTests()
    {
        akuiteoCustomerProviderMock = new Mock<IAkuiteoCustomerProvider>();
        contactRepositoryMock = new Mock<IContactRepository>();
        loggerMock = new Mock<ILogger<AkuiteoCustomerService>>();
    }

    #region CreateCustomerAsync

    /// <summary>
    /// Ensures the real mode maps the payload and returns the created account number.
    /// </summary>
    [Fact]
    public async Task CreateCustomerAsync_WhenRealModeSucceeds_ShouldReturnCreatedAccountNumber()
    {
        // Arrange
        AkuiteoCreateCustomerRequest? capturedRequest = null;
        var service = CreateService();
        var request = CreateRequest();

        SetupContact(request.CaseManagerContactId!.Value, "case.manager@rydge.fr");
        SetupContact(request.AccountManagerContactId!.Value, "account.manager@rydge.fr");

        akuiteoCustomerProviderMock
            .Setup(provider => provider.CreateCustomerAsync(It.IsAny<AkuiteoCreateCustomerRequest>()))
            .Callback<AkuiteoCreateCustomerRequest>(payload => capturedRequest = payload)
            .ReturnsAsync(new AkuiteoCustomerCreationProviderResult
            {
                IsSuccess = true,
                StatusCode = 201,
                AccountNumber = "9010001713"
            });

        // Act
        var result = await service.CreateCustomerAsync(request);

        // Assert
        Assert.Equal("9010001713", result.AccountNumber);
        Assert.NotNull(capturedRequest);
        Assert.Equal(request.LegalName, capturedRequest!.LegalName);
        Assert.Equal(request.Siren, capturedRequest.Siren);
        Assert.Equal(request.Siret, capturedRequest.Siret);
        Assert.Equal(request.LegalStructure, capturedRequest.LegalStructure);
        Assert.Equal(request.LegalForm, capturedRequest.LegalFormCode);
        Assert.Equal(request.NafCode, capturedRequest.NafCode);
        Assert.Equal(request.Address, capturedRequest.Address.Line1);
        Assert.Equal(request.ZipCode, capturedRequest.Address.ZipCode);
        Assert.Equal(request.City, capturedRequest.Address.City);
        Assert.Equal(request.DepartmentCode, capturedRequest.Address.DepartmentCode);
        Assert.Equal(request.RegionCode, capturedRequest.Address.RegionCode);
        Assert.Equal(request.CountryCode, capturedRequest.Address.CountryCode);
        Assert.Equal("case.manager@rydge.fr", capturedRequest.CaseManagerEmail);
        Assert.Equal("account.manager@rydge.fr", capturedRequest.AccountManagerEmail);
        contactRepositoryMock.Verify(repository => repository.GetContactByEmailOrIdAsync(null, request.CaseManagerContactId), Times.Once);
        contactRepositoryMock.Verify(repository => repository.GetContactByEmailOrIdAsync(null, request.AccountManagerContactId), Times.Once);
    }

    /// <summary>
    /// Ensures downstream technical failures are translated to a conflict exception.
    /// </summary>
    [Fact]
    public async Task CreateCustomerAsync_WhenProviderReturnsFailure_ShouldThrowTechnicalException()
    {
        // Arrange
        var service = CreateService();
        var request = CreateRequest();
        SetupContact(request.CaseManagerContactId!.Value, "case.manager@rydge.fr");
        SetupContact(request.AccountManagerContactId!.Value, "account.manager@rydge.fr");

        akuiteoCustomerProviderMock
            .Setup(provider => provider.CreateCustomerAsync(It.IsAny<AkuiteoCreateCustomerRequest>()))
            .ReturnsAsync(new AkuiteoCustomerCreationProviderResult
            {
                IsSuccess = false,
                StatusCode = 500,
                ErrorMessage = "downstream error"
            });

        // Act
        var exception = await Assert.ThrowsAsync<AkuiteoCustomerCreationTechnicalException>(() => service.CreateCustomerAsync(request));

        // Assert
        Assert.Equal("downstream error", exception.Message);
    }

    /// <summary>
    /// Ensures an empty downstream account number is treated as a technical conflict.
    /// </summary>
    [Fact]
    public async Task CreateCustomerAsync_WhenProviderReturnsNoAccountNumber_ShouldThrowTechnicalException()
    {
        // Arrange
        var service = CreateService();
        var request = CreateRequest();
        SetupContact(request.CaseManagerContactId!.Value, "case.manager@rydge.fr");
        SetupContact(request.AccountManagerContactId!.Value, "account.manager@rydge.fr");

        akuiteoCustomerProviderMock
            .Setup(provider => provider.CreateCustomerAsync(It.IsAny<AkuiteoCreateCustomerRequest>()))
            .ReturnsAsync(new AkuiteoCustomerCreationProviderResult
            {
                IsSuccess = true,
                StatusCode = 201,
                AccountNumber = string.Empty
            });

        // Act & Assert
        await Assert.ThrowsAsync<AkuiteoCustomerCreationTechnicalException>(() => service.CreateCustomerAsync(request));
    }

    /// <summary>
    /// Ensures <see cref="HttpRequestException"/> is wrapped as a conflict exception.
    /// </summary>
    [Fact]
    public async Task CreateCustomerAsync_WhenProviderThrowsHttpRequestException_ShouldThrowTechnicalException()
    {
        // Arrange
        var service = CreateService();
        var request = CreateRequest();
        SetupContact(request.CaseManagerContactId!.Value, "case.manager@rydge.fr");
        SetupContact(request.AccountManagerContactId!.Value, "account.manager@rydge.fr");

        akuiteoCustomerProviderMock
            .Setup(provider => provider.CreateCustomerAsync(It.IsAny<AkuiteoCreateCustomerRequest>()))
            .ThrowsAsync(new HttpRequestException("network"));

        // Act & Assert
        await Assert.ThrowsAsync<AkuiteoCustomerCreationTechnicalException>(() => service.CreateCustomerAsync(request));
    }

    /// <summary>
    /// Ensures <see cref="TaskCanceledException"/> is wrapped as a conflict exception.
    /// </summary>
    [Fact]
    public async Task CreateCustomerAsync_WhenProviderThrowsTaskCanceledException_ShouldThrowTechnicalException()
    {
        // Arrange
        var service = CreateService();
        var request = CreateRequest();
        SetupContact(request.CaseManagerContactId!.Value, "case.manager@rydge.fr");
        SetupContact(request.AccountManagerContactId!.Value, "account.manager@rydge.fr");

        akuiteoCustomerProviderMock
            .Setup(provider => provider.CreateCustomerAsync(It.IsAny<AkuiteoCreateCustomerRequest>()))
            .ThrowsAsync(new TaskCanceledException("timeout"));

        // Act & Assert
        await Assert.ThrowsAsync<AkuiteoCustomerCreationTechnicalException>(() => service.CreateCustomerAsync(request));
    }

    /// <summary>
    /// Ensures token retrieval failures are translated to a customer technical exception.
    /// </summary>
    [Fact]
    public async Task CreateCustomerAsync_WhenTokenRetrievalFails_ShouldThrowTechnicalException()
    {
        // Arrange
        var service = CreateService();
        var request = CreateRequest();
        SetupContact(request.CaseManagerContactId!.Value, "case.manager@rydge.fr");
        SetupContact(request.AccountManagerContactId!.Value, "account.manager@rydge.fr");

        akuiteoCustomerProviderMock
            .Setup(provider => provider.CreateCustomerAsync(It.IsAny<AkuiteoCreateCustomerRequest>()))
            .ThrowsAsync(new AkuiteoAuthenticationTechnicalException("Unable to retrieve the Microsoft token for Akuiteo."));

        // Act
        var exception = await Assert.ThrowsAsync<AkuiteoCustomerCreationTechnicalException>(() => service.CreateCustomerAsync(request));

        // Assert
        Assert.Equal("Unable to retrieve the Microsoft token for Akuiteo.", exception.Message);
    }

    /// <summary>
    /// Ensures an unknown manager contact id is treated as an invalid payload.
    /// </summary>
    [Fact]
    public async Task CreateCustomerAsync_WhenManagerContactIsMissing_ShouldThrowBadRequestException()
    {
        // Arrange
        var service = CreateService();
        var request = CreateRequest();

        contactRepositoryMock
            .Setup(repository => repository.GetContactByEmailOrIdAsync(null, request.CaseManagerContactId))
            .ReturnsAsync((Contact?)null);

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => service.CreateCustomerAsync(request));
        akuiteoCustomerProviderMock.Verify(
            provider => provider.CreateCustomerAsync(It.IsAny<AkuiteoCreateCustomerRequest>()),
            Times.Never);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Creates the service under test.
    /// </summary>
    /// <returns>The configured service.</returns>
    private AkuiteoCustomerService CreateService()
    {
        return new AkuiteoCustomerService(
            akuiteoCustomerProviderMock.Object,
            contactRepositoryMock.Object,
            loggerMock.Object);
    }

    /// <summary>
    /// Configures a resolved contact email.
    /// </summary>
    /// <param name="contactId">The contact identifier.</param>
    /// <param name="email">The contact email.</param>
    private void SetupContact(int contactId, string email)
    {
        contactRepositoryMock
            .Setup(repository => repository.GetContactByEmailOrIdAsync(null, contactId))
            .ReturnsAsync(new Contact
            {
                ContactId = contactId,
                Email = email
            });
    }

    /// <summary>
    /// Creates a valid Registry request.
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

    #endregion
}

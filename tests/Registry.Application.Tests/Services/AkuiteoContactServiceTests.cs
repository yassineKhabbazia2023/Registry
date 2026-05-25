using Application.Exceptions;
using Application.Interfaces;
using Application.Models;
using Application.Models.Results;
using Application.Requests;
using Application.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Registry.Application.Tests.Services;

/// <summary>
/// Tests for <see cref="AkuiteoContactService"/>.
/// </summary>
public class AkuiteoContactServiceTests
{
    #region Fields

    private readonly Mock<IAkuiteoContactProvider> akuiteoContactProviderMock;
    private readonly Mock<ILogger<AkuiteoContactService>> loggerMock;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="AkuiteoContactServiceTests"/> class.
    /// </summary>
    public AkuiteoContactServiceTests()
    {
        akuiteoContactProviderMock = new Mock<IAkuiteoContactProvider>();
        loggerMock = new Mock<ILogger<AkuiteoContactService>>();
    }

    #region CreateContactAsync

    /// <summary>
    /// Ensures the real mode maps the payload and returns the created contact identifier.
    /// </summary>
    [Fact]
    public async Task CreateContactAsync_WhenRealModeSucceeds_ShouldReturnCreatedContactId()
    {
        // Arrange
        AkuiteoCreateContactRequest? capturedRequest = null;
        var service = CreateService();
        var request = CreateRequest();

        akuiteoContactProviderMock
            .Setup(provider => provider.CreateContactAsync(It.IsAny<AkuiteoCreateContactRequest>()))
            .Callback<AkuiteoCreateContactRequest>(payload => capturedRequest = payload)
            .ReturnsAsync(new AkuiteoContactCreationProviderResult
            {
                IsSuccess = true,
                StatusCode = 201,
                ContactId = "500145940"
            });

        // Act
        var result = await service.CreateContactAsync(request);

        // Assert
        Assert.Equal("500145940", result.ContactId);
        Assert.NotNull(capturedRequest);
        Assert.Equal(request.AccountNumber, capturedRequest!.AccountNumber);
        Assert.Equal(request.Title, capturedRequest.Title);
        Assert.Equal(request.LastName, capturedRequest.LastName);
        Assert.Equal(request.FirstName, capturedRequest.FirstName);
        Assert.Equal(request.JobTitle, capturedRequest.JobTitle);
        Assert.Equal(request.ContactDepartment, capturedRequest.ContactDepartment);
        Assert.Equal(request.CompanyRole, capturedRequest.CompanyRole);
        Assert.Equal(request.Email, capturedRequest.Email);
        Assert.Equal(request.MobilePhone, capturedRequest.MobilePhone);
        Assert.Equal(request.ContactTypes!.IsDigitalVaultContact!.Value, capturedRequest.ContactTypes.IsDigitalVaultContact);
        Assert.Equal(request.ContactTypes.IsDebtCollectionContact!.Value, capturedRequest.ContactTypes.IsDebtCollectionContact);
        Assert.Equal(request.ContactTypes.IsMandateSignatory!.Value, capturedRequest.ContactTypes.IsMandateSignatory);
    }

    /// <summary>
    /// Ensures downstream technical failures are translated to a conflict exception.
    /// </summary>
    [Fact]
    public async Task CreateContactAsync_WhenProviderReturnsFailure_ShouldThrowTechnicalException()
    {
        // Arrange
        var service = CreateService();
        var request = CreateRequest();

        akuiteoContactProviderMock
            .Setup(provider => provider.CreateContactAsync(It.IsAny<AkuiteoCreateContactRequest>()))
            .ReturnsAsync(new AkuiteoContactCreationProviderResult
            {
                IsSuccess = false,
                StatusCode = 500,
                ErrorMessage = "downstream error"
            });

        // Act
        var exception = await Assert.ThrowsAsync<AkuiteoContactCreationTechnicalException>(() => service.CreateContactAsync(request));

        // Assert
        Assert.Equal("downstream error", exception.Message);
    }

    /// <summary>
    /// Ensures the token retrieval failure is translated to a contact technical exception.
    /// </summary>
    [Fact]
    public async Task CreateContactAsync_WhenTokenRetrievalFails_ShouldThrowTechnicalException()
    {
        // Arrange
        var service = CreateService();
        var request = CreateRequest();

        akuiteoContactProviderMock
            .Setup(provider => provider.CreateContactAsync(It.IsAny<AkuiteoCreateContactRequest>()))
            .ThrowsAsync(new AkuiteoAuthenticationTechnicalException("Unable to retrieve the Microsoft token for Akuiteo."));

        // Act
        var exception = await Assert.ThrowsAsync<AkuiteoContactCreationTechnicalException>(() => service.CreateContactAsync(request));

        // Assert
        Assert.Equal("Unable to retrieve the Microsoft token for Akuiteo.", exception.Message);
    }

    /// <summary>
    /// Ensures an unreachable downstream service is translated to a contact technical exception.
    /// </summary>
    [Fact]
    public async Task CreateContactAsync_WhenProviderThrowsHttpRequestException_ShouldThrowTechnicalException()
    {
        // Arrange
        var service = CreateService();
        var request = CreateRequest();

        akuiteoContactProviderMock
            .Setup(provider => provider.CreateContactAsync(It.IsAny<AkuiteoCreateContactRequest>()))
            .ThrowsAsync(new HttpRequestException("network error"));

        // Act
        var exception = await Assert.ThrowsAsync<AkuiteoContactCreationTechnicalException>(() => service.CreateContactAsync(request));

        // Assert
        Assert.Equal("Akuiteo is unavailable.", exception.Message);
    }

    /// <summary>
    /// Ensures a downstream timeout is translated to a contact technical exception.
    /// </summary>
    [Fact]
    public async Task CreateContactAsync_WhenProviderThrowsTaskCanceledException_ShouldThrowTechnicalException()
    {
        // Arrange
        var service = CreateService();
        var request = CreateRequest();

        akuiteoContactProviderMock
            .Setup(provider => provider.CreateContactAsync(It.IsAny<AkuiteoCreateContactRequest>()))
            .ThrowsAsync(new TaskCanceledException("timeout"));

        // Act
        var exception = await Assert.ThrowsAsync<AkuiteoContactCreationTechnicalException>(() => service.CreateContactAsync(request));

        // Assert
        Assert.Equal("Akuiteo timed out.", exception.Message);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Creates the service under test.
    /// </summary>
    /// <returns>The configured service.</returns>
    private AkuiteoContactService CreateService()
    {
        return new AkuiteoContactService(
            akuiteoContactProviderMock.Object,
            loggerMock.Object);
    }

    /// <summary>
    /// Creates a valid Registry request.
    /// </summary>
    /// <returns>A valid request instance.</returns>
    private static AkuiteoContactCreationRequest CreateRequest()
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

    #endregion
}

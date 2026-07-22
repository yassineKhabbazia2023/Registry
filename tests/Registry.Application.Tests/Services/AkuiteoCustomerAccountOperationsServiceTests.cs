using Application.Exceptions;
using Application.Interfaces;
using Application.Models.Results;
using Application.Requests;
using Application.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Registry.Application.Tests.Services;

/// <summary>
/// Tests Akuiteo customer account operations on <see cref="AkuiteoCustomerService"/>.
/// </summary>
public class AkuiteoCustomerAccountOperationsServiceTests
{
    private const int AccountId = 12345;
    private const string AccountNumber = "9010001695";

    private readonly Mock<IAkuiteoCustomerProvider> providerMock = new();
    private readonly Mock<IAccountService> accountServiceMock = new();
    private readonly Mock<ILogger<AkuiteoCustomerService>> loggerMock = new();

    #region AccountOperations

    /// <summary>
    /// Ensures payment information uses the account number resolved from the Registry identifier.
    /// </summary>
    [Fact]
    public async Task GetPaymentInformationsAsync_WhenProviderSucceeds_ShouldReturnResponse()
    {
        // Arrange
        var response = CreatePaymentInformationsResponse();
        providerMock
            .Setup(provider => provider.GetPaymentInformationsAsync(AccountNumber))
            .ReturnsAsync(new AkuiteoPaymentInformationsProviderResult
            {
                IsSuccess = true,
                StatusCode = 200,
                Response = response
            });
        var service = CreateService();

        // Act
        var result = await service.GetPaymentInformationsAsync(AccountId);

        // Assert
        Assert.Same(response.Data, result);
        accountServiceMock.Verify(service => service.GetAccountNumberByIdAsync(AccountId), Times.Once);
        providerMock.Verify(provider => provider.GetPaymentInformationsAsync(AccountNumber), Times.Once);
    }

    /// <summary>
    /// Ensures payment-information provider failures become account technical exceptions.
    /// </summary>
    [Fact]
    public async Task GetPaymentInformationsAsync_WhenProviderFails_ShouldThrowTechnicalException()
    {
        // Arrange
        providerMock
            .Setup(provider => provider.GetPaymentInformationsAsync(AccountNumber))
            .ReturnsAsync(new AkuiteoPaymentInformationsProviderResult
            {
                IsSuccess = false,
                StatusCode = 400,
                ErrorMessage = "payment lookup failed"
            });
        var service = CreateService();

        // Act
        var exception = await Assert.ThrowsAsync<AkuiteoAccountOperationTechnicalException>(
            () => service.GetPaymentInformationsAsync(AccountId));

        // Assert
        Assert.Equal("payment lookup failed", exception.Message);
    }

    /// <summary>
    /// Ensures payment-information authentication failures become account technical exceptions.
    /// </summary>
    [Fact]
    public async Task GetPaymentInformationsAsync_WhenAuthenticationFails_ShouldThrowTechnicalException()
    {
        // Arrange
        providerMock
            .Setup(provider => provider.GetPaymentInformationsAsync(AccountNumber))
            .ThrowsAsync(new AkuiteoAuthenticationTechnicalException("token failure"));
        var service = CreateService();

        // Act
        var exception = await Assert.ThrowsAsync<AkuiteoAccountOperationTechnicalException>(
            () => service.GetPaymentInformationsAsync(AccountId));

        // Assert
        Assert.Equal("token failure", exception.Message);
    }

    /// <summary>
    /// Ensures malformed payment responses are logged and mapped while retaining the parsing exception chain.
    /// </summary>
    [Fact]
    public async Task GetPaymentInformationsAsync_WhenResponseIsMalformed_ShouldPreserveExceptionChain()
    {
        // Arrange
        var jsonException = new Newtonsoft.Json.JsonReaderException("invalid JSON");
        var deserializationException = new AkuiteoResponseDeserializationTechnicalException(
            "Akuiteo returned an invalid response.",
            jsonException);
        providerMock
            .Setup(provider => provider.GetPaymentInformationsAsync(AccountNumber))
            .ThrowsAsync(deserializationException);
        var service = CreateService();

        // Act
        var exception = await Assert.ThrowsAsync<AkuiteoAccountOperationTechnicalException>(
            () => service.GetPaymentInformationsAsync(AccountId));

        // Assert
        Assert.Equal("Akuiteo returned an invalid response.", exception.Message);
        Assert.Same(deserializationException, exception.InnerException);
        Assert.Same(jsonException, exception.InnerException!.InnerException);
    }

    /// <summary>
    /// Ensures payment-information network failures become account technical exceptions.
    /// </summary>
    [Fact]
    public async Task GetPaymentInformationsAsync_WhenNetworkFails_ShouldThrowTechnicalException()
    {
        // Arrange
        providerMock
            .Setup(provider => provider.GetPaymentInformationsAsync(AccountNumber))
            .ThrowsAsync(new HttpRequestException("network failure"));
        var service = CreateService();

        // Act
        var exception = await Assert.ThrowsAsync<AkuiteoAccountOperationTechnicalException>(
            () => service.GetPaymentInformationsAsync(AccountId));

        // Assert
        Assert.Equal("Akuiteo is unavailable.", exception.Message);
    }

    /// <summary>
    /// Ensures payment-information timeouts become account technical exceptions.
    /// </summary>
    [Fact]
    public async Task GetPaymentInformationsAsync_WhenTimedOut_ShouldThrowTechnicalException()
    {
        // Arrange
        providerMock
            .Setup(provider => provider.GetPaymentInformationsAsync(AccountNumber))
            .ThrowsAsync(new TaskCanceledException("timeout"));
        var service = CreateService();

        // Act
        var exception = await Assert.ThrowsAsync<AkuiteoAccountOperationTechnicalException>(
            () => service.GetPaymentInformationsAsync(AccountId));

        // Assert
        Assert.Equal("Akuiteo timed out.", exception.Message);
    }

    /// <summary>
    /// Ensures an unknown Registry account stops the downstream Akuiteo call.
    /// </summary>
    [Fact]
    public async Task GetPaymentInformationsAsync_WhenAccountDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var service = CreateService(accountNumber: null);

        // Act
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.GetPaymentInformationsAsync(AccountId));

        // Assert
        providerMock.Verify(
            provider => provider.GetPaymentInformationsAsync(It.IsAny<string>()),
            Times.Never);
    }

    /// <summary>
    /// Ensures banking information uses the account number resolved from the Registry identifier.
    /// </summary>
    [Fact]
    public async Task UpdateBankingInformationsAsync_WhenProviderSucceeds_ShouldReturnResponse()
    {
        // Arrange
        var request = new[] { new AkuiteoBankingInformationRequest { Action = "ADD" } };
        var response = CreateResponse();
        providerMock
            .Setup(provider => provider.UpdateBankingInformationsAsync(AccountNumber, request))
            .ReturnsAsync(CreateSuccessResult(response));
        var service = CreateService();

        // Act
        var result = await service.UpdateBankingInformationsAsync(AccountId, request);

        // Assert
        Assert.Same(response, result);
        accountServiceMock.Verify(service => service.GetAccountNumberByIdAsync(AccountId), Times.Once);
        providerMock.Verify(
            provider => provider.UpdateBankingInformationsAsync(AccountNumber, request),
            Times.Once);
    }

    /// <summary>
    /// Ensures the generic patch uses the account number resolved from the Registry identifier.
    /// </summary>
    [Fact]
    public async Task PatchAccountAsync_WhenProviderSucceeds_ShouldReturnResponse()
    {
        // Arrange
        var request = JObject.Parse("""{"nested":{"value":null}}""");
        var response = CreateResponse();
        providerMock
            .Setup(provider => provider.PatchAccountAsync(AccountNumber, It.IsAny<JObject>()))
            .ReturnsAsync(CreateSuccessResult(response));
        var service = CreateService();

        // Act
        var result = await service.PatchAccountAsync(AccountId, request);

        // Assert
        Assert.Same(response, result);
        accountServiceMock.Verify(service => service.GetAccountNumberByIdAsync(AccountId), Times.Once);
        providerMock.Verify(
            provider => provider.PatchAccountAsync(
                AccountNumber,
                It.Is<JObject>(payload => payload.ToString(Formatting.None) == request.ToString(Formatting.None))),
            Times.Once);
    }

    /// <summary>
    /// Ensures invalid Registry identifiers are rejected before account resolution.
    /// </summary>
    [Fact]
    public async Task GetPaymentInformationsAsync_WhenAccountIdIsInvalid_ShouldRejectRequest()
    {
        // Arrange
        var service = CreateService();

        // Act
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.GetPaymentInformationsAsync(0));

        // Assert
        accountServiceMock.Verify(
            accountService => accountService.GetAccountNumberByIdAsync(It.IsAny<int>()),
            Times.Never);
    }

    /// <summary>
    /// Ensures generic updates reject invalid Registry identifiers before account resolution.
    /// </summary>
    [Fact]
    public async Task PatchAccountAsync_WhenAccountIdIsInvalid_ShouldRejectRequest()
    {
        // Arrange
        var service = CreateService();

        // Act
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.PatchAccountAsync(0, JObject.Parse("{}")));

        // Assert
        accountServiceMock.Verify(
            accountService => accountService.GetAccountNumberByIdAsync(It.IsAny<int>()),
            Times.Never);
    }

    /// <summary>
    /// Ensures a downstream failure result becomes the standard account technical exception.
    /// </summary>
    [Fact]
    public async Task PatchAccountAsync_WhenProviderFails_ShouldThrowTechnicalException()
    {
        // Arrange
        var request = JObject.Parse("{}");
        providerMock
            .Setup(provider => provider.PatchAccountAsync(AccountNumber, It.IsAny<JObject>()))
            .ReturnsAsync(new AkuiteoAccountOperationProviderResult
            {
                IsSuccess = false,
                StatusCode = 429,
                ErrorMessage = "rate limit exceeded"
            });
        var service = CreateService();

        // Act
        var exception = await Assert.ThrowsAsync<AkuiteoAccountOperationTechnicalException>(
            () => service.PatchAccountAsync(AccountId, request));

        // Assert
        Assert.Equal("rate limit exceeded", exception.Message);
    }

    /// <summary>
    /// Ensures authentication failures become account technical exceptions.
    /// </summary>
    [Fact]
    public async Task PatchAccountAsync_WhenAuthenticationFails_ShouldThrowTechnicalException()
    {
        // Arrange
        var request = JObject.Parse("{}");
        providerMock
            .Setup(provider => provider.PatchAccountAsync(AccountNumber, It.IsAny<JObject>()))
            .ThrowsAsync(new AkuiteoAuthenticationTechnicalException("token failure"));
        var service = CreateService();

        // Act
        var exception = await Assert.ThrowsAsync<AkuiteoAccountOperationTechnicalException>(
            () => service.PatchAccountAsync(AccountId, request));

        // Assert
        Assert.Equal("token failure", exception.Message);
    }

    /// <summary>
    /// Ensures malformed account-operation responses are logged and mapped while retaining the parsing exception chain.
    /// </summary>
    [Fact]
    public async Task PatchAccountAsync_WhenResponseIsMalformed_ShouldPreserveExceptionChain()
    {
        // Arrange
        var request = JObject.Parse("{}");
        var jsonException = new System.Text.Json.JsonException("invalid JSON");
        var deserializationException = new AkuiteoResponseDeserializationTechnicalException(
            "Akuiteo returned an invalid response.",
            jsonException);
        providerMock
            .Setup(provider => provider.PatchAccountAsync(AccountNumber, It.IsAny<JObject>()))
            .ThrowsAsync(deserializationException);
        var service = CreateService();

        // Act
        var exception = await Assert.ThrowsAsync<AkuiteoAccountOperationTechnicalException>(
            () => service.PatchAccountAsync(AccountId, request));

        // Assert
        Assert.Equal("Akuiteo returned an invalid response.", exception.Message);
        Assert.Same(deserializationException, exception.InnerException);
        Assert.Same(jsonException, exception.InnerException!.InnerException);
    }

    /// <summary>
    /// Ensures network failures become account technical exceptions.
    /// </summary>
    [Fact]
    public async Task PatchAccountAsync_WhenNetworkFails_ShouldThrowTechnicalException()
    {
        // Arrange
        var request = JObject.Parse("{}");
        providerMock
            .Setup(provider => provider.PatchAccountAsync(AccountNumber, It.IsAny<JObject>()))
            .ThrowsAsync(new HttpRequestException("network failure"));
        var service = CreateService();

        // Act
        var exception = await Assert.ThrowsAsync<AkuiteoAccountOperationTechnicalException>(
            () => service.PatchAccountAsync(AccountId, request));

        // Assert
        Assert.Equal("Akuiteo is unavailable.", exception.Message);
    }

    /// <summary>
    /// Ensures timeout failures become account technical exceptions.
    /// </summary>
    [Fact]
    public async Task PatchAccountAsync_WhenTimedOut_ShouldThrowTechnicalException()
    {
        // Arrange
        var request = JObject.Parse("{}");
        providerMock
            .Setup(provider => provider.PatchAccountAsync(AccountNumber, It.IsAny<JObject>()))
            .ThrowsAsync(new TaskCanceledException("timeout"));
        var service = CreateService();

        // Act
        var exception = await Assert.ThrowsAsync<AkuiteoAccountOperationTechnicalException>(
            () => service.PatchAccountAsync(AccountId, request));

        // Assert
        Assert.Equal("Akuiteo timed out.", exception.Message);
    }

    #endregion

    /// <summary>
    /// Creates the service under test.
    /// </summary>
    /// <returns>The service.</returns>
    private AkuiteoCustomerService CreateService(string? accountNumber = AccountNumber)
    {
        accountServiceMock
            .Setup(service => service.GetAccountNumberByIdAsync(AccountId))
            .ReturnsAsync(accountNumber);

        return new AkuiteoCustomerService(
            providerMock.Object,
            accountServiceMock.Object,
            Mock.Of<IContactRepository>(),
            loggerMock.Object);
    }

    /// <summary>
    /// Creates a successful Akuiteo response.
    /// </summary>
    /// <returns>The response.</returns>
    private static AkuiteoAccountOperationResponse CreateResponse()
    {
        return new AkuiteoAccountOperationResponse
        {
            Meta = new AkuiteoMetaResponse { Status = "succeeded", Messages = Array.Empty<AkuiteoMessageResponse>() }
        };
    }

    /// <summary>
    /// Creates a successful payment-information response.
    /// </summary>
    /// <returns>The response.</returns>
    private static AkuiteoPaymentInformationsResponse CreatePaymentInformationsResponse()
    {
        return new AkuiteoPaymentInformationsResponse
        {
            Meta = new AkuiteoMetaResponse { Status = "succeeded" },
            Data = new AkuiteoPaymentInformationsDataResponse()
        };
    }

    /// <summary>
    /// Creates a successful provider result.
    /// </summary>
    /// <param name="response">The Akuiteo response.</param>
    /// <returns>The provider result.</returns>
    private static AkuiteoAccountOperationProviderResult CreateSuccessResult(AkuiteoAccountOperationResponse response)
    {
        return new AkuiteoAccountOperationProviderResult
        {
            IsSuccess = true,
            StatusCode = 200,
            Response = response
        };
    }
}

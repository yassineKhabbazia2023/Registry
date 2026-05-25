using System.Net;
using System.Text.Json;
using Application.Exceptions;
using Application.Providers;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace Registry.Infrastructure.Tests.Providers;

/// <summary>
/// Tests for <see cref="AkuiteoEligibilityProvider"/>.
/// </summary>
public class AkuiteoEligibilityProviderTests
{
    #region ExistsAsync

    /// <summary>
    /// Ensures the provider posts to the Akuiteo account search endpoint with the expected payload.
    /// </summary>
    [Fact]
    public async Task ExistsAsync_WhenAkuiteoReturnsData_ShouldPostExpectedRequestAndReturnTrue()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        string? capturedRequestBody = null;
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"meta":{"status":"succeeded","messages":[]},"data":[{"property1":"value1"}]}""")
        };

        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) =>
            {
                capturedRequest = request;
                capturedRequestBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            })
            .ReturnsAsync(response);

        var provider = CreateProvider(handlerMock);

        // Act
        var result = await provider.ExistsAsync("72200393604516");

        // Assert
        Assert.True(result);
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest!.Method);
        Assert.Equal(new Uri("https://api.akuiteo.local/akuiteo/account/search"), capturedRequest.RequestUri);

        Assert.NotNull(capturedRequestBody);
        using var jsonDocument = JsonDocument.Parse(capturedRequestBody);
        var siret = jsonDocument.RootElement.GetProperty("siret");
        Assert.Equal("IS", siret.GetProperty("operator").GetString());
        Assert.Equal("72200393604516", siret.GetProperty("value").GetString());
        Assert.Equal("*", siret.GetProperty("wildcards").GetString());
    }

    /// <summary>
    /// Ensures an empty Akuiteo data array means the account does not exist.
    /// </summary>
    [Fact]
    public async Task ExistsAsync_WhenAkuiteoReturnsEmptyData_ShouldReturnFalse()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"meta":{"status":"succeeded","messages":[]},"data":[]}""")
        };

        var handlerMock = CreateHandler(response);
        var provider = CreateProvider(handlerMock);

        // Act
        var result = await provider.ExistsAsync("73282932000074");

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Ensures a downstream non-success response is surfaced as an Akuiteo account-search technical exception.
    /// </summary>
    [Fact]
    public async Task ExistsAsync_WhenDownstreamFails_ShouldThrowTechnicalException()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = new StringContent("Akuiteo unavailable")
        };

        var handlerMock = CreateHandler(response);
        var provider = CreateProvider(handlerMock);

        // Act
        var exception = await Assert.ThrowsAsync<AkuiteoAccountSearchTechnicalException>(() => provider.ExistsAsync("72200393604516"));

        // Assert
        Assert.Equal("Akuiteo unavailable", exception.Message);
    }

    /// <summary>
    /// Ensures a failed Akuiteo metadata status is surfaced as an Akuiteo account-search technical exception.
    /// </summary>
    [Fact]
    public async Task ExistsAsync_WhenAkuiteoMetaStatusFails_ShouldThrowTechnicalException()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"meta":{"status":"failed","messages":[{"timestamp":"2026-04-13T12:44:49.244+0000","code":"BAD_REQUEST","level":"error","text":"siret is invalid"}]},"data":[]}""")
        };

        var handlerMock = CreateHandler(response);
        var provider = CreateProvider(handlerMock);

        // Act
        var exception = await Assert.ThrowsAsync<AkuiteoAccountSearchTechnicalException>(() => provider.ExistsAsync("72200393604516"));

        // Assert
        Assert.Equal("siret is invalid", exception.Message);
    }

    /// <summary>
    /// Ensures an invalid 2xx downstream payload is surfaced as an Akuiteo account-search technical exception.
    /// </summary>
    [Fact]
    public async Task ExistsAsync_WhenSuccessResponsePayloadIsInvalid_ShouldThrowTechnicalException()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("not-json")
        };

        var handlerMock = CreateHandler(response);
        var provider = CreateProvider(handlerMock);

        // Act
        var exception = await Assert.ThrowsAsync<AkuiteoAccountSearchTechnicalException>(() => provider.ExistsAsync("72200393604516"));

        // Assert
        Assert.Equal("Akuiteo returned an invalid response.", exception.Message);
    }

    /// <summary>
    /// Ensures token acquisition failures from the shared Akuiteo handler are wrapped in the account-search technical exception.
    /// </summary>
    [Fact]
    public async Task ExistsAsync_WhenTokenRetrievalFails_ShouldThrowAccountSearchTechnicalException()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new AkuiteoAuthenticationTechnicalException("Unable to retrieve the Microsoft token for Akuiteo."));

        var provider = CreateProvider(handlerMock);

        // Act
        var exception = await Assert.ThrowsAsync<AkuiteoAccountSearchTechnicalException>(() => provider.ExistsAsync("72200393604516"));

        // Assert
        Assert.Equal("Unable to retrieve the Microsoft token for Akuiteo.", exception.Message);
    }

    #endregion

    /// <summary>
    /// Creates an HTTP handler returning the specified response.
    /// </summary>
    /// <param name="response">The response returned by the handler.</param>
    /// <returns>The configured HTTP handler mock.</returns>
    private static Mock<HttpMessageHandler> CreateHandler(HttpResponseMessage response)
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        return handlerMock;
    }

    /// <summary>
    /// Creates an Akuiteo eligibility provider using the specified HTTP handler.
    /// </summary>
    /// <param name="handlerMock">The HTTP handler mock.</param>
    /// <returns>The Akuiteo eligibility provider.</returns>
    private static AkuiteoEligibilityProvider CreateProvider(Mock<HttpMessageHandler> handlerMock)
    {
        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://api.akuiteo.local/")
        };

        var loggerMock = new Mock<ILogger<AkuiteoEligibilityProvider>>();
        return new AkuiteoEligibilityProvider(httpClient, loggerMock.Object);
    }
}

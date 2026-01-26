using System.Net;
using Application.Models.Results;
using Application.Providers;
using Application.Requests;
using Moq;
using Moq.Protected;

namespace Registry.Infrastructure.Tests.Providers;

public class HubSpotProviderTests
{
    [Fact]
    public async Task SubmitIntegrationAsync_WithValidPayload_PostsToHubSpot()
    {
        // Arrange
        var portalId = "143978459";
        var formGuid = "48d828d6-5e31-40ab-afee-416a014879f0";
        var payload = new HubSpotSubmissionRequest
        {
            Fields = new List<HubSpotFieldRequest>
            {
                new() { Name = "code_client", Value = "ACC-2025-001847" }
            }
        };

        HttpRequestMessage? capturedRequest = null;
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);

        var mockHttpMessageHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(httpResponseMessage);

        var httpClient = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://api.hsforms.com/submissions/v3/integration/submit/")
        };

        var clientFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        clientFactory.Setup(factory => factory.CreateClient("HubSpot")).Returns(httpClient);

        var provider = new HubSpotProvider(clientFactory.Object);

        // Act
        var result = await provider.SubmitIntegrationAsync(portalId, formGuid, payload);

        // Assert
        clientFactory.Verify(factory => factory.CreateClient("HubSpot"), Times.Once);
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest!.Method);
        Assert.Equal(new Uri($"https://api.hsforms.com/submissions/v3/integration/submit/{portalId}/{formGuid}"), capturedRequest.RequestUri);
        Assert.NotNull(capturedRequest.Content);
        Assert.Equal("application/json", capturedRequest.Content!.Headers.ContentType?.MediaType);
        Assert.True(result.IsSuccess);
        Assert.Equal((int)HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task SubmitIntegrationAsync_WhenBadRequest_ReturnsFailureResult()
    {
        // Arrange
        var portalId = "143978459";
        var formGuid = "48d828d6-5e31-40ab-afee-416a014879f0";
        var payload = new HubSpotSubmissionRequest
        {
            Fields = new List<HubSpotFieldRequest>()
        };

        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("invalid payload")
        };

        var mockHttpMessageHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponseMessage);

        var httpClient = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://api.hsforms.com/submissions/v3/integration/submit/")
        };

        var clientFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        clientFactory.Setup(factory => factory.CreateClient("HubSpot")).Returns(httpClient);

        var provider = new HubSpotProvider(clientFactory.Object);

        // Act
        var result = await provider.SubmitIntegrationAsync(portalId, formGuid, payload);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal((int)HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("invalid payload", result.ErrorMessage);
    }
}

using System.Net;
using Application.Models;
using Application.Providers;
using Moq;
using Moq.Protected;

namespace Registry.Infrastructure.Tests.Providers;

/// <summary>
/// Tests for <see cref="AkuiteoDocumentProvider"/>.
/// </summary>
public class AkuiteoDocumentProviderTests
{
    #region UploadDocumentAsync

    /// <summary>
    /// Ensures the provider posts multipart content to the Akuiteo account document endpoint.
    /// </summary>
    [Fact]
    public async Task UploadDocumentAsync_WhenSuccessful_ShouldPostExpectedMultipartRequest()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        string? capturedRequestBody = null;
        var response = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent("""{"meta":{"status":"succeeded","messages":[]},"data":{}}""")
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

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://api.akuiteo.local/")
        };

        var provider = new AkuiteoDocumentProvider(httpClient);

        // Act
        var result = await provider.UploadDocumentAsync(CreateRequest());

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest!.Method);
        Assert.Equal(new Uri("https://api.akuiteo.local/akuiteo/account/9010001695/documents"), capturedRequest.RequestUri);
        Assert.Equal("multipart/form-data", capturedRequest.Content!.Headers.ContentType!.MediaType);
        Assert.NotNull(capturedRequestBody);
        Assert.Contains("name=document", capturedRequestBody);
        Assert.Contains("filename=sample.pdf", capturedRequestBody);
        Assert.Contains("Content-Type: application/pdf", capturedRequestBody);
        Assert.Contains("document-content", capturedRequestBody);
    }

    /// <summary>
    /// Ensures a downstream non-success response is mapped to a failure result.
    /// </summary>
    [Fact]
    public async Task UploadDocumentAsync_WhenDownstreamFails_ShouldReturnFailureResult()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = new StringContent("Akuiteo unavailable")
        };

        var provider = CreateProvider(response);

        // Act
        var result = await provider.UploadDocumentAsync(CreateRequest());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal((int)HttpStatusCode.BadGateway, result.StatusCode);
        Assert.Equal("Akuiteo unavailable", result.ErrorMessage);
        Assert.Equal("Akuiteo unavailable", result.RawResponseBody);
    }

    /// <summary>
    /// Ensures a downstream non-success Akuiteo metadata response with string messages is mapped to a clean error message.
    /// </summary>
    [Fact]
    public async Task UploadDocumentAsync_WhenDownstreamFailsWithStringMetaMessage_ShouldReturnMessageAndRawResponse()
    {
        // Arrange
        const string responseBody = """
        {
          "meta": {
            "status": "failed",
            "messages": [
              "Internal error"
            ]
          },
          "data": {
          }
        }
        """;
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent(responseBody)
        };

        var provider = CreateProvider(response);

        // Act
        var result = await provider.UploadDocumentAsync(CreateRequest());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal((int)HttpStatusCode.InternalServerError, result.StatusCode);
        Assert.Equal("Internal error", result.ErrorMessage);
        Assert.Equal(responseBody, result.RawResponseBody);
    }

    /// <summary>
    /// Ensures a 2xx downstream response with a failed Akuiteo metadata status is mapped to a failure result.
    /// </summary>
    [Fact]
    public async Task UploadDocumentAsync_WhenAkuiteoMetaStatusFails_ShouldReturnFailureResult()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"meta":{"status":"failed","messages":[{"text":"document is invalid"}]},"data":{}}""")
        };

        var provider = CreateProvider(response);

        // Act
        var result = await provider.UploadDocumentAsync(CreateRequest());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal((int)HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("document is invalid", result.ErrorMessage);
        Assert.NotNull(result.RawResponseBody);
    }

    /// <summary>
    /// Ensures an empty successful response is accepted because the upload endpoint does not document a response body.
    /// </summary>
    [Fact]
    public async Task UploadDocumentAsync_WhenSuccessResponseIsEmpty_ShouldReturnSuccessResult()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent(string.Empty)
        };

        var provider = CreateProvider(response);

        // Act
        var result = await provider.UploadDocumentAsync(CreateRequest());

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal((int)HttpStatusCode.Created, result.StatusCode);
        Assert.Null(result.ErrorMessage);
        Assert.Null(result.RawResponseBody);
    }

    /// <summary>
    /// Ensures a malformed 2xx downstream payload is mapped to a failure result.
    /// </summary>
    [Fact]
    public async Task UploadDocumentAsync_WhenSuccessResponsePayloadIsInvalid_ShouldReturnFailureResult()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("not-json")
        };

        var provider = CreateProvider(response);

        // Act
        var result = await provider.UploadDocumentAsync(CreateRequest());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal((int)HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("Akuiteo returned an invalid response.", result.ErrorMessage);
        Assert.Equal("not-json", result.RawResponseBody);
    }

    #endregion

    /// <summary>
    /// Creates an Akuiteo document provider using a fixed response.
    /// </summary>
    /// <param name="response">The response returned by the handler.</param>
    /// <returns>The Akuiteo document provider.</returns>
    private static AkuiteoDocumentProvider CreateProvider(HttpResponseMessage response)
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://api.akuiteo.local/")
        };

        return new AkuiteoDocumentProvider(httpClient);
    }

    /// <summary>
    /// Creates a valid document upload request.
    /// </summary>
    /// <returns>A valid request instance.</returns>
    private static AkuiteoDocumentUploadRequest CreateRequest()
    {
        return new AkuiteoDocumentUploadRequest
        {
            AccountNumber = "9010001695",
            DocumentName = "sample.pdf",
            ContentType = "application/pdf",
            Length = 16,
            Content = new MemoryStream("document-content"u8.ToArray())
        };
    }
}

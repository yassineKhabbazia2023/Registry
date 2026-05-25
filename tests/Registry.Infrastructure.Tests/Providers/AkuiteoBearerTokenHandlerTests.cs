using System.Net;
using Application.Exceptions;
using Application.Providers;
using Azure.Core;
using Microsoft.Extensions.Logging;
using Moq;

namespace Registry.Infrastructure.Tests.Providers;

/// <summary>
/// Tests for <see cref="AkuiteoBearerTokenHandler"/>.
/// </summary>
public class AkuiteoBearerTokenHandlerTests
{
    #region SendAsync

    /// <summary>
    /// Ensures the handler injects the bearer token in outgoing requests.
    /// </summary>
    [Fact]
    public async Task SendAsync_WhenTokenIsRetrieved_ShouldAddBearerToken()
    {
        // Arrange
        var credential = new TestTokenCredential("access-token");
        var innerHandler = new CapturingHandler();
        var handler = new AkuiteoBearerTokenHandler(
            credential,
            ["api://client-id/.default"],
            Mock.Of<ILogger<AkuiteoBearerTokenHandler>>())
        {
            InnerHandler = innerHandler
        };

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://akuiteo.local/")
        };

        // Act
        await httpClient.GetAsync("accounts");

        // Assert
        Assert.NotNull(innerHandler.CapturedRequest);
        Assert.Equal("Bearer", innerHandler.CapturedRequest!.Headers.Authorization?.Scheme);
        Assert.Equal("access-token", innerHandler.CapturedRequest.Headers.Authorization?.Parameter);
    }

    /// <summary>
    /// Ensures token acquisition failures are translated into the Akuiteo technical exception flow.
    /// </summary>
    [Fact]
    public async Task SendAsync_WhenTokenRetrievalFails_ShouldThrowAkuiteoAuthenticationTechnicalException()
    {
        // Arrange
        var credential = new TestTokenCredential(exception: new InvalidOperationException("token failure"));
        var handler = new AkuiteoBearerTokenHandler(
            credential,
            ["api://client-id/.default"],
            Mock.Of<ILogger<AkuiteoBearerTokenHandler>>())
        {
            InnerHandler = new CapturingHandler()
        };

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://akuiteo.local/")
        };

        // Act
        var act = () => httpClient.GetAsync("accounts");

        // Assert
        await Assert.ThrowsAsync<AkuiteoAuthenticationTechnicalException>(act);
    }

    #endregion

    /// <summary>
    /// Token credential used to control Azure AD token retrieval in tests.
    /// </summary>
    private sealed class TestTokenCredential : TokenCredential
    {
        private readonly string? token;
        private readonly Exception? exception;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestTokenCredential"/> class.
        /// </summary>
        /// <param name="token">The token to return.</param>
        /// <param name="exception">The exception to throw.</param>
        public TestTokenCredential(string? token = null, Exception? exception = null)
        {
            this.token = token;
            this.exception = exception;
        }

        /// <inheritdoc/>
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            if (exception is not null)
            {
                throw exception;
            }

            return new AccessToken(token ?? string.Empty, DateTimeOffset.UtcNow.AddMinutes(5));
        }

        /// <inheritdoc/>
        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return new ValueTask<AccessToken>(GetToken(requestContext, cancellationToken));
        }
    }

    /// <summary>
    /// Captures the outgoing request.
    /// </summary>
    private sealed class CapturingHandler : HttpMessageHandler
    {
        /// <summary>
        /// Gets the last captured request.
        /// </summary>
        public HttpRequestMessage? CapturedRequest { get; private set; }

        /// <summary>
        /// Handles the outgoing request.
        /// </summary>
        /// <param name="request">The outgoing request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A successful response.</returns>
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CapturedRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}

using Application.Exceptions;
using Application.Interfaces;
using Application.Models;
using Application.Models.Results;
using Application.Services;
using Kpmg.ExceptionMiddleware.AdvancedException;
using Microsoft.Extensions.Logging;
using Moq;

namespace Registry.Application.Tests.Services;

/// <summary>
/// Tests for <see cref="AkuiteoDocumentService"/>.
/// </summary>
public class AkuiteoDocumentServiceTests
{
    #region Fields

    private readonly Mock<IAkuiteoDocumentProvider> akuiteoDocumentProviderMock;
    private readonly Mock<ILogger<AkuiteoDocumentService>> loggerMock;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="AkuiteoDocumentServiceTests"/> class.
    /// </summary>
    public AkuiteoDocumentServiceTests()
    {
        akuiteoDocumentProviderMock = new Mock<IAkuiteoDocumentProvider>();
        loggerMock = new Mock<ILogger<AkuiteoDocumentService>>();
    }

    #region UploadDocumentAsync

    /// <summary>
    /// Ensures the service returns the upload outcome when Akuiteo succeeds.
    /// </summary>
    [Fact]
    public async Task UploadDocumentAsync_WhenProviderSucceeds_ShouldReturnUploadOutcome()
    {
        // Arrange
        var service = CreateService();
        var request = CreateRequest();

        akuiteoDocumentProviderMock
            .Setup(provider => provider.UploadDocumentAsync(request))
            .ReturnsAsync(new AkuiteoDocumentUploadProviderResult
            {
                IsSuccess = true,
                StatusCode = 201
            });

        // Act
        var result = await service.UploadDocumentAsync(request);

        // Assert
        Assert.Equal("9010001695", result.AccountNumber);
        Assert.Equal("sample.pdf", result.DocumentName);
        Assert.True(result.IsUploaded);
        akuiteoDocumentProviderMock.Verify(provider => provider.UploadDocumentAsync(request), Times.Once);
    }

    /// <summary>
    /// Ensures an empty account number is rejected before calling Akuiteo.
    /// </summary>
    [Fact]
    public async Task UploadDocumentAsync_WhenAccountNumberIsMissing_ShouldThrowBadRequestException()
    {
        // Arrange
        var service = CreateService();
        var request = CreateRequest(accountNumber: string.Empty);

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => service.UploadDocumentAsync(request));
        akuiteoDocumentProviderMock.Verify(provider => provider.UploadDocumentAsync(It.IsAny<AkuiteoDocumentUploadRequest>()), Times.Never);
    }

    /// <summary>
    /// Ensures a missing document stream is rejected before calling Akuiteo.
    /// </summary>
    [Fact]
    public async Task UploadDocumentAsync_WhenDocumentIsMissing_ShouldThrowBadRequestException()
    {
        // Arrange
        var service = CreateService();
        var request = CreateRequest(content: Stream.Null, length: 0);

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => service.UploadDocumentAsync(request));
        akuiteoDocumentProviderMock.Verify(provider => provider.UploadDocumentAsync(It.IsAny<AkuiteoDocumentUploadRequest>()), Times.Never);
    }

    /// <summary>
    /// Ensures an empty document name is rejected before calling Akuiteo.
    /// </summary>
    [Fact]
    public async Task UploadDocumentAsync_WhenDocumentNameIsMissing_ShouldThrowBadRequestException()
    {
        // Arrange
        var service = CreateService();
        var request = CreateRequest(documentName: string.Empty);

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => service.UploadDocumentAsync(request));
        akuiteoDocumentProviderMock.Verify(provider => provider.UploadDocumentAsync(It.IsAny<AkuiteoDocumentUploadRequest>()), Times.Never);
    }

    /// <summary>
    /// Ensures an unsupported content type is rejected before calling Akuiteo.
    /// </summary>
    [Fact]
    public async Task UploadDocumentAsync_WhenContentTypeIsUnsupported_ShouldThrowBadRequestException()
    {
        // Arrange
        var service = CreateService();
        var request = CreateRequest(contentType: "text/plain");

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => service.UploadDocumentAsync(request));
        akuiteoDocumentProviderMock.Verify(provider => provider.UploadDocumentAsync(It.IsAny<AkuiteoDocumentUploadRequest>()), Times.Never);
    }

    /// <summary>
    /// Ensures documents larger than 5 Mo are rejected before calling Akuiteo.
    /// </summary>
    [Fact]
    public async Task UploadDocumentAsync_WhenDocumentExceedsFiveMo_ShouldThrowBadRequestException()
    {
        // Arrange
        var service = CreateService();
        var request = CreateRequest(length: (5 * 1024 * 1024) + 1);

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => service.UploadDocumentAsync(request));
        akuiteoDocumentProviderMock.Verify(provider => provider.UploadDocumentAsync(It.IsAny<AkuiteoDocumentUploadRequest>()), Times.Never);
    }

    /// <summary>
    /// Ensures downstream technical failures are translated to a conflict exception.
    /// </summary>
    [Fact]
    public async Task UploadDocumentAsync_WhenProviderReturnsFailure_ShouldThrowTechnicalException()
    {
        // Arrange
        var service = CreateService();
        var request = CreateRequest();

        akuiteoDocumentProviderMock
            .Setup(provider => provider.UploadDocumentAsync(request))
            .ReturnsAsync(new AkuiteoDocumentUploadProviderResult
            {
                IsSuccess = false,
                StatusCode = 500,
                ErrorMessage = "downstream error"
            });

        // Act
        var exception = await Assert.ThrowsAsync<AkuiteoDocumentUploadTechnicalException>(() => service.UploadDocumentAsync(request));

        // Assert
        Assert.Equal("downstream error", exception.Message);
    }

    /// <summary>
    /// Ensures token retrieval failures are translated to a document upload technical exception.
    /// </summary>
    [Fact]
    public async Task UploadDocumentAsync_WhenTokenRetrievalFails_ShouldThrowTechnicalException()
    {
        // Arrange
        var service = CreateService();
        var request = CreateRequest();

        akuiteoDocumentProviderMock
            .Setup(provider => provider.UploadDocumentAsync(request))
            .ThrowsAsync(new AkuiteoAuthenticationTechnicalException("Unable to retrieve the Microsoft token for Akuiteo."));

        // Act
        var exception = await Assert.ThrowsAsync<AkuiteoDocumentUploadTechnicalException>(() => service.UploadDocumentAsync(request));

        // Assert
        Assert.Equal("Unable to retrieve the Microsoft token for Akuiteo.", exception.Message);
    }

    /// <summary>
    /// Ensures an unreachable downstream service is translated to a document upload technical exception.
    /// </summary>
    [Fact]
    public async Task UploadDocumentAsync_WhenProviderThrowsHttpRequestException_ShouldThrowTechnicalException()
    {
        // Arrange
        var service = CreateService();
        var request = CreateRequest();

        akuiteoDocumentProviderMock
            .Setup(provider => provider.UploadDocumentAsync(request))
            .ThrowsAsync(new HttpRequestException("network error"));

        // Act
        var exception = await Assert.ThrowsAsync<AkuiteoDocumentUploadTechnicalException>(() => service.UploadDocumentAsync(request));

        // Assert
        Assert.Equal("Akuiteo is unavailable.", exception.Message);
    }

    /// <summary>
    /// Ensures a downstream timeout is translated to a document upload technical exception.
    /// </summary>
    [Fact]
    public async Task UploadDocumentAsync_WhenProviderThrowsTaskCanceledException_ShouldThrowTechnicalException()
    {
        // Arrange
        var service = CreateService();
        var request = CreateRequest();

        akuiteoDocumentProviderMock
            .Setup(provider => provider.UploadDocumentAsync(request))
            .ThrowsAsync(new TaskCanceledException("timeout"));

        // Act
        var exception = await Assert.ThrowsAsync<AkuiteoDocumentUploadTechnicalException>(() => service.UploadDocumentAsync(request));

        // Assert
        Assert.Equal("Akuiteo timed out.", exception.Message);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Creates the service under test.
    /// </summary>
    /// <returns>The configured service.</returns>
    private AkuiteoDocumentService CreateService()
    {
        return new AkuiteoDocumentService(
            akuiteoDocumentProviderMock.Object,
            loggerMock.Object);
    }

    /// <summary>
    /// Creates a valid document upload request with optional overrides.
    /// </summary>
    /// <param name="accountNumber">The account number.</param>
    /// <param name="documentName">The document name.</param>
    /// <param name="contentType">The document content type.</param>
    /// <param name="content">The document content stream.</param>
    /// <param name="length">The document size.</param>
    /// <returns>A document upload request.</returns>
    private static AkuiteoDocumentUploadRequest CreateRequest(
        string accountNumber = "9010001695",
        string documentName = "sample.pdf",
        string contentType = "application/pdf",
        Stream? content = null,
        long length = 12)
    {
        return new AkuiteoDocumentUploadRequest
        {
            AccountNumber = accountNumber,
            DocumentName = documentName,
            ContentType = contentType,
            Length = length,
            Content = content ?? new MemoryStream("document"u8.ToArray())
        };
    }

    #endregion
}

// <copyright file="InvoiceBlobProviderTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Exceptions;
using Application.Providers;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Registry.Infrastructure.Tests.Providers;

public class InvoiceBlobProviderTests
{
    private const string DocumentPath =
        "https://strecvdsomprdfc001.blob.core.windows.net/recouvrement/Invoices/FRANT/2025/TPME_FAC_CLI_202510031534.pdf";

    private readonly Mock<BlobClient> _blobClientMock = new();
    private readonly List<Uri> _requestedUris = [];
    private readonly InvoiceBlobProvider _sut;

    public InvoiceBlobProviderTests()
    {
        _sut = new InvoiceBlobProvider(
            blobUri =>
            {
                _requestedUris.Add(blobUri);

                var realClient = new BlobClient(blobUri);
                _blobClientMock.Setup(b => b.Uri).Returns(realClient.Uri);
                _blobClientMock.Setup(b => b.BlobContainerName).Returns(realClient.BlobContainerName);
                _blobClientMock.Setup(b => b.Name).Returns(realClient.Name);

                return _blobClientMock.Object;
            },
            new Mock<ILogger<InvoiceBlobProvider>>().Object);
    }

    [Fact]
    public async Task GetPdfAsync_ShouldDownloadTheBlobAtTheDocumentPathUrl()
    {
        // Arrange
        SetupDownload(CreateDownloadResult("%PDF-1.4 fake"));

        // Act
        await using var result = await _sut.GetPdfAsync(DocumentPath);

        // Assert
        _requestedUris.Should().ContainSingle().Which.Should().Be(new Uri(DocumentPath));
    }

    [Fact]
    public async Task GetPdfAsync_ShouldReturnBlobContentAndFileName()
    {
        // Arrange
        SetupDownload(CreateDownloadResult("%PDF-1.4 fake"));

        // Act
        await using var result = await _sut.GetPdfAsync(DocumentPath);

        // Assert
        result.Should().NotBeNull();
        result!.FileName.Should().Be("TPME_FAC_CLI_202510031534.pdf");

        using var reader = new StreamReader(result.Content);
        (await reader.ReadToEndAsync()).Should().Be("%PDF-1.4 fake");
    }

    [Fact]
    public async Task GetPdfAsync_WhenBlobDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        SetupDownloadFailure(new RequestFailedException(404, "BlobNotFound"));

        // Act
        var result = await _sut.GetPdfAsync(DocumentPath);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPdfAsync_WhenStorageDeniesAccess_ShouldThrowTechnicalException()
    {
        // Arrange
        SetupDownloadFailure(new RequestFailedException(403, "AuthorizationPermissionMismatch"));

        // Act
        var act = async () => await _sut.GetPdfAsync(DocumentPath);

        // Assert
        var exception = await act.Should().ThrowAsync<InvoiceDownloadTechnicalException>();
        exception.Which.Message.Should().Contain("403");
    }

    [Fact]
    public async Task GetPdfAsync_WhenStorageNeverAnswers_ShouldThrowTechnicalException()
    {
        // Arrange
        SetupDownloadFailure(new TaskCanceledException("The operation was cancelled because it exceeded the configured timeout."));

        // Act
        var act = async () => await _sut.GetPdfAsync(DocumentPath);

        // Assert
        await act.Should().ThrowAsync<InvoiceDownloadTechnicalException>();
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("/recouvrement/Invoices/a.pdf")]
    [InlineData("http://strecvdsomprdfc001.blob.core.windows.net/recouvrement/Invoices/a.pdf")]
    [InlineData("https://strecvdsomprdfc001.blob.core.windows.net/recouvrement")]
    [InlineData("https://strecvdsomprdfc001.blob.core.windows.net/")]
    public async Task GetPdfAsync_WhenDocumentPathIsUnusable_ShouldThrowTechnicalExceptionWithoutCallingStorage(string documentPath)
    {
        // Act
        var act = async () => await _sut.GetPdfAsync(documentPath);

        // Assert
        await act.Should().ThrowAsync<InvoiceDownloadTechnicalException>();
        _requestedUris.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPdfAsync_WhenTheDocumentPathHostIsNotABlobEndpoint_ShouldThrowWithoutCallingStorage()
    {
        // Act
        var act = async () => await _sut.GetPdfAsync("https://attacker.example.com/recouvrement/Invoices/a.pdf");

        // Assert
        var exception = await act.Should().ThrowAsync<InvoiceDownloadTechnicalException>();
        exception.Which.Message.Should().Contain("attacker.example.com");
        _requestedUris.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPdfAsync_WhenAnotherStorageAccountCarriesTheDocument_ShouldServeIt()
    {
        // Arrange
        SetupDownload(CreateDownloadResult("%PDF-1.4 fake"));
        const string otherAccountPath = "https://stanotheraccount.blob.core.windows.net/factures/b.pdf";

        // Act
        await using var result = await _sut.GetPdfAsync(otherAccountPath);

        // Assert
        result.Should().NotBeNull();
        _requestedUris.Should().ContainSingle().Which.Should().Be(new Uri(otherAccountPath));
    }

    [Fact]
    public async Task GetPdfAsync_WhenTheBlobClientCannotBeBuilt_ShouldThrowTechnicalException()
    {
        // Arrange
        var sut = new InvoiceBlobProvider(
            _ => throw new ArgumentException("Cannot build the blob client."),
            new Mock<ILogger<InvoiceBlobProvider>>().Object);

        // Act
        var act = async () => await sut.GetPdfAsync(DocumentPath);

        // Assert
        await act.Should().ThrowAsync<InvoiceDownloadTechnicalException>();
    }

    [Fact]
    public async Task GetPdfAsync_WhenBlobNameCarriesControlCharacters_ShouldStripThemFromFileName()
    {
        // Arrange
        SetupDownload(CreateDownloadResult("%PDF"));

        // Act
        await using var result = await _sut.GetPdfAsync(
            "https://strecvdsomprdfc001.blob.core.windows.net/recouvrement/Invoices/inj%0D%0AX-Evil:%201ect.pdf");

        // Assert
        result.Should().NotBeNull();
        result!.FileName.Should().NotContain("\r").And.NotContain("\n");
    }

    [Fact]
    public async Task GetPdfAsync_WhenSanitizingLeavesNothing_ShouldFallBackToADefaultFileName()
    {
        // Arrange
        SetupDownload(CreateDownloadResult("%PDF"));

        // Act
        await using var result = await _sut.GetPdfAsync(
            "https://strecvdsomprdfc001.blob.core.windows.net/recouvrement/%01%02");

        // Assert
        result.Should().NotBeNull();
        result!.FileName.Should().Be("invoice.pdf");
    }

    private static BlobDownloadStreamingResult CreateDownloadResult(string content)
    {
        return BlobsModelFactory.BlobDownloadStreamingResult(
            content: new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content)));
    }

    private void SetupDownload(BlobDownloadStreamingResult downloadResult)
    {
        _blobClientMock
            .Setup(b => b.DownloadStreamingAsync(It.IsAny<BlobDownloadOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(downloadResult, new Mock<Response>().Object));
    }

    private void SetupDownloadFailure(Exception exception)
    {
        _blobClientMock
            .Setup(b => b.DownloadStreamingAsync(It.IsAny<BlobDownloadOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);
    }
}

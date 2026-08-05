// <copyright file="InvoiceContentServiceTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Models.Results;
using Application.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Pulse.Registry.Domain.Entities;

namespace Registry.Application.Tests.Services;

public class InvoiceContentServiceTests
{
    private const string DocumentPath =
        "https://strecvdsomprdfc001.blob.core.windows.net/recouvrement/Invoices/FRANT/2025/TPME_FAC_CLI_202510031534.pdf";

    private const string InvoiceNumber = "FAC-1";
    private const string AccountNumber = "ACC-1";

    private readonly Mock<IInvoiceRepository> _invoiceRepositoryMock = new();
    private readonly Mock<IInvoiceBlobProvider> _invoiceBlobProviderMock = new();

    [Fact]
    public async Task GetPdfAsync_WhenInvoiceUnknownForTheAccount_ShouldReturnNullWithoutCallingProvider()
    {
        // Arrange
        _invoiceRepositoryMock
            .Setup(repository => repository.GetInsertedByInvoiceAndAccountNumberAsync("FAC-UNKNOWN", AccountNumber))
            .ReturnsAsync((InvoiceEntity?)null);

        var service = CreateService();

        // Act
        var result = await service.GetPdfAsync("FAC-UNKNOWN", AccountNumber);

        // Assert
        result.Should().BeNull();
        _invoiceBlobProviderMock.Verify(provider => provider.GetPdfAsync(It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetPdfAsync_WhenDocumentPathIsEmpty_ShouldReturnNullWithoutCallingProvider(string? documentPath)
    {
        // Arrange
        _invoiceRepositoryMock
            .Setup(repository => repository.GetInsertedByInvoiceAndAccountNumberAsync(InvoiceNumber, AccountNumber))
            .ReturnsAsync(CreateInvoice(documentPath!));

        var service = CreateService();

        // Act
        var result = await service.GetPdfAsync(InvoiceNumber, AccountNumber);

        // Assert
        result.Should().BeNull();
        _invoiceBlobProviderMock.Verify(provider => provider.GetPdfAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetPdfAsync_WhenInvoiceKnown_ShouldServeTheDocumentPathOfTheRow()
    {
        // Arrange
        var expected = new InvoiceContentResponse(new MemoryStream(), new MemoryStream("%PDF"u8.ToArray()), "a.pdf");

        _invoiceRepositoryMock
            .Setup(repository => repository.GetInsertedByInvoiceAndAccountNumberAsync(InvoiceNumber, AccountNumber))
            .ReturnsAsync(CreateInvoice(DocumentPath));
        _invoiceBlobProviderMock
            .Setup(provider => provider.GetPdfAsync(DocumentPath))
            .ReturnsAsync(expected);

        var service = CreateService();

        // Act
        await using var result = await service.GetPdfAsync(InvoiceNumber, AccountNumber);

        // Assert
        result.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task GetPdfAsync_ShouldLookTheInvoiceUpForTheRequestedAccountOnly()
    {
        // Arrange
        var service = CreateService();

        // Act
        await service.GetPdfAsync(InvoiceNumber, "ACC-2");

        // Assert
        _invoiceRepositoryMock.Verify(
            repository => repository.GetInsertedByInvoiceAndAccountNumberAsync(InvoiceNumber, "ACC-2"),
            Times.Once);
    }

    private static InvoiceEntity CreateInvoice(string documentPath) => new()
    {
        InvoiceId = 1,
        InvoiceNumber = InvoiceNumber,
        AccountNumber = AccountNumber,
        Operation = "INSERT",
        InvoiceDate = new DateTime(2026, 1, 15),
        DocumentPath = documentPath,
        Type = "Facture RYDGE",
        Status = "Pending",
        CreatedOn = new DateTime(2026, 1, 15),
    };

    private InvoiceContentService CreateService()
        => new(
            _invoiceRepositoryMock.Object,
            _invoiceBlobProviderMock.Object,
            new Mock<ILogger<InvoiceContentService>>().Object);
}

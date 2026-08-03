// <copyright file="InvoiceReceptionServiceTest.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Exceptions;
using Application.Interfaces;
using Application.Models;
using Application.Models.Results;
using Application.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;

namespace Registry.Application.Tests.Services;

public class InvoiceReceptionServiceTest
{
    private const string BlobName = "Invoice_20260731_101530.csv";

    private readonly Mock<IBlobStorageManager> _blobStorageManagerMock;
    private readonly Mock<IInvoiceService> _invoiceServiceMock;
    private readonly Mock<IInvoiceEventPublisher> _invoiceEventPublisherMock;
    private readonly InvoiceReceptionService _sut;

    public InvoiceReceptionServiceTest()
    {
        _blobStorageManagerMock = new Mock<IBlobStorageManager>(MockBehavior.Strict);
        _invoiceServiceMock = new Mock<IInvoiceService>();
        _invoiceEventPublisherMock = new Mock<IInvoiceEventPublisher>();
        var logger = new Mock<ILogger<InvoiceReceptionService>>();

        _sut = new InvoiceReceptionService(
            _blobStorageManagerMock.Object,
            _invoiceServiceMock.Object,
            _invoiceEventPublisherMock.Object,
            logger.Object);
    }

    [Fact]
    public async Task ReceiveAsync_WithMissingExploitedColumn_RejectsWithoutStorage()
    {
        // Arrange
        var csvContent = new StringBuilder();
        csvContent.AppendLine("item_type;client_code;item_number;item_date_issue;item_field_perso15");
        csvContent.AppendLine("FAC;C000123;FA-2024-0001;15/01/2024;https://docs.pulse.fr/FA-2024-0001.pdf");

        // Act
        var outcome = await _sut.ReceiveAsync(csvContent.ToString());

        // Assert
        outcome.IsAccepted.Should().BeFalse();
        outcome.ErrorMessage.Should().Be("Invalid data: Missing columns in header");
        _blobStorageManagerMock.Verify(x => x.SaveFileAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ReceiveAsync_WithWrongSeparator_RejectsWithoutStorage()
    {
        // Arrange
        var csvContent = new StringBuilder();
        csvContent.AppendLine("client_code,item_number,item_date_issue,item_field_perso15,operation");
        csvContent.AppendLine("C000123,FA-2024-0001,15/01/2024,https://docs.pulse.fr/FA-2024-0001.pdf,INSERT");

        // Act
        var outcome = await _sut.ReceiveAsync(csvContent.ToString());

        // Assert
        outcome.IsAccepted.Should().BeFalse();
        outcome.ErrorMessage.Should().Be("Invalid data: Missing columns in header");
        _blobStorageManagerMock.Verify(x => x.SaveFileAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ReceiveAsync_WithEmptyContent_RejectsWithoutStorage()
    {
        // Act
        var outcome = await _sut.ReceiveAsync(string.Empty);

        // Assert
        outcome.IsAccepted.Should().BeFalse();
        outcome.ErrorMessage.Should().Be("Invalid data: The input data does not contain any lines.");
        _blobStorageManagerMock.Verify(x => x.SaveFileAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ReceiveAsync_WithLineColumnCountMismatch_RejectsWithoutStorage()
    {
        // Arrange
        var csvContent = new StringBuilder();
        csvContent.AppendLine("client_code;item_number;item_date_issue;item_field_perso15;operation");
        csvContent.AppendLine("C000123;FA-2024-0001;15/01/2024;https://docs.pulse.fr/FA-2024-0001.pdf;INSERT;EXTRA");

        // Act
        var outcome = await _sut.ReceiveAsync(csvContent.ToString());

        // Assert
        outcome.IsAccepted.Should().BeFalse();
        outcome.ErrorMessage.Should().StartWith("Invalid data:").And.Contain("Column count mismatch");
        _blobStorageManagerMock.Verify(x => x.SaveFileAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ReceiveAsync_WhenBlobStorageFails_RejectsWithoutProcessing()
    {
        // Arrange
        _blobStorageManagerMock.Setup(x => x.SaveFileAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new BlobStorageOperationException("An error occurred while saving the file.", new Exception("boom")));

        // Act
        var outcome = await _sut.ReceiveAsync(BuildValidCsv());

        // Assert
        outcome.IsAccepted.Should().BeFalse();
        outcome.ErrorMessage.Should().Be("Something went wrong when saving received csv");
        _invoiceServiceMock.Verify(s => s.InsertPendingLinesAsync(It.IsAny<List<RefInvoiceCsv>>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ReceiveAsync_WithValidContent_StoresProcessesAndNotifies()
    {
        // Arrange
        var csvContent = BuildValidCsv();
        var summary = new InvoiceCsvReceptionResult { BlobName = BlobName, TotalLines = 1, ValidLines = 1 };

        _blobStorageManagerMock.Setup(x => x.SaveFileAsync("Invoice", csvContent)).ReturnsAsync(BlobName);
        _invoiceServiceMock
            .Setup(s => s.InsertPendingLinesAsync(It.IsAny<List<RefInvoiceCsv>>(), BlobName))
            .ReturnsAsync(summary);

        // Act
        var outcome = await _sut.ReceiveAsync(csvContent);

        // Assert
        outcome.IsAccepted.Should().BeTrue();
        outcome.Summary.Should().BeSameAs(summary);

        _invoiceServiceMock.Verify(
            s => s.InsertPendingLinesAsync(
                It.Is<List<RefInvoiceCsv>>(l => l.Count == 1 && l[0].InvoiceNumber == "FA-2024-0001" && l[0].AccountNumber == "C000123"),
                BlobName),
            Times.Once);
        _invoiceEventPublisherMock.Verify(p => p.SendInvoiceLinesBatchEvent(BlobName), Times.Once);
    }

    [Fact]
    public async Task ReceiveAsync_WhenNoValidLines_DoesNotNotifyQueue()
    {
        // Arrange
        var csvContent = BuildValidCsv();
        var summary = new InvoiceCsvReceptionResult { BlobName = BlobName, TotalLines = 1, ValidLines = 0, RejectedLines = 1 };

        _blobStorageManagerMock.Setup(x => x.SaveFileAsync("Invoice", csvContent)).ReturnsAsync(BlobName);
        _invoiceServiceMock
            .Setup(s => s.InsertPendingLinesAsync(It.IsAny<List<RefInvoiceCsv>>(), BlobName))
            .ReturnsAsync(summary);

        // Act
        var outcome = await _sut.ReceiveAsync(csvContent);

        // Assert
        outcome.IsAccepted.Should().BeTrue();
        outcome.Summary.Should().BeSameAs(summary);
        _invoiceEventPublisherMock.Verify(p => p.SendInvoiceLinesBatchEvent(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ReceiveAsync_WithPartiallyRejectedLines_StillNotifiesQueue()
    {
        // Arrange
        var csvContent = BuildValidCsv();
        var summary = new InvoiceCsvReceptionResult { BlobName = BlobName, TotalLines = 2, ValidLines = 1, RejectedLines = 1 };

        _blobStorageManagerMock.Setup(x => x.SaveFileAsync("Invoice", csvContent)).ReturnsAsync(BlobName);
        _invoiceServiceMock
            .Setup(s => s.InsertPendingLinesAsync(It.IsAny<List<RefInvoiceCsv>>(), BlobName))
            .ReturnsAsync(summary);

        // Act
        var outcome = await _sut.ReceiveAsync(csvContent);

        // Assert
        outcome.IsAccepted.Should().BeTrue();
        _invoiceEventPublisherMock.Verify(p => p.SendInvoiceLinesBatchEvent(BlobName), Times.Once);
    }

    [Fact]
    public async Task ReceiveAsync_WhenQueueTriggerFails_IsStillAccepted()
    {
        // Arrange
        var csvContent = BuildValidCsv();
        var summary = new InvoiceCsvReceptionResult { BlobName = BlobName, TotalLines = 1, ValidLines = 1 };

        _blobStorageManagerMock.Setup(x => x.SaveFileAsync("Invoice", csvContent)).ReturnsAsync(BlobName);
        _invoiceServiceMock
            .Setup(s => s.InsertPendingLinesAsync(It.IsAny<List<RefInvoiceCsv>>(), BlobName))
            .ReturnsAsync(summary);
        _invoiceEventPublisherMock
            .Setup(p => p.SendInvoiceLinesBatchEvent(BlobName))
            .ThrowsAsync(new ServiceBusOperationException("Invoice lines queue name is not configured."));

        // Act
        var outcome = await _sut.ReceiveAsync(csvContent);

        // Assert
        outcome.IsAccepted.Should().BeTrue();
        outcome.Summary.Should().BeSameAs(summary);
    }

    private static string BuildValidCsv()
    {
        var csvContent = new StringBuilder();
        csvContent.AppendLine("client_code;item_amount_initial_inc_tax;item_number;item_date_issue;item_type;item_field_perso15;operation");
        csvContent.AppendLine("C000123;1200.00;FA-2024-0001;15/01/2024;FAC;https://docs.pulse.fr/FA-2024-0001.pdf;INSERT");
        return csvContent.ToString();
    }
}

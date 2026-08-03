// <copyright file="InvoiceServiceTest.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Helpers;
using Application.Interfaces;
using Application.Models;
using Application.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Pulse.Registry.Domain.Entities;

namespace Registry.Application.Tests.Services;

public class InvoiceServiceTest
{
    private const string BlobName = "Invoice_20260730_101530.csv";

    private readonly Mock<IInvoiceRepository> _invoiceRepositoryMock;
    private readonly InvoiceService _sut;

    public InvoiceServiceTest()
    {
        _invoiceRepositoryMock = new Mock<IInvoiceRepository>();
        _invoiceRepositoryMock
            .Setup(r => r.GetByInvoiceNumbersAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync([]);

        var logger = new Mock<ILogger<InvoiceService>>();
        _sut = new InvoiceService(_invoiceRepositoryMock.Object, new ValidationHelper<RefInvoiceCsv>(), logger.Object);
    }

    [Fact]
    public async Task InsertPendingLinesAsync_WithValidLines_InsertsThemAsPendingAndReturnsSummary()
    {
        // Arrange
        var lines = new List<RefInvoiceCsv>
        {
            CreateLine("C000123", "FA-2024-0001"),
            CreateLine("C000456", "FA-2024-0002"),
        };

        List<InvoiceEntity>? inserted = null;
        _invoiceRepositoryMock
            .Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<InvoiceEntity>>()))
            .Callback<IEnumerable<InvoiceEntity>>(entities => inserted = entities.ToList())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.InsertPendingLinesAsync(lines, BlobName);

        // Assert
        result.BlobName.Should().Be(BlobName);
        result.TotalLines.Should().Be(2);
        result.ValidLines.Should().Be(2);
        result.RejectedLines.Should().Be(0);
        result.Errors.Should().BeEmpty();

        inserted.Should().NotBeNull().And.HaveCount(2);
        inserted![0].AccountNumber.Should().Be("C000123");
        inserted[0].InvoiceNumber.Should().Be("FA-2024-0001");
        inserted[0].InvoiceDate.Should().Be(new DateTime(2024, 1, 15));
        inserted[0].DocumentPath.Should().Be("https://docs.pulse.fr/FA-2024-0001.pdf");
        inserted[0].Operation.Should().Be("INSERT");
        inserted[0].Status.Should().Be("Pending");
        inserted[0].Type.Should().Be("Facture RYDGE");
    }

    [Fact]
    public async Task InsertPendingLinesAsync_WithInvalidLine_RejectsItAndInsertsTheRest()
    {
        // Arrange
        var invalidLine = CreateLine("C000456", "FA-2024-0002");
        invalidLine.InvoiceDate = "2024-99-99";

        var lines = new List<RefInvoiceCsv>
        {
            CreateLine("C000123", "FA-2024-0001"),
            invalidLine,
        };

        // Act
        var result = await _sut.InsertPendingLinesAsync(lines, BlobName);

        // Assert
        result.TotalLines.Should().Be(2);
        result.ValidLines.Should().Be(1);
        result.RejectedLines.Should().Be(1);
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Line.Should().Be(2);
        result.Errors[0].Reason.Should().Contain("Invalid date format (dd/MM/yyyy)");

        _invoiceRepositoryMock.Verify(
            r => r.AddRangeAsync(It.Is<IEnumerable<InvoiceEntity>>(e => e.Count() == 1 && e.First().InvoiceNumber == "FA-2024-0001")),
            Times.Once);
    }

    [Fact]
    public async Task InsertPendingLinesAsync_WithDuplicateLineInFile_RejectsSecondOccurrence()
    {
        // Arrange
        var lines = new List<RefInvoiceCsv>
        {
            CreateLine("C000123", "FA-2024-0001"),
            CreateLine("C000123", "FA-2024-0001"),
        };

        // Act
        var result = await _sut.InsertPendingLinesAsync(lines, BlobName);

        // Assert
        result.ValidLines.Should().Be(1);
        result.RejectedLines.Should().Be(1);
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Line.Should().Be(2);
        result.Errors[0].Reason.Should().Be("Duplicate line (operation, accountNumber, invoiceNumber)");
    }

    [Fact]
    public async Task InsertPendingLinesAsync_WithLineAlreadyInDatabase_RejectsItAsDuplicate()
    {
        // Arrange
        var lines = new List<RefInvoiceCsv>
        {
            CreateLine("C000123", "FA-2024-0001"),
        };

        _invoiceRepositoryMock
            .Setup(r => r.GetByInvoiceNumbersAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync([
                new InvoiceEntity { AccountNumber = "C000123", InvoiceNumber = "FA-2024-0001", Operation = "INSERT" }
            ]);

        // Act
        var result = await _sut.InsertPendingLinesAsync(lines, BlobName);

        // Assert
        result.ValidLines.Should().Be(0);
        result.RejectedLines.Should().Be(1);
        result.Errors[0].Reason.Should().Be("Duplicate line (operation, accountNumber, invoiceNumber)");
        _invoiceRepositoryMock.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<InvoiceEntity>>()), Times.Never);
    }

    [Fact]
    public async Task InsertPendingLinesAsync_WithSameInvoiceButDifferentOperation_InsertsBoth()
    {
        // Arrange
        var deleteLine = CreateLine("C000123", "FA-2024-0001");
        deleteLine.Operation = "DELETE";

        var lines = new List<RefInvoiceCsv>
        {
            CreateLine("C000123", "FA-2024-0001"),
            deleteLine,
        };

        // Act
        var result = await _sut.InsertPendingLinesAsync(lines, BlobName);

        // Assert
        result.ValidLines.Should().Be(2);
        result.RejectedLines.Should().Be(0);
    }

    [Fact]
    public async Task InsertPendingLinesAsync_WithNoValidLine_DoesNotInsertAnything()
    {
        // Arrange
        var invalidLine = CreateLine("C000123", "FA-2024-0001");
        invalidLine.Operation = "UPDATE";

        // Act
        var result = await _sut.InsertPendingLinesAsync([invalidLine], BlobName);

        // Assert
        result.ValidLines.Should().Be(0);
        result.RejectedLines.Should().Be(1);
        _invoiceRepositoryMock.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<InvoiceEntity>>()), Times.Never);
    }

    [Fact]
    public async Task InsertPendingLinesAsync_WithNoLines_ReturnsEmptySummaryWithoutInsert()
    {
        // Act
        var result = await _sut.InsertPendingLinesAsync([], BlobName);

        // Assert
        result.BlobName.Should().Be(BlobName);
        result.TotalLines.Should().Be(0);
        result.ValidLines.Should().Be(0);
        result.RejectedLines.Should().Be(0);
        result.Errors.Should().BeEmpty();
        _invoiceRepositoryMock.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<InvoiceEntity>>()), Times.Never);
    }

    private static RefInvoiceCsv CreateLine(string accountNumber, string invoiceNumber)
    {
        return new RefInvoiceCsv
        {
            AccountNumber = accountNumber,
            InvoiceNumber = invoiceNumber,
            InvoiceDate = "15/01/2024",
            DocumentPath = "https://docs.pulse.fr/" + invoiceNumber + ".pdf",
            Operation = "INSERT",
        };
    }
}

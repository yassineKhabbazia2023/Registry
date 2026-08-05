// <copyright file="InvoiceControllerTest.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Exceptions;
using Application.Interfaces;
using Application.Models.Results;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Registry.WebApi.Controllers;
using System.Net;
using WebApi.Configurations;

namespace Registry.WebApi.Tests.Controllers;

public class InvoiceControllerTest
{
    private const string HeaderToken = "invoice-header-secret";
    private const string QueryToken = "legacy-query-secret";
    private const string CsvContent = "client_code;operation\nC000123;INSERT\n";

    private readonly Mock<IHeaderTokenValidator> _headerTokenValidatorMock;
    private readonly Mock<IInvoiceReceptionService> _invoiceReceptionServiceMock;
    private readonly Mock<IInvoiceContentService> _invoiceContentServiceMock;
    private readonly InvoiceController _controller;

    public InvoiceControllerTest()
    {
        _headerTokenValidatorMock = new Mock<IHeaderTokenValidator>();
        _invoiceReceptionServiceMock = new Mock<IInvoiceReceptionService>(MockBehavior.Strict);
        _invoiceContentServiceMock = new Mock<IInvoiceContentService>(MockBehavior.Strict);
        var logger = new Mock<ILogger<InvoiceController>>();

        _controller = new InvoiceController(
            _headerTokenValidatorMock.Object,
            _invoiceReceptionServiceMock.Object,
            _invoiceContentServiceMock.Object,
            logger.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
    }

    [Fact]
    public async Task UpdateAsync_WithUnauthorizedToken_ShouldReturnUnauthorizedWithoutReception()
    {
        // Arrange
        _headerTokenValidatorMock.Setup(x => x.IsAuthorized(HeaderToken, QueryToken)).Returns(false);

        // Act
        var response = await _controller.UpdateAsync(HeaderToken, QueryToken, CsvContent) as UnauthorizedObjectResult;

        // Assert
        response.Should().NotBeNull();
        response!.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);
        response.Value.Should().Be("Invalid token.");
        _invoiceReceptionServiceMock.Verify(s => s.ReceiveAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenReceptionIsRejected_ShouldReturnBadRequestWithErrorMessage()
    {
        // Arrange
        _headerTokenValidatorMock.Setup(x => x.IsAuthorized(HeaderToken, null)).Returns(true);
        _invoiceReceptionServiceMock
            .Setup(s => s.ReceiveAsync(CsvContent))
            .ReturnsAsync(InvoiceCsvReceptionOutcome.Rejected("Invalid data: Missing columns in header"));

        // Act
        var response = await _controller.UpdateAsync(HeaderToken, null, CsvContent) as BadRequestObjectResult;

        // Assert
        response.Should().NotBeNull();
        response!.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        response.Value.Should().Be("Invalid data: Missing columns in header");
    }

    [Fact]
    public async Task UpdateAsync_WhenReceptionSucceedsWithoutRejectedLines_ShouldReturnOkWithSummary()
    {
        // Arrange
        var summary = new InvoiceCsvReceptionResult { BlobName = "Invoice_20260731_101530.csv", TotalLines = 1, ValidLines = 1 };

        _headerTokenValidatorMock.Setup(x => x.IsAuthorized(null, QueryToken)).Returns(true);
        _invoiceReceptionServiceMock
            .Setup(s => s.ReceiveAsync(CsvContent))
            .ReturnsAsync(InvoiceCsvReceptionOutcome.Accepted(summary));

        // Act
        var response = await _controller.UpdateAsync(null, QueryToken, CsvContent) as OkObjectResult;

        // Assert
        response.Should().NotBeNull();
        response!.StatusCode.Should().Be((int)HttpStatusCode.OK);
        response.Value.Should().BeSameAs(summary);
    }

    [Fact]
    public async Task UpdateAsync_WhenReceptionSucceedsWithRejectedLines_ShouldReturnBadRequestWithSummary()
    {
        // Arrange
        var summary = new InvoiceCsvReceptionResult { BlobName = "Invoice_20260731_101530.csv", TotalLines = 2, ValidLines = 1, RejectedLines = 1 };

        _headerTokenValidatorMock.Setup(x => x.IsAuthorized(HeaderToken, null)).Returns(true);
        _invoiceReceptionServiceMock
            .Setup(s => s.ReceiveAsync(CsvContent))
            .ReturnsAsync(InvoiceCsvReceptionOutcome.Accepted(summary));

        // Act
        var response = await _controller.UpdateAsync(HeaderToken, null, CsvContent) as BadRequestObjectResult;

        // Assert
        response.Should().NotBeNull();
        response!.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        response.Value.Should().BeSameAs(summary);
    }

    [Fact]
    public async Task GetContentAsync_WhenInvoiceKnown_ShouldStreamPdfInline()
    {
        // Arrange
        _invoiceContentServiceMock
            .Setup(s => s.GetPdfAsync("FAC-1", "ACC-1"))
            .ReturnsAsync(CreateContent("FAC-2026-004512.pdf"));

        // Act
        var response = await _controller.GetContentAsync("FAC-1", "ACC-1") as FileStreamResult;

        // Assert
        response.Should().NotBeNull();
        response!.ContentType.Should().Be("application/pdf");

        var contentDisposition = _controller.Response.Headers.ContentDisposition.ToString();
        contentDisposition.Should().StartWith("inline");
        contentDisposition.Should().Contain("FAC-2026-004512.pdf");
    }

    [Fact]
    public async Task GetContentAsync_WhenInvoiceUnknown_ShouldReturnNotFound()
    {
        // Arrange
        _invoiceContentServiceMock
            .Setup(s => s.GetPdfAsync("FAC-UNKNOWN", "ACC-1"))
            .ReturnsAsync((InvoiceContentResponse?)null);

        // Act
        var response = await _controller.GetContentAsync("FAC-UNKNOWN", "ACC-1");

        // Assert
        response.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetContentAsync_WhenDownloadFails_ShouldReturnBadGatewayWithoutTechnicalDetails()
    {
        // Arrange
        _invoiceContentServiceMock
            .Setup(s => s.GetPdfAsync("FAC-1", "ACC-1"))
            .ThrowsAsync(new InvoiceDownloadTechnicalException(
                "Invoice pdf download failed for blob recouvrement/Invoices/FRANT/2025/TPME_FAC_CLI_202510031534.pdf: status 403."));

        // Act
        var response = await _controller.GetContentAsync("FAC-1", "ACC-1") as ObjectResult;

        // Assert
        response.Should().NotBeNull();
        response!.StatusCode.Should().Be((int)HttpStatusCode.BadGateway);

        var problem = response.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Status.Should().Be((int)HttpStatusCode.BadGateway);
        problem.Title.Should().NotContain("recouvrement");
        problem.Detail.Should().BeNull();
    }

    private static InvoiceContentResponse CreateContent(string fileName)
    {
        return new InvoiceContentResponse(new MemoryStream(), new MemoryStream("%PDF"u8.ToArray()), fileName);
    }
}

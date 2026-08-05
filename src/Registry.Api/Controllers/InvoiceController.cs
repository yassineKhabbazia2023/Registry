// <copyright file="InvoiceController.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Exceptions;
using Application.Interfaces;
using Application.Models.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using WebApi.Configurations;

namespace Registry.WebApi.Controllers;

/// <summary>
/// InvoiceController.
/// </summary>
[ApiController]
[Route("api/invoices")]
public class InvoiceController : ControllerBase
{
    private const string PdfContentType = "application/pdf";
    private const string InlineDisposition = "inline";

    private readonly IHeaderTokenValidator _headerTokenValidator;
    private readonly IInvoiceReceptionService _invoiceReceptionService;
    private readonly IInvoiceContentService _invoiceContentService;
    private readonly ILogger<InvoiceController> _logger;

    /// <summary>
    /// InvoiceController.
    /// </summary>
    /// <param name="headerTokenValidator">The header token validator.</param>
    /// <param name="invoiceReceptionService">The invoice reception service.</param>
    /// <param name="invoiceContentService">The invoice content service.</param>
    /// <param name="logger">The logger.</param>
    public InvoiceController(
        IHeaderTokenValidator headerTokenValidator,
        IInvoiceReceptionService invoiceReceptionService,
        IInvoiceContentService invoiceContentService,
        ILogger<InvoiceController> logger)
    {
        _headerTokenValidator = headerTokenValidator;
        _invoiceReceptionService = invoiceReceptionService;
        _invoiceContentService = invoiceContentService;
        _logger = logger;
    }

    /// <summary>
    /// Receives the Seres invoices csv.
    /// </summary>
    /// <param name="headerToken">X-Registry-Token header value, checked first.</param>
    /// <param name="token">Query string token, transitional fallback.</param>
    /// <param name="data">Raw csv content.</param>
    /// <returns>The reception summary.</returns>
    [HttpPost("update")]
    [Consumes("application/csv")]
    public async Task<IActionResult> UpdateAsync(
        [FromHeader(Name = "X-Registry-Token")] string? headerToken,
        [FromQuery] string? token,
        [FromBody] string data)
    {
        if (!_headerTokenValidator.IsAuthorized(headerToken, token))
        {
            _logger.LogWarning("Invoice csv rejected: invalid token (header used: {HeaderUsed})", headerToken != null);
            return new UnauthorizedObjectResult("Invalid token.");
        }

        var outcome = await _invoiceReceptionService.ReceiveAsync(data);

        if (!outcome.IsAccepted)
        {
            return BadRequest(outcome.ErrorMessage);
        }

        return outcome.Summary!.RejectedLines == 0 ? Ok(outcome.Summary) : BadRequest(outcome.Summary);
    }

    /// <summary>
    /// Streams the pdf of the given invoice.
    /// </summary>
    /// <param name="invoiceNumber">The invoice number.</param>
    /// <param name="accountNumber">The account the invoice belongs to.</param>
    /// <returns>The pdf stream.</returns>
    /// <response code="200">The pdf is streamed as application/pdf.</response>
    /// <response code="404">The invoice is unknown to ref.Invoice for that account.</response>
    /// <response code="502">The pdf download failed.</response>
    [HttpGet("{invoiceNumber}/accounts/{accountNumber}/content")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> GetContentAsync(string invoiceNumber, string accountNumber)
    {
        InvoiceContentResponse? content;

        try
        {
            content = await _invoiceContentService.GetPdfAsync(invoiceNumber, accountNumber);
        }
        catch (InvoiceDownloadTechnicalException exception)
        {
            _logger.LogError(
                exception,
                "Invoice pdf download failed for invoice {InvoiceNumber}, account {AccountNumber}",
                invoiceNumber,
                accountNumber);

            return StatusCode(
                StatusCodes.Status502BadGateway,
                new ProblemDetails
                {
                    Status = StatusCodes.Status502BadGateway,
                    Title = "Invoice pdf download failed."
                });
        }

        if (content is null)
        {
            _logger.LogWarning(
                "No invoice pdf served for invoice {InvoiceNumber}, account {AccountNumber}",
                invoiceNumber,
                accountNumber);
            return NotFound();
        }

        // Disposed only once the response body has been written, not before.
        Response.RegisterForDisposeAsync(content);

        var contentDisposition = new ContentDispositionHeaderValue(InlineDisposition);
        contentDisposition.SetHttpFileName(content.FileName);
        Response.Headers.ContentDisposition = contentDisposition.ToString();

        // Passing fileDownloadName here would overwrite the inline header set above with "attachment".
        return File(content.Content, PdfContentType);
    }
}

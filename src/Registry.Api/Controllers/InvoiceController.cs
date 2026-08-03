// <copyright file="InvoiceController.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using WebApi.Configurations;

namespace Registry.WebApi.Controllers;

/// <summary>
/// InvoiceController.
/// </summary>
[ApiController]
[Route("api/invoice")]
public class InvoiceController : ControllerBase
{
    private readonly IHeaderTokenValidator _headerTokenValidator;
    private readonly IInvoiceReceptionService _invoiceReceptionService;
    private readonly ILogger<InvoiceController> _logger;

    /// <summary>
    /// InvoiceController.
    /// </summary>
    /// <param name="headerTokenValidator">The header token validator.</param>
    /// <param name="invoiceReceptionService">The invoice reception service.</param>
    /// <param name="logger">The logger.</param>
    public InvoiceController(
        IHeaderTokenValidator headerTokenValidator,
        IInvoiceReceptionService invoiceReceptionService,
        ILogger<InvoiceController> logger)
    {
        _headerTokenValidator = headerTokenValidator;
        _invoiceReceptionService = invoiceReceptionService;
        _logger = logger;
    }

    /// <summary>
    /// Receives the Seres invoices csv sent by IPAAS.
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
}

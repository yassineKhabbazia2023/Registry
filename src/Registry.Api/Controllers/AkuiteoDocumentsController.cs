// <copyright file="AkuiteoDocumentsController.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Exceptions;
using Application.Interfaces;
using Application.Models;
using Application.Models.Results;
using Kpmg.ExceptionMiddleware.AdvancedException;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Registry.WebApi.Controllers;

/// <summary>
/// Exposes Akuiteo document endpoints.
/// </summary>
[ApiController]
[Route("api/akuiteo")]
public class AkuiteoDocumentsController : ControllerBase
{
    private readonly IAkuiteoDocumentService akuiteoDocumentService;
    private readonly ILogger<AkuiteoDocumentsController> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AkuiteoDocumentsController"/> class.
    /// </summary>
    /// <param name="akuiteoDocumentService">The Akuiteo document service.</param>
    /// <param name="logger">The logger.</param>
    public AkuiteoDocumentsController(
        IAkuiteoDocumentService akuiteoDocumentService,
        ILogger<AkuiteoDocumentsController> logger)
    {
        this.akuiteoDocumentService = akuiteoDocumentService;
        this.logger = logger;
    }

    /// <summary>
    /// Uploads one document to Akuiteo for an account.
    /// </summary>
    /// <param name="accountNumber">The Akuiteo account number receiving the document.</param>
    /// <param name="document">The uploaded PDF or image document.</param>
    /// <returns>The document upload outcome.</returns>
    /// <response code="201">The document was uploaded to Akuiteo.</response>
    /// <response code="400">The account number or uploaded document is invalid.</response>
    /// <response code="409">Akuiteo is unavailable or returned a technical error. The problem title contains the Akuiteo error message when available.</response>
    [HttpPost("account/{accountNumber}/documents")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(AkuiteoDocumentUploadResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> UploadDocumentAsync(string accountNumber, IFormFile document)
    {
        try
        {
            using var documentContent = document?.OpenReadStream() ?? Stream.Null;
            var response = await akuiteoDocumentService.UploadDocumentAsync(new AkuiteoDocumentUploadRequest
            {
                AccountNumber = accountNumber,
                DocumentName = document?.FileName ?? string.Empty,
                ContentType = document?.ContentType ?? string.Empty,
                Length = document?.Length ?? 0,
                Content = documentContent
            });

            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (AkuiteoDocumentUploadTechnicalException exception)
        {
            return StatusCode(
                StatusCodes.Status409Conflict,
                new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = exception.Message
                });
        }
        catch (BadRequestException exception)
        {
            logger.LogWarning(
                exception,
                "Akuiteo document upload request is invalid. AccountNumber: {AccountNumber}, DocumentName: {DocumentName}, ContentType: {ContentType}, Length: {Length}, Reason: {Reason}",
                accountNumber,
                document?.FileName,
                document?.ContentType,
                document?.Length,
                exception.Message);
            return BadRequest();
        }
    }
}

// <copyright file="AkuiteoController.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Exceptions;
using Application.Interfaces;
using Application.Models.Results;
using Application.Requests;
using System.ComponentModel.DataAnnotations;
using Kpmg.ExceptionMiddleware.AdvancedException;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace Registry.WebApi.Controllers;

/// <summary>
/// Exposes Akuiteo customer, contact, and account endpoints.
/// </summary>
[ApiController]
[Route("api/akuiteo")]
public class AkuiteoController : ControllerBase
{
    private readonly IAkuiteoCustomerService akuiteoCustomerService;
    private readonly IAkuiteoContactService akuiteoContactService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AkuiteoController"/> class.
    /// </summary>
    /// <param name="akuiteoCustomerService">The Akuiteo customer service.</param>
    /// <param name="akuiteoContactService">The Akuiteo contact service.</param>
    public AkuiteoController(
        IAkuiteoCustomerService akuiteoCustomerService,
        IAkuiteoContactService akuiteoContactService)
    {
        this.akuiteoCustomerService = akuiteoCustomerService;
        this.akuiteoContactService = akuiteoContactService;
    }

    /// <summary>
    /// Creates a customer in Akuiteo.
    /// </summary>
    /// <param name="request">The customer payload to create, including manager contact identifiers resolved from Contact.Contacts.</param>
    /// <returns>The created Akuiteo account number.</returns>
    /// <response code="201">The Akuiteo customer was created and the account number is returned.</response>
    /// <response code="400">The request payload is invalid or incomplete.</response>
    /// <response code="409">Akuiteo is unavailable or returned a technical error. The problem title contains the Akuiteo error message when available.</response>
    [HttpPost("customers")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(AkuiteoCustomerCreationResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> CreateCustomerAsync([FromBody] AkuiteoCustomerCreationRequest request)
    {
        try
        {
            var response = await akuiteoCustomerService.CreateCustomerAsync(request);
            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (AkuiteoCustomerCreationTechnicalException exception)
        {
            return StatusCode(
                StatusCodes.Status409Conflict,
                new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = exception.Message
                });
        }
        catch (BadRequestException)
        {
            return BadRequest();
        }
    }

    /// <summary>
    /// Creates a contact in Akuiteo.
    /// </summary>
    /// <param name="request">The contact payload to create.</param>
    /// <returns>The created Akuiteo contact identifier.</returns>
    /// <response code="201">The Akuiteo contact was created and the contact identifier is returned.</response>
    /// <response code="400">The request payload is invalid or incomplete.</response>
    /// <response code="409">Akuiteo is unavailable or returned a technical error. The problem title contains the Akuiteo error message when available.</response>
    [HttpPost("contacts")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(AkuiteoContactCreationResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> CreateContactAsync([FromBody] AkuiteoContactCreationRequest request)
    {
        try
        {
            var response = await akuiteoContactService.CreateContactAsync(request);
            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (AkuiteoContactCreationTechnicalException exception)
        {
            return StatusCode(
                StatusCodes.Status409Conflict,
                new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = exception.Message
                });
        }
        catch (BadRequestException)
        {
            return BadRequest();
        }
    }

    /// <summary>
    /// Searches contacts in Akuiteo using an exact email filter.
    /// </summary>
    /// <param name="email">The contact email address.</param>
    /// <returns>The matching contacts, or an empty collection when none exists.</returns>
    /// <response code="200">The contact search completed successfully.</response>
    /// <response code="400">The email query parameter is missing or invalid.</response>
    /// <response code="409">Akuiteo is unavailable or returned a technical error.</response>
    [HttpGet("contacts")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyCollection<AkuiteoContactSearchDataResponse>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> SearchContactsAsync(
        [FromQuery, Required, EmailAddress] string email)
    {
        try
        {
            return Ok(await akuiteoContactService.SearchContactsAsync(email));
        }
        catch (AkuiteoContactSearchTechnicalException exception)
        {
            return StatusCode(
                StatusCodes.Status409Conflict,
                new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = exception.Message
                });
        }
    }

    /// <summary>
    /// Retrieves payment information for an Akuiteo account.
    /// </summary>
    /// <param name="accountId">The Registry account identifier.</param>
    /// <returns>The configured payment conditions, methods, and banking information without the Akuiteo metadata wrapper.</returns>
    /// <response code="200">The payment information is returned, or an empty JSON object when no payment preference is configured.</response>
    /// <response code="404">The Registry account was not found.</response>
    /// <response code="409">Akuiteo is unavailable or returned a technical error.</response>
    [HttpGet("account/{accountId:int:min(1)}/payment-informations")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AkuiteoPaymentInformationsDataResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> GetPaymentInformationsAsync(int accountId)
    {
        try
        {
            var response = await akuiteoCustomerService.GetPaymentInformationsAsync(accountId);
            return Ok(response);
        }
        catch (AkuiteoAccountOperationTechnicalException exception)
        {
            return CreateAccountOperationConflict(exception);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Updates banking information for an Akuiteo account.
    /// </summary>
    /// <param name="accountId">The Registry account identifier.</param>
    /// <param name="request">The collection of banking-information changes.</param>
    /// <returns>The Akuiteo operation metadata.</returns>
    /// <response code="200">Akuiteo updated the banking information.</response>
    /// <response code="400">The request payload is invalid.</response>
    /// <response code="404">The Registry account was not found.</response>
    /// <response code="409">Akuiteo is unavailable or returned a technical error.</response>
    [HttpPost("account/{accountId:int:min(1)}/banking-informations")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AkuiteoAccountOperationResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> UpdateBankingInformationsAsync(
        int accountId,
        [FromBody] IReadOnlyCollection<AkuiteoBankingInformationRequest> request)
    {
        try
        {
            var response = await akuiteoCustomerService.UpdateBankingInformationsAsync(accountId, request);
            return Ok(response);
        }
        catch (AkuiteoAccountOperationTechnicalException exception)
        {
            return CreateAccountOperationConflict(exception);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Partially updates an Akuiteo account with an unmodified generic JSON payload.
    /// </summary>
    /// <param name="accountId">The Registry account identifier.</param>
    /// <param name="request">The generic Akuiteo account fields to update.</param>
    /// <returns>The Akuiteo operation metadata.</returns>
    /// <response code="200">Akuiteo updated the account.</response>
    /// <response code="400">The JSON request payload is malformed.</response>
    /// <response code="404">The Registry account was not found.</response>
    /// <response code="409">Akuiteo is unavailable or returned a technical error.</response>
    [HttpPatch("account/{accountId:int:min(1)}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AkuiteoAccountOperationResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> PatchAccountAsync(int accountId, [FromBody] JObject request)
    {
        try
        {
            var response = await akuiteoCustomerService.PatchAccountAsync(accountId, request);
            return Ok(response);
        }
        catch (AkuiteoAccountOperationTechnicalException exception)
        {
            return CreateAccountOperationConflict(exception);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Creates the standard Registry conflict response for an Akuiteo account operation.
    /// </summary>
    /// <param name="exception">The account operation failure.</param>
    /// <returns>The conflict response.</returns>
    private ObjectResult CreateAccountOperationConflict(AkuiteoAccountOperationTechnicalException exception)
    {
        return StatusCode(
            StatusCodes.Status409Conflict,
            new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = exception.Message
            });
    }
}

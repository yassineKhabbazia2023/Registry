// <copyright file="AkuiteoController.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Exceptions;
using Application.Interfaces;
using Application.Models.Results;
using Application.Requests;
using Kpmg.ExceptionMiddleware.AdvancedException;
using Microsoft.AspNetCore.Mvc;

namespace Registry.WebApi.Controllers;

/// <summary>
/// Exposes Akuiteo customer and contact creation endpoints.
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
}

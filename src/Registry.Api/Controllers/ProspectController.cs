// <copyright file="ProspectController.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Exceptions;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Registry.WebApi.Controllers;

/// <summary>
/// Exposes prospect-related endpoints.
/// </summary>
[ApiController]
[Route("api/prospects")]
public class ProspectController : ControllerBase
{
    private readonly IProspectEligibilityService prospectEligibilityService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProspectController"/> class.
    /// </summary>
    /// <param name="prospectEligibilityService">The prospect eligibility service.</param>
    public ProspectController(IProspectEligibilityService prospectEligibilityService)
    {
        this.prospectEligibilityService = prospectEligibilityService;
    }

    /// <summary>
    /// Checks whether a company is eligible according to its SIRET.
    /// </summary>
    /// <param name="siret">The SIRET to validate.</param>
    /// <returns>The HTTP response describing the SIRET eligibility outcome.</returns>
    /// <response code="200">The company already exists in Akuiteo and is not eligible.</response>
    /// <response code="202">The company does not exist in Akuiteo and is eligible.</response>
    /// <response code="400">The provided SIRET is invalid or the Akuiteo eligibility check failed.</response>
    [HttpGet("check-eligibility")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CheckEligibilityAsync([FromQuery] string? siret)
    {
        try
        {
            var isEligible = await prospectEligibilityService.CheckEligibilityAsync(siret);
            return StatusCode(isEligible ? StatusCodes.Status202Accepted : StatusCodes.Status200OK);
        }
        catch (AkuiteoAccountSearchTechnicalException)
        {
            return BadRequest();
        }
    }
}

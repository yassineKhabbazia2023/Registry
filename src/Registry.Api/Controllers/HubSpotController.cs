// <copyright file="HubSpotController.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Requests;
using Microsoft.AspNetCore.Mvc;

namespace Registry.WebApi.Controllers;

/// <summary>
/// HubSpotController.
/// </summary>
[ApiController]
[Route("api/hubspot")]
public class HubSpotController : ControllerBase
{
    private readonly IHubSpotService hubSpotService;

    /// <summary>
    /// HubSpotController
    /// <param name="hubSpotService"></param>
    /// </summary>
    public HubSpotController(IHubSpotService hubSpotService)
    {
        this.hubSpotService = hubSpotService;
    }

    /// <summary>
    /// Soumettre un formulaire HubSpot via l'endpoint d'integration.
    /// </summary>
    /// <param name="currentUserId">Identifiant de l'utilisateur connecte (header CurrentUser).</param>
    /// <param name="accountNumber">Identifiant fonctionnel de l'entite morale (optionnel).</param>
    /// <param name="request">Donnees du formulaire HubSpot.</param>
    /// <returns>http 200.</returns>
    /// <returns>http 400.</returns>
    [HttpPost("accounts/{accountNumber}/submissions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitIntegrationAsync([FromHeader(Name = "CurrentUser")] int currentUserId, string? accountNumber, [FromBody] HubSpotSubmissionInputRequest request)
    {
        var result = await hubSpotService.SubmitIntegrationAsync(accountNumber, request);
        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, result.ErrorMessage);
        }

        return Ok();
    }
}

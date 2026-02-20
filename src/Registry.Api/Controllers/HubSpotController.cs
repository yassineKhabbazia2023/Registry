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
    /// <param name="accountId">Identifiant de l'entite morale.</param>
    /// <param name="request">Donnees du formulaire HubSpot.</param>
    /// <returns>http 201.</returns>
    /// <returns>http 422.</returns>
    [HttpPost("accounts/{accountId:int:min(1)}/submissions")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> SubmitIntegrationAsync([FromHeader(Name = "CurrentUser")] int currentUserId, int accountId, [FromBody] HubSpotSubmissionInputRequest request)
    {
        try
        {
            var result = await hubSpotService.SubmitIntegrationAsync(accountId, request);
            return StatusCode(result.StatusCode);
        }
        catch (ArgumentException)
        {
            return StatusCode(StatusCodes.Status422UnprocessableEntity);
        }
    }

    /// <summary>
    /// Retourne l'etat de soumission d'un formulaire HubSpot.
    /// </summary>
    /// <param name="accountId">Identifiant de l'entite morale.</param>
    /// <returns>http 200.</returns>
    /// <returns>http 404.</returns>
    [HttpGet("accounts/{accountId:int:min(1)}/submissions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubmissionStateAsync(int accountId)
    {
        try
        {
            var result = await hubSpotService.GetSubmissionStateAsync(accountId);
            return StatusCode(result.StatusCode);
        }
        catch (ArgumentException)
        {
            return StatusCode(StatusCodes.Status404NotFound);
        }
    }
}

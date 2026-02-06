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
    /// <returns>http 201.</returns>
    /// <returns>http 422.</returns>
    [HttpPost("accounts/{accountNumber}/submissions")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> SubmitIntegrationAsync([FromHeader(Name = "CurrentUser")] int currentUserId, string? accountNumber, [FromBody] HubSpotSubmissionInputRequest request)
    {
        try
        {
            var result = await hubSpotService.SubmitIntegrationAsync(accountNumber, request);
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
    /// <param name="accountNumber">Identifiant fonctionnel de l'entite morale.</param>
    /// <returns>http 200.</returns>
    /// <returns>http 404.</returns>
    [HttpGet("accounts/{accountNumber}/submissions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubmissionStateAsync(string accountNumber)
    {
        try
        {
            var result = await hubSpotService.GetSubmissionStateAsync(accountNumber);
            return StatusCode(result.StatusCode);
        }
        catch (ArgumentException)
        {
            return StatusCode(StatusCodes.Status404NotFound);
        }
    }
}

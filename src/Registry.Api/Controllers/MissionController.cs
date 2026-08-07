// <copyright file="MissionController.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WebApi.Configurations.Models;

namespace Registry.WebApi.Controllers;

/// <summary>
/// MissionController.
/// </summary>
[ApiController]
[Route("api/mission")]
public class MissionController : ControllerBase
{
    private readonly IMissionReceptionService _missionReceptionService;
    private readonly TokenModel _tokenModel;

    /// <summary>
    /// MissionController.
    /// </summary>
    /// <param name="missionReceptionService">missionReceptionService.</param>
    /// <param name="tokenModel">tokenModel.</param>
    public MissionController(IMissionReceptionService missionReceptionService, IOptions<TokenModel> tokenModel)
    {
        _missionReceptionService = missionReceptionService;
        _tokenModel = tokenModel!.Value;
    }

    /// <summary>
    /// Allow updating mission informations for Pulse.
    /// </summary>
    /// <param name="token">Security token ensuring the caller is legit.</param>
    /// <param name="data">Data containing the mission information in CSV format.</param>
    /// <returns></returns>
    [HttpPost("update")]
    [Consumes("application/csv")]
    public async Task<IActionResult> UpdateAsync([FromQuery] string token, [FromBody] string data)
    {
        if (string.IsNullOrWhiteSpace(token) || !_tokenModel.Token.Equals(token))
        {
            return new UnauthorizedObjectResult("Invalid token.");
        }

        var outcome = await _missionReceptionService.ReceiveAsync(data);

        if (!outcome.IsAccepted)
        {
            return BadRequest(outcome.ErrorMessage);
        }

        return Ok(outcome.Summary);
    }
}

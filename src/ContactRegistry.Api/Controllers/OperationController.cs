// <copyright file="OperationController.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Requests;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ContactRegistry.WebApi.Controllers;

/// <summary>
/// OperationController.
/// </summary>
[ApiController]
[Route("api/operations")]
public class OperationController : ControllerBase
{
    private readonly IOperationService _operationService;

    /// <summary>
    /// OperationController.
    /// </summary>
    /// <param name="operationService">operationService.</param>
    public OperationController(IOperationService operationService)
    {
        _operationService = operationService;
    }

    /// <summary>
    /// GetOperationsAsync.
    /// </summary>
    /// <param name="accountNumber">AccountNumber.</param>
    /// <param name="operationSearchCriteria">Critère de recherche.</param>
    /// <returns>La liste des opérations en attente filtrée par accountNumber.</returns>
    [HttpGet("{accountNumber}")]
    public async Task<IActionResult> GetOperationsAsync([Required] string accountNumber, [FromQuery] OperationSearchCriteria operationSearchCriteria)
    {
        var result = await _operationService.GetOperationsAsync(accountNumber, operationSearchCriteria);

        return Ok(result);
    }
}

// <copyright file="OperationController.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ContactRegistry.WebApi.Controllers;

/// <summary>
/// OperationController.
/// </summary>
[ApiController]
[Route("api/operation")]
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
    /// <param name="operationName">Nom de l'opération.</param>
    /// <param name="status">Statut de l'opération.</param>
    /// <param name="accountNumber">Code IBS.</param>
    /// <returns>La liste des opérations en attente filtrée par operationName, status and accountNumber.</returns>
    [HttpGet]
    public async Task<IActionResult> GetOperationsAsync(string operationName, string status, string accountNumber)
    {
        var result = await _operationService.GetOperationsAsync(operationName, status, accountNumber);

        return Ok(result);
    }
}

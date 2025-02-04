// <copyright file="OperationController.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Exceptions;
using Application.Interfaces;
using Application.Models;
using Application.Requests;
using Kpmg.ExceptionMiddleware.AdvancedException;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Registry.WebApi.Controllers;

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

    /// <summary>
    /// Mettre à jour partiellement les informations d'une opération du registry.
    /// </summary>
    /// <param name="operationId">ID de l'opération.</param>
    /// <param name="email">Email du contact qui a validé/refusé l'opération.</param>
    /// <param name="creOperationPatch">Informations à mettre à jour.</param>
    /// <returns>Les informations de l'opération mises à jour.</returns>
    [HttpPatch("{operationId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RegOperation))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOperationAsync([Required] int operationId, [Required] string email, [FromBody] JsonPatchDocument<RegOperation> creOperationPatch)
    {
        if (creOperationPatch == null)
        {
            throw new BadRequestException(Errors.BadRequestOperationPatchCode, Errors.BadRequestOperationPatchMessage);
        }

        var operationToUpdate = await _operationService.GetOperationByIdAsync(operationId);
        creOperationPatch.ApplyTo(operationToUpdate!);
        var updatedOperation = await _operationService.UpdateOperationAsync(operationId, email, operationToUpdate!);

        return Ok(updatedOperation);
    }
}

// <copyright file="RoleCreatedEventHandler.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Consts;
using Application.Interfaces;
using Azure;
using Application.Mappers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Pulse.Back.Events.Abstractions;
using Pulse.Back.Events.IntegrationEvents;
using System.Net;
using System.Web.Http;

namespace Application.Providers;

public class RoleCreatedEventHandler : IEventHandler
{
    private readonly ILogger<RoleCreatedEventHandler> _logger;
    private readonly IRoleRegistryProvider _roleRegistryProvider;
    private readonly IRoleRepository _roleRepository;
    private readonly IOperationRepository _operationRepository;

    public RoleCreatedEventHandler(
        ILogger<RoleCreatedEventHandler> logger,
        IRoleRegistryProvider roleRegistryProvider,
        IRoleRepository roleRepository,
        IOperationRepository operationRepository)
    {
        _logger = logger;
        _roleRegistryProvider = roleRegistryProvider;
        _roleRepository = roleRepository;
        _operationRepository = operationRepository;
    }

    public async Task HandleAsync(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            _logger.LogError("[ERREUR] Impossible de traiter l'événement car le message est null - RoleCreatedEventHandler");
            return;
        }

        var roleEvent = JsonConvert.DeserializeObject<RoleCreatedEvent>(message);

        if (roleEvent?.Data == null || roleEvent.Data.ContactId <= 0 || roleEvent.Data.AccountId < -1)
        {
            _logger.LogError("[ERREUR] Format de données invalide pour le role du contact: {ContactId} sur l'account: {AccountId}", roleEvent?.Data.ContactId, roleEvent?.Data.AccountId);
            return;
        }

        _logger.LogInformation("Consommation de l'event type: {EventType}, contactId: {ContactId}, accountId: {AccountId}",
            roleEvent.EventType,
            roleEvent.Data.ContactId,
            roleEvent.Data.AccountId);

        var roleEntity = roleEvent!.Data.RoleEventCreatedDataToModel();

        // Map Role to model Pulse for persist in DB
        var rolePulse = roleEvent!.Data.MapToRoleEntity();
        await _roleRepository.AddRoleAsync(rolePulse!);

        // Update status operation
        await UpdateOperationProcessStatusAsync(rolePulse.ContactEmail!, rolePulse.AccountNumber!);

        var responseMessage = await _roleRegistryProvider.CreateRoleAsync(roleEntity);

        if (responseMessage.StatusCode != HttpStatusCode.OK)
        {
            var errorMessage = responseMessage.Content.ReadAsAsync<HttpError>()?.Result?.Message;
            _logger.LogError("[ERREUR] Échec de la création de role. Cause : {ErrorMessage}. - RoleCreatedEventHandler", errorMessage);
            return;
        }

        _logger.LogInformation("Le role du contact: {ContactId} sur l'account: {AccountId} vient d'être crée.", roleEntity.ContactEmailOffice, roleEntity.AccountNumber);
    }

    private async Task UpdateOperationProcessStatusAsync(string email, string accountNumber)
    {
        var operation = await _operationRepository.FindRoleOperationAsync(new Application.Requests.OperationSearchCriteria()
        {
            OperationName = "INSERT"
        }, email, accountNumber);

        if (operation.Any())
        {
            if (operation.First().ProcessStatus!.Equals(ProcessStatus.Sent, StringComparison.InvariantCultureIgnoreCase))
            {
                await _operationRepository.UpdateOperationProcessStatusAsync(ProcessStatus.Succeeded.ToString(), operation.First());
            }
        }
    }
}

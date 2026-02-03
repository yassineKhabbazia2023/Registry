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
using Application.Requests;
using Application.Enums;

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


        // Map Role to model Pulse for persist in DB
        var rolePulse = roleEvent!.Data.MapToRoleEntity();

        var existingRole = await _roleRepository.GetPulseRole(rolePulse.ContactEmail!, rolePulse.AccountNumber!);
        if (existingRole != null && existingRole.ContactFlagPortailFactures != rolePulse.ContactFlagPortailFactures)
        {
            // Synchronisation pour aligner la valeur de ContactFlagPortailFactures entre Registry et Pulse.
            existingRole.ContactFlagPortailFactures = rolePulse.ContactFlagPortailFactures;
            await _roleRepository.UpdatePulseRole(existingRole);
            await UpdateOperationProcessStatusAsync(rolePulse.ContactEmail!, rolePulse.AccountNumber!);
            return;
        }

        await _roleRepository.AddRoleAsync(rolePulse!);

        // Update status operation
        await UpdateOperationProcessStatusAsync(rolePulse.ContactEmail!, rolePulse.AccountNumber!);

        #region Flux sortant
        //var roleEntity = roleEvent!.Data.RoleEventCreatedDataToModel();
        //var responseMessage = await _roleRegistryProvider.CreateRoleAsync(roleEntity);

        //if (responseMessage.StatusCode != HttpStatusCode.OK)
        //{
        //    var errorMessage = responseMessage.Content.ReadAsAsync<HttpError>()?.Result?.Message;
        //    _logger.LogError("[ERREUR] Échec de la création de role. Cause : {ErrorMessage}. - RoleCreatedEventHandler", errorMessage);
        //    return;
        //}

        //_logger.LogInformation("Le role du contact: {ContactId} sur l'account: {AccountId} vient d'être crée.", roleEntity.ContactEmailOffice, roleEntity.AccountNumber);
        #endregion
    }

    private async Task UpdateOperationProcessStatusAsync(string email, string accountNumber)
    {
        var criteria = new OperationSearchCriteria()
        {
            OperationName = "INSERT"
        };

        var operation = await _operationRepository.FetchOperationsByCriteriaAsync(criteria, OperationStrategyType.ROLE, email, secondaryFilter: accountNumber);

        if (operation.Any())
        {
            await _operationRepository.BulkUpdateOperationsStatusAsync(ProcessStatus.Succeeded.ToString(), operation);
        }
    }
}

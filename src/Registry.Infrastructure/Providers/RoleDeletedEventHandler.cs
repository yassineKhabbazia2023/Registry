// <copyright file="RoleUpdatedEventHandler.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Consts;
using Application.Interfaces;
using Application.Mappers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Pulse.Back.Events.Abstractions;
using Pulse.Back.Events.IntegrationEvents;
using System.Net;
using System.Web.Http;

namespace Infrastructure.Providers;

public class RoleDeletedEventHandler : IEventHandler
{
    private readonly ILogger<RoleDeletedEventHandler> _logger;
    private readonly IRoleRegistryProvider _roleRegistryProvider;
    private readonly IRoleRepository _roleRepository;
    private readonly IOperationRepository _operationRepository;

    public RoleDeletedEventHandler(
    ILogger<RoleDeletedEventHandler> logger,
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
            _logger.LogError("[ERREUR] Impossible de traiter l'événement car le message est null - RoleDeletedEventHandler");
            return;
        }

        var roleEvent = JsonConvert.DeserializeObject<RoleDeletedEvent>(message);

        if (roleEvent?.Data == null || roleEvent.Data.ContactId <= 0 || roleEvent.Data.AccountId < -1)
        {
            _logger.LogError("[ERREUR] Format de données invalide pour le role du contact: {ContactId} sur l'account: {AccountId}", roleEvent?.Data.ContactId, roleEvent?.Data.AccountId);
            return;
        }

        _logger.LogInformation("Consommation de l'event type: {EventType}, contactId: {ContactId}, accountId: {AccountId}",
            roleEvent.EventType,
            roleEvent.Data.ContactId,
            roleEvent.Data.AccountId);

        var roleEntity = roleEvent!.Data.RoleEventDeletedDataToModel();

        // Map Role to model Pulse for persist in DB
        var rolePulse = roleEvent!.Data.MapToRoleEntity();
        await _roleRepository.DeleteRoleAsync(rolePulse!);

        await UpdateOperationProcessStatusAsync(rolePulse.ContactEmail!, rolePulse.AccountNumber!);

        var responseMessage = await _roleRegistryProvider.UpdateRoleAsync(roleEntity!);

        if (responseMessage.StatusCode != HttpStatusCode.OK)
        {
            var errorMessage = responseMessage.Content.ReadAsAsync<HttpError>().Result.Message;
            _logger.LogError("[ERREUR] Échec de la suppression de role. Cause : {ErrorMessage}. - RoleDeletedEventHandler", errorMessage);
            return;
        }

        _logger.LogInformation("Le role du contact: {ContactId} sur l'account: {AccountId} vient d'être modifié.", roleEntity.ContactEmailOffice, roleEntity.AccountNumber);
    }

    private async Task UpdateOperationProcessStatusAsync(string email, string accountNumber)
    {
        var operation = await _operationRepository.FindRoleOperationAsync(new Application.Requests.OperationSearchCriteria()
        {
            OperationName = "DELETED"
        }, email, accountNumber);

        if (operation != null)
        {
            if (operation.First().ProcessStatus!.Equals(ProcessStatus.Sent, StringComparison.InvariantCultureIgnoreCase))
            {
                await _operationRepository.UpdateOperationProcessStatusAsync(ProcessStatus.Succeeded.ToString(), operation.First());
            }
        }
    }
}

// <copyright file="RoleCreatedEventHandler.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Azure;
using Infrastructure.Mappers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Pulse.Back.Events.Abstractions;
using Pulse.Back.Events.IntegrationEvents;
using System.Net;
using System.Web.Http;

namespace Infrastructure.Providers;

public class RoleCreatedEventHandler : IEventHandler
{
    private readonly ILogger<RoleCreatedEventHandler> _logger;
    private readonly IRoleRegistryProvider _roleRegistryProvider;

    public RoleCreatedEventHandler(
        ILogger<RoleCreatedEventHandler> logger,
        IRoleRegistryProvider roleRegistryProvider)
    {
        _logger = logger;
        _roleRegistryProvider = roleRegistryProvider;
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

        var responseMessage = await _roleRegistryProvider.CreateRoleAsync(roleEntity);

        if (responseMessage.StatusCode != HttpStatusCode.OK)
        {
            var errorMessage = responseMessage.Content.ReadAsAsync<HttpError>().Result.Message;
            _logger.LogError("[ERREUR] Échec de la création de role. Cause : {ErrorMessage}. - RoleCreatedEventHandler", errorMessage);
        }

        _logger.LogInformation("Le role du contact: {ContactId} sur l'account: {AccountId} vient d'être crée.", roleEntity.ContactEmailOffice, roleEntity.AccountNumber);
    }
}

// <copyright file="RoleCreatedEventHandler.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Infrastructure.Mappers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Pulse.Back.Events.Abstractions;
using Pulse.Back.Events.IntegrationEvents;

namespace Infrastructure.Providers;

public class RoleCreatedEventHandler : IEventHandler
{
    private readonly ILogger<RoleCreatedEventHandler> _logger;
    private readonly IRoleRegistryService _roleRegistryService;

    public RoleCreatedEventHandler(
        ILogger<RoleCreatedEventHandler> logger,
        IRoleRegistryService roleRegistryService)
    {
        _logger = logger;
        _roleRegistryService = roleRegistryService;
    }

    public async Task HandleAsync(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        var roleEvent = JsonConvert.DeserializeObject<RoleCreatedEvent>(message);
        _logger.LogInformation("Consommation de l'event type: {EventType}, contactId: {ContactId}, accountId: {AccountId}",
            roleEvent?.EventType,
            roleEvent?.Data?.ContactId,
            roleEvent?.Data?.AccountId);

        if (roleEvent?.Data == null || roleEvent.Data.ContactId <= 0 || roleEvent.Data.AccountId < -1)
        {
            return;
        }

        var roleEntity = roleEvent!.Data.RoleEventDataToModel();

        if (!await _roleRegistryService.DoesRoleExistAsync(roleEntity))
        {
            await _roleRegistryService.CreateRoleAsync(roleEntity);

            _logger.LogInformation("Le role de contact: {ContactId}, account: {AccountId} vient d'être crée.", roleEntity.ContactId, roleEntity.AccountId);
        }
        else
        {
            _logger.LogInformation("Le role de contact: {ContactId}, account: {AccountId} existe déjà.", roleEntity.ContactId, roleEntity.AccountId);
        }

    }
}

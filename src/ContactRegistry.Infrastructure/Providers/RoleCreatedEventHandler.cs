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

        var roleEntity = roleEvent!.Data.RoleEventCreatedDataToModel();

        if (!await _roleRegistryProvider.DoesRoleExistAsync(roleEntity))
        {
            await _roleRegistryProvider.CreateRoleAsync(roleEntity);

            _logger.LogInformation("Le role du contact: {ContactId}, account: {AccountId} vient d'être crée.", roleEntity.ContactEmailOffice, roleEntity.AccountNumber);
        }
        else
        {
            await _roleRegistryProvider.UpdateRoleAsync(roleEntity);

            _logger.LogInformation("Le role du contact: {ContactId}, account: {AccountId} existe déjà et vient d'être modifié.", roleEntity.ContactEmailOffice, roleEntity.AccountNumber);
        }

    }
}

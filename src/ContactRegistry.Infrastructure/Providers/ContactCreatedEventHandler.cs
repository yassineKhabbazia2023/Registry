// <copyright file="ContactEventHandler.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Models;
using Infrastructure.Mappers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Pulse.Back.Events.Abstractions;
using Pulse.Back.Events.IntegrationEvents;
using Pulse.Back.Events.IntegrationEvents.EventsData;

namespace Infrastructure.Providers;
public class ContactCreatedEventHandler : IEventHandler
{
    private readonly ILogger<ContactCreatedEventHandler> _logger;
    private readonly IContactRegistryProvider _provider;


    public ContactCreatedEventHandler(ILogger<ContactCreatedEventHandler> logger, IContactRegistryProvider provider)
    {
        _logger = logger; 
        _provider = provider;
    }

    public async Task HandleAsync(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            _logger.LogError($"[EVENT-TYPE]: {nameof(ContactCreatedEvent)} [ERROR]: Message Body Is Empty");
            return;
        }

        var contactEvent = JsonConvert.DeserializeObject<ContactCreatedEvent>(message);
        _logger.LogInformation($"Consommation de l'event type: {contactEvent?.EventType}",
            contactEvent?.EventType,
            contactEvent?.Data?.ContactId);

        if (contactEvent?.Data == null)
        {
            _logger.LogError($"[EVENT-TYPE]: {nameof(ContactCreatedEvent)} [ERROR]: Data is Null Or Empty");
            return;
        }

        try
        {
            var contactRegistry = contactEvent.Data.ContactStateEventDataToModel();
            await _provider.CreateContactAsync(contactRegistry);
        }
        catch(Exception e)
        {
            _logger.LogError($"[EVENT-TYPE]: {nameof(ContactCreatedEvent)} [ERROR]: {e.Message}");
            return;
        }

        _logger.LogInformation($"[EVENT-TYPE]: {nameof(ContactCreatedEvent)} [EMAIL]: {contactEvent.Data.Email} Creation Succeeded");
    }
}


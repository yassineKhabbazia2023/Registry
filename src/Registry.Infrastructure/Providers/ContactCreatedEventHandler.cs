// <copyright file="ContactEventHandler.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Models;
using Application.Mappers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Pulse.Back.Events.Abstractions;
using Pulse.Back.Events.IntegrationEvents;
using Pulse.Back.Events.IntegrationEvents.EventsData;
using Application.Models.Results;
using Application.Models.Contacts;

namespace Application.Providers;
public class ContactCreatedEventHandler : IEventHandler
{
    private readonly ILogger<ContactCreatedEventHandler> logger;
    private readonly IContactService contactService;


    public ContactCreatedEventHandler(
        ILogger<ContactCreatedEventHandler> logger, 
        IContactService contactService
        )
    {
        this.logger = logger; 
        this.contactService = contactService;
    }

    public async Task HandleAsync(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            this.logger.LogError($"[EVENT-TYPE]: {nameof(ContactCreatedEvent)} [ERROR]: Message Body Is Empty");
            return;
        }

        var contactEvent = JsonConvert.DeserializeObject<ContactCreatedEvent>(message);
        this.logger.LogInformation($"Consommation de l'event type: {contactEvent?.EventType}",
            contactEvent?.EventType,
            contactEvent?.Data?.ContactId);

        if (contactEvent?.Data == null)
        {
            this.logger.LogError($"[EVENT-TYPE]: {nameof(ContactCreatedEvent)} [ERROR]: Data is Null Or Empty");
            return;
        }
       
        try
        {
            this.logger.LogInformation($"[Event]: {nameof(ContactCreatedEventHandler)} Started");
            ContactEventResult<Contact> result = await this.contactService.OnCreatedContactEventExecution(contactEvent.Data);
            this.logger.LogInformation($"[Event]: {nameof(ContactCreatedEventHandler)} Finished with the following result; [Result]: ${JsonConvert.SerializeObject(result)}");
        }
        catch (Exception e)
        {
            this.logger.LogError($"[EVENT-TYPE]: {nameof(ContactCreatedEvent)} [ERROR]: {e.Message}");
            return;
        }

        this.logger.LogInformation($"[EVENT-TYPE]: {nameof(ContactCreatedEvent)} [EMAIL]: {contactEvent.Data.Email} Creation Succeeded");
    }
}


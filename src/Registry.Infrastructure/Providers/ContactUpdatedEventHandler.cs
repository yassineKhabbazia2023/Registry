// <copyright file="ContactUpdatedEventHandler.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Models.Contacts;
using Application.Models.Results;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Pulse.Back.Events.Abstractions;
using Pulse.Back.Events.IntegrationEvents;

namespace Infrastructure.Providers;
public class ContactUpdatedEventHandler : IEventHandler
{
    private readonly ILogger<ContactUpdatedEventHandler> logger;
    private readonly IContactService contactService;

    public ContactUpdatedEventHandler(ILogger<ContactUpdatedEventHandler> logger, IContactService contactService)
    {
        this.logger = logger;
        this.contactService = contactService;
    }

    public async Task HandleAsync(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            logger.LogError($"[EVENT-TYPE]: {nameof(ContactUpdatedEvent)} [ERROR]: Message Body Is Empty");
            return;
        }

        var contactEvent = JsonConvert.DeserializeObject<ContactUpdatedEvent>(message);
        logger.LogInformation($"Consommation de l'event type: {contactEvent?.EventType}",
            contactEvent?.EventType,
            contactEvent?.Data?.ContactId);

        if (contactEvent == null || contactEvent?.Data == null || contactEvent.Data?.ContactId <= 0)
        {
            logger.LogError($"[EVENT-TYPE]: {nameof(ContactUpdatedEvent)} [ERROR]: Data is Null Or Empty [Body]: {JsonConvert.SerializeObject(contactEvent)}");
            return;
        }

        try
        {
            logger.LogInformation($"[Event]: {nameof(ContactUpdatedEventHandler)} Started");
            ContactEventResult<Contact> result = await this.contactService.OnUpdatedContactEventExecution(contactEvent.Data);
            logger.LogInformation($"[Event]: {nameof(ContactUpdatedEventHandler)} Finished with the following result; [Result]: ${JsonConvert.SerializeObject(result)}");
        }

        catch (Exception e)
        {
            logger.LogError($"[EVENT-TYPE]: {nameof(ContactUpdatedEvent)} [ERROR]: {e.Message}");
            throw;
        }

        logger.LogInformation($"[EVENT-TYPE]: {nameof(ContactUpdatedEvent)} [EMAIL]: {contactEvent.Data.Email} Update Succeeded");
    }
}


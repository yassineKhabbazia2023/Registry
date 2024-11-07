using Application.Interfaces;
using Infrastructure.Mappers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Pulse.Back.Events.IntegrationEvents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Providers
{
    public class ContactUpdatedEventHandler
    {
        private readonly ILogger<ContactUpdatedEventHandler> _logger;
        private readonly IContactRegistryProvider _provider;


        public ContactUpdatedEventHandler(ILogger<ContactUpdatedEventHandler> logger, IContactRegistryProvider provider)
        {
            _logger = logger;
            _provider = provider;
        }

        public async Task HandleAsync(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                _logger.LogError($"[EVENT-TYPE]: {nameof(ContactUpdatedEvent)} [ERROR]: Message Body Is Empty");
                return;
            }

            var contactEvent = JsonConvert.DeserializeObject<ContactUpdatedEvent>(message);
            _logger.LogInformation($"Consommation de l'event type: {contactEvent?.EventType}",
                contactEvent?.EventType,
                contactEvent?.Data?.ContactId);

            if (contactEvent == null || contactEvent?.Data == null || contactEvent.Data?.ContactId <= 0)
            {
                _logger.LogError($"[EVENT-TYPE]: {nameof(ContactUpdatedEvent)} [ERROR]: Data is Null Or Empty [Body]: {JsonConvert.SerializeObject(contactEvent)}");
                return;
            }

            try
            {
                var contactRegistry = contactEvent.Data.ContactStateEventDataToModel();
                await _provider.UpdateContactAsync(contactRegistry);
            }
            catch (Exception e)
            {
                _logger.LogError($"[EVENT-TYPE]: {nameof(ContactUpdatedEvent)} [ERROR]: {e.Message}");
                return;
            }

            _logger.LogInformation($"[EVENT-TYPE]: {nameof(ContactUpdatedEvent)} [EMAIL]: {contactEvent.Data.Email} Update Succeeded");
        }
    }
}

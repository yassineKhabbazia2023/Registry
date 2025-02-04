using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Pulse.Back.Events.Abstractions;
using Pulse.Back.Events.IntegrationEvents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Providers
{
    public class ContactRemovedEventHandler : IEventHandler
    {
        private readonly ILogger<ContactRemovedEventHandler> logger;
        private readonly IContactService contactService;
        public ContactRemovedEventHandler(ILogger<ContactRemovedEventHandler> logger,IContactService contactService)
        {
            this.logger = logger;
            this.contactService = contactService;
        }

        public async Task HandleAsync(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            var contactEvent = JsonConvert.DeserializeObject<ContactRemovedEvent>(message);
            this.logger.LogInformation("Consommation de l'event type: {EventType}, contactId: {ContactId}",
                contactEvent?.EventType,
                contactEvent?.Data?.ContactId);

            if (contactEvent?.Data == null || contactEvent?.Data?.ContactId <= 0)
            {
                this.logger.LogError($"[Event]: {contactEvent.EventType} Data is null or empty");
                return;
            }

            int? contactId = contactEvent?.Data?.ContactId;

            try
            {
                logger.LogInformation($"[Event]: {contactEvent.EventType} Starts Processing");
                var result = await this.contactService.OnRemovedContactEventExecution(contactEvent?.Data);
                logger.LogInformation($"[Event]: {contactEvent.EventType} Finish Processing with the following result; [Result]:{JsonConvert.SerializeObject(result)}");
            }
            catch(Exception ex ) 
            {
                this.logger.LogError($"[Event]: {contactEvent.EventType} Something went wrong while processing this request; [Error]:{ex.Message}");
                return;
            }

        }
    }
}

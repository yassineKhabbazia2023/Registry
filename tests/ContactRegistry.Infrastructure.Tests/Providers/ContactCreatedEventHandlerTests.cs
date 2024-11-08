using Application.Interfaces;
using Application.Models;
using Infrastructure.Providers;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Pulse.Back.Events.Abstractions;
using Pulse.Back.Events.IntegrationEvents;
using Pulse.Back.Events.IntegrationEvents.EventsData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContactRegistry.Infrastructure.Tests.Providers
{
    public class ContactCreatedEventHandlerTests
    {
        private readonly Mock<IContactRegistryProvider> _mockProvider;
        public ContactCreatedEventHandlerTests()
        {
            _mockProvider = new Mock<IContactRegistryProvider>(MockBehavior.Strict);
        }

        [Fact]
        public async Task ContactCreatedEvent_ShouldSendRequestToReferentiel()
        {
            ContactStateEventData contactStateEventData = new ContactStateEventData
            {
                Email = "test@kpmg.fr",
                ContactId = 1,
                Type = "Collaborateur",
                LastName = "Test",
                FirstName = "exam",
                IsActive = true,
                CreationDate = DateTime.Now,
                Source="PULSE"
            };


            var logger = new Mock<ILogger<ContactCreatedEventHandler>>();

            var contactEvent = new ContactCreatedEvent(contactStateEventData);

            var contentMessage = JsonConvert.SerializeObject(contactEvent);
            
            var eventHandler = new ContactCreatedEventHandler(logger.Object, _mockProvider.Object);

            _mockProvider.Setup(x => x.CreateContactAsync(It.IsAny<Application.Models.ContactRegistry>()));

            await eventHandler.HandleAsync(contentMessage);

            _mockProvider.Verify(x => x.CreateContactAsync(It.IsAny<Application.Models.ContactRegistry>()), Times.Once);

        }
    }
}
